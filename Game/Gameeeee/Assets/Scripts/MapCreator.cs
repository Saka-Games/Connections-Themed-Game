using UnityEngine;
using System.Collections.Generic;

public class MapCreator : MonoBehaviour
{
    [Header("Grid Ayarları")]
    public int width = 9;
    public int height = 9;
    public float cellSize = 500f;

    [Header("Hiyerarşi Düzeni")]
    public Transform tilesParent;

    [Header("Tile Prefabları")]
    [Tooltip("Dizideki tüm prefablar rastgele dağıtılır.")]
    public GameObject[] tilePrefabs;

    // Koordinat çakışmasını engellemek için veri tabanı
    public Dictionary<Vector2Int, Tile> gridData = new Dictionary<Vector2Int, Tile>();

    void Start()
    {
        if (tilesParent == null) tilesParent = this.transform;
        // GenerateRandomMap(); oyun açılır açılmaz değil, "Yeni Oyun" denildiğinde çağrılacak!
    }

    public void GenerateRandomMap()
    {
        // 1. ÖNCE TEMİZLİK: Sahnedeki tüm eski objeleri hiyerarşiden tamamen sil
        ClearMap();

        if (tilePrefabs == null || tilePrefabs.Length == 0)
        {
            Debug.LogError("⚠️ MapCreator: Tile Prefabs dizisi boş!");
            return;
        }

        // 2. GRID OLUŞTURMA
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Vector2Int coords = new Vector2Int(x, z);

                // Eğer bu koordinatta zaten bir şey varsa (çakışma önleyici) atla
                if (gridData.ContainsKey(coords)) continue;

                // Rastgele prefab seçimi
                GameObject randomPrefab = tilePrefabs[Random.Range(0, tilePrefabs.Length)];

                // Koordinat hesaplama (Pivot kaymalarına karşı tam merkezleme)
                Vector3 spawnPos = new Vector3(x * cellSize, 0, z * cellSize);

                // Tile Oluşturma
                GameObject newTileObj = Instantiate(randomPrefab, spawnPos, Quaternion.identity, tilesParent);
                
                // İsimlendirme (Hiyerarşide karışıklığı önler)
                newTileObj.name = $"Tile_{x}_{z}_[{randomPrefab.name}]";

                Tile tileScript = newTileObj.GetComponent<Tile>();
                if (tileScript != null)
                {
                    tileScript.gridX = x;
                    tileScript.gridZ = z;
                    tileScript.cellSize = cellSize;
                    tileScript.InitializeTile();

                    gridData.Add(coords, tileScript);
                }
            }
        }
        Debug.Log($"<color=green>✔ {width}x{height} Harita Başarıyla Oluşturuldu!</color>");
    }

    // Eski tile'ları güvenli bir şekilde silen yardımcı fonksiyon
    private void ClearMap()
    {
        gridData.Clear();

        // Hiyerarşideki tüm çocukları sondan başa doğru sil (Index kaymasını önler)
        for (int i = tilesParent.childCount - 1; i >= 0; i--)
        {
            GameObject child = tilesParent.GetChild(i).gameObject;
            if (Application.isPlaying) 
                Destroy(child);
            else 
                DestroyImmediate(child);
        }
    }

    public void ReplaceTile(int x, int z, GameObject newPrefab)
    {
        Vector2Int coords = new Vector2Int(x, z);
        
        if (gridData.ContainsKey(coords))
        {
            if (Application.isPlaying) Destroy(gridData[coords].gameObject);
            else DestroyImmediate(gridData[coords].gameObject);
            gridData.Remove(coords);
        }

        Vector3 spawnPos = new Vector3(x * cellSize, 0, z * cellSize);
        GameObject newObj = Instantiate(newPrefab, spawnPos, Quaternion.identity, tilesParent);
        newObj.name = $"Tile_{x}_{z}_[Upgraded]";
        
        Tile newScript = newObj.GetComponent<Tile>();
        if (newScript != null)
        {
            newScript.gridX = x;
            newScript.gridZ = z;
            newScript.cellSize = cellSize;
            newScript.InitializeTile();
            gridData.Add(coords, newScript);
        }
    }
}