using UnityEngine;
using System.Collections.Generic;

public class TilePerformance : MonoBehaviour
{
    private List<Animator> animatorsInTile = new List<Animator>();
    private List<Renderer> allRenderers = new List<Renderer>(); // Tile içindeki tüm görseller
    private Transform cameraTransform;
    private bool isVisible = true;

    [Header("Mesafe Ayarları")]
    [Tooltip("Bu mesafeden sonra Tile tamamen gizlenir (Rendering kapanır).")]
    public float hideDistance = 4000f; 
    [Tooltip("Bu mesafeden sonra animasyonlar durur.")]
    public float disableAnimDistance = 2000f;

    [Header("Gelişmiş Optimizasyon")]
    public bool alwaysDisableAnimations = true; 
    public bool convertToStaticMesh = true;

    void Awake()
    {
        if (Application.isPlaying && convertToStaticMesh)
        {
            SimplifySkinnedMeshes();
        }
    }

    void Start()
    {
        cameraTransform = Camera.main.transform;
        
        // Karodaki her şeyi listeye al (Renderer ve Animators)
        allRenderers.AddRange(GetComponentsInChildren<Renderer>());
        animatorsInTile.AddRange(GetComponentsInChildren<Animator>());

        if (alwaysDisableAnimations)
        {
            foreach (var anim in animatorsInTile) if (anim != null) anim.enabled = false;
        }
    }

    void Update()
    {
        // Mesafe tabanlı render optimizasyonu (Culling) kaldırıldı.
        // Her şey artık her mesafeden görünür kalacak.
        if (!isVisible) SetTileVisibility(true);

        // Animasyon kontrolü hala mesafe tabanlı devam edebilir (Opsiyonel)
        if (!alwaysDisableAnimations && !convertToStaticMesh && animatorsInTile.Count > 0)
        {
            float distance = Vector3.Distance(transform.position, cameraTransform.position);
            ToggleAnimators(distance <= disableAnimDistance);
        }
    }

    private void SetTileVisibility(bool state)
    {
        isVisible = state;
        foreach (var r in allRenderers)
        {
            if (r != null) r.enabled = state;
        }
        
        // Eğer Tile uyuyorsa animator de uyumalı
        if (!state) ToggleAnimators(false);
    }

    private void SimplifySkinnedMeshes()
    {
        SkinnedMeshRenderer[] skinnedMeshes = GetComponentsInChildren<SkinnedMeshRenderer>();

        foreach (var smr in skinnedMeshes)
        {
            GameObject obj = smr.gameObject;
            Mesh staticMesh = smr.sharedMesh;
            Material[] sharedMaterials = smr.sharedMaterials;

            if (obj.TryGetComponent<Animator>(out Animator anim)) Destroy(anim);
            Destroy(smr);

            MeshFilter filter = obj.AddComponent<MeshFilter>();
            filter.mesh = staticMesh;

            MeshRenderer renderer = obj.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = sharedMaterials;
        }
    }

    private void ToggleAnimators(bool state)
    {
        foreach (var anim in animatorsInTile)
        {
            if (anim != null && anim.enabled != state) anim.enabled = state;
        }
    }
}