using UnityEngine;

// 1. ADIM: Orchard Türünü Buraya Ekledik
public enum TileType { Meadow, Farm, Factory, City, Orchard, Pasture, Road }

[ExecuteAlways]
public class Tile : MonoBehaviour
{
    [Header("Tile Identity")]
    public string tileID;
    public TileType type;

    [Header("Grid Info")]
    public int gridX;
    public int gridZ;
    public float cellSize = 500f;
    public bool isConnected = false;

    // Oyun her başladığında (Play) yönler rastgeleleşir
    private void Awake()
    {
        if (Application.isPlaying)
        {
            RandomizeRotation();
            InitializeTile();
        }
    }

    // Editor içinde (Update ve OnValidate) her şeyin güncel kalmasını sağlar
    private void Update()
    {
        if (!Application.isPlaying) InitializeTile();
        else UpdateVisualStatus();
    }

    private void UpdateVisualStatus()
    {
        // Örn: Bağlantılı olmayan binaları hafifçe karartalım veya bir lamba yakalım
        // Şimdilik sadece debug için renk değişimi yapabiliriz
        // (Gerçek projede bir lamba objesi açıp kapatmak daha iyidir)
    }

    private void Start()
    {
        if (Application.isPlaying)
        {
            // OYUN BÖLÜMÜ: Eğer bu objeye (veya prefaba) ProductionBuilding scripti atanmamışsa, KENDİ KENDİNE eklesin!
            AttachProductionBuildingIfNeeded();
        }
    }

    private void OnValidate()
    {
        InitializeTile();
    }

    private void AttachProductionBuildingIfNeeded()
    {
        // Yalnızca düz/zemin boş alanlar ve YOLLAR bina (production) scripti taşımaz
        if (type == TileType.Meadow || type == TileType.Road) return;
        
        // Acaba birisi (belki editor) halihazırda script atmış mı?
        ProductionBuilding pb = GetComponent<ProductionBuilding>();
        if (pb == null) pb = GetComponentInChildren<ProductionBuilding>();

        // Eğer yoksa (Kullanıcı prefablara eklemeyi unuttuysa), SİSTEM KENDİ OLUŞTURSUN!
        if (pb == null)
        {
            pb = gameObject.AddComponent<ProductionBuilding>();
            
            // Tile tipine göre doğru bina ekonomisini/tipini ayarla!
            switch (type)
            {
                case TileType.Farm: 
                    pb.binaTipi = ProductionBuilding.BuildingType.Tarla; 
                    pb.binaAdi = "Büyük Tarla";
                    break;
                case TileType.Factory: 
                    pb.binaTipi = ProductionBuilding.BuildingType.Fabrika; 
                    pb.binaAdi = "Saka Dondurma Fabrikası";
                    break;
                case TileType.Orchard: 
                    pb.binaTipi = ProductionBuilding.BuildingType.MeyveBahcesi; 
                    pb.binaAdi = "Lüks Meyve Bahçesi";
                    break;
                case TileType.Pasture: 
                    pb.binaTipi = ProductionBuilding.BuildingType.Ahil; 
                    pb.binaAdi = "İnek Çiftliği";
                    break;
                case TileType.City: 
                    pb.binaTipi = ProductionBuilding.BuildingType.Sehir; 
                    pb.binaAdi = "Şehir (Yerleşim)";
                    pb.population = Random.Range(500, 1501); // 500-1500 arası
                    pb.targetCustomerRate = Random.Range(30f, 70f); // %30-%70 arası
                    pb.potentialCustomers = Mathf.RoundToInt(pb.population * (pb.targetCustomerRate / 100f));
                    break;
            }
        }
    }

    public void InitializeTile()
    {
        AssignID();
        AssignTypeFromName();
        SnapToGrid();
        SetupCollider();
        ApplyRoadVisual();
        
        #if UNITY_EDITOR
        if (!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }

    private void RandomizeRotation()
    {
        // 0, 90, 180, 270 açılarından birini rastgele seçer
        float[] angles = { 0f, 90f, 180f, 270f };
        float randomY = angles[Random.Range(0, angles.Length)];
        transform.localEulerAngles = new Vector3(0, randomY, 0);
    }

    private void AssignID()
    {
        if (string.IsNullOrEmpty(tileID))
        {
            tileID = System.Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
        }
    }

    private void AssignTypeFromName()
    {
        string n = name.ToLower();

        // 2. ADIM: İsimde "orchard" geçiyorsa Orchard yapıyoruz
        if (n.Contains("pasture")) type = TileType.Pasture;
        else if (n.Contains("orchard")) type = TileType.Orchard;
        else if (n.Contains("farm")) type = TileType.Farm;
        else if (n.Contains("factory")) type = TileType.Factory;
        else if (n.Contains("city")) type = TileType.City;
        else if (n.Contains("road")) type = TileType.Road;
        else type = TileType.Meadow;
    }

    private void ApplyRoadVisual()
    {
        if (type != TileType.Road) return;
        
        // Şık bir Asphalt Siyahı / Koyu gri renk
        Color roadGray = new Color(0.15f, 0.15f, 0.15f, 1f);

        foreach (Transform child in transform)
        {
            if (child.name.ToLower().Contains("ground"))
            {
                Renderer r = child.GetComponent<Renderer>();
                if (r != null)
                {
                    // Material kopyasını alıp boyuyoruz (Instance oluşur)
                    r.material.color = roadGray;
                }
            }
        }
    }

    public void SnapToGrid()
    {
        if (this == null || transform == null) return;

        // Editor'de elle çevirince 90'ın katlarına mıknatıslar
        Vector3 currentRot = transform.localEulerAngles;
        float snappedY = Mathf.Round(currentRot.y / 90f) * 90f;
        transform.localEulerAngles = new Vector3(0, snappedY, 0);

        Vector3 targetPos = new Vector3(gridX * cellSize, 0, gridZ * cellSize);

        Transform validGround = null;
        foreach (Transform child in transform)
        {
            if (child.name.ToLower().Contains("ground"))
            {
                MeshRenderer rend = child.GetComponent<MeshRenderer>();
                if (rend != null && rend.bounds.size.x > 400f) 
                {
                    validGround = child;
                    break;
                }
            }
        }

        if (validGround != null)
        {
            Vector3 offset = validGround.position - transform.position;
            offset.y = 0; 
            transform.position = targetPos - offset;
        }
        else
        {
            transform.position = targetPos;
        }
    }

    private void SafeDestroy(Object obj)
    {
        if (obj == null) return;
        if (Application.isPlaying)
        {
            Destroy(obj);
        }
        else
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (obj != null) UnityEngine.Object.DestroyImmediate(obj, true);
            };
#else
            DestroyImmediate(obj, true);
#endif
        }
    }

    private void SetupCollider()
    {
        // 1. Root/Parent objesindeki eski hatalı/kayık BoxCollider'ları temizleyelim (Varsa)
        BoxCollider oldCol = GetComponent<BoxCollider>();
        if (oldCol != null) SafeDestroy(oldCol);

        BoxCollider childOldCol = GetComponentInChildren<BoxCollider>();
        if (childOldCol != null && childOldCol.gameObject == gameObject) SafeDestroy(childOldCol);

        // 2. Senin o mükemmel Snapping için kaydırdığın asıl "Ground" parçasını bulalım
        Transform validGround = null;
        foreach (Transform child in transform)
        {
            if (child.name.ToLower().Contains("ground"))
            {
                validGround = child;
                break;
            }
        }

        // 3. Collider'ı (Tıklanabilir Alanı) sadece ve sadece "Ground" üzerine ekleyelim! 
        // Böylece Ground nerdeyse, tıklama alanı DOĞRUDAN orası olacaktır. Ne merkez şaşar ne kayma yaşanır.
        if (validGround != null)
        {
            // O devasa BoxCollider'ın Scale hatasını siliyoruz! Görünmez kutular komşulara taşmayacak.
            BoxCollider groundCol = validGround.GetComponent<BoxCollider>();
            if (groundCol != null) SafeDestroy(groundCol);

            // GÖRSEL (Mesh) NEYSE TIKLAMA ALANI O OLSUN:
            MeshCollider mc = validGround.GetComponent<MeshCollider>();
            if (mc == null) mc = validGround.gameObject.AddComponent<MeshCollider>();
            
            // Eğer isterseniz Collider'ı katı/gerçek modeliyle bırakıyoruz ki FAREMİZ %100 GÖRDÜĞÜMÜZ YERE tıkasın.
        }
    }

    // --- GÖRSEL MERKEZ FIX: Pivot yerine 'Ground' (Zemin) merkezini baz alıyoruz ---
    public Vector3 GetVisualCenter()
    {
        foreach (Transform child in transform)
        {
            if (child.name.ToLower().Contains("ground"))
            {
                return child.position;
            }
        }
        return transform.position;
    }
}