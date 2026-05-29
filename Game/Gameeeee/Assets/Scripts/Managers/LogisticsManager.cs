using UnityEngine;
using System.Collections.Generic;

public class LogisticsManager : MonoBehaviour
{
    public static LogisticsManager Instance;

    private List<ProductionBuilding> buildings = new List<ProductionBuilding>();
    private float updateInterval = 2f; 
    private float timer = 0f;

    [Header("Görsel Ayarlar")]
    public GameObject truckPrefab;

#if UNITY_EDITOR
    [ContextMenu("Kamyon Modelini Bul (Otomatik)")]
    public void AutoFindTruck()
    {
        string path = "Assets/Ready to use Assets/SimplePoly City - Low Poly Assets/Prefab/Vehicles/Vehicle with Separated Wheels/Vehicle_Truck_color03_separate.prefab";
        truckPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (truckPrefab != null) Debug.Log("<color=green>[LOGISTICS]:</color> Kamyon prefabı başarıyla atandı.");
        else Debug.LogWarning("<color=red>[LOGISTICS]:</color> Kamyon prefabı belirtilen klasörde bulunamadı!");
    }
#endif

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    public void RegisterBuilding(ProductionBuilding b)
    {
        if (!buildings.Contains(b)) buildings.Add(b);
    }

    public void UnregisterBuilding(ProductionBuilding b)
    {
        if (buildings.Contains(b)) buildings.Remove(b);
    }

    public List<ProductionBuilding> GetBuildings() => buildings;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= updateInterval)
        {
            ProcessLogistics();
            timer = 0f;
        }
    }

    private void ProcessLogistics()
    {
        // Önce yok edilmiş binaları listeden temizle (NullReference koruması)
        buildings.RemoveAll(b => b == null);

        foreach (var consumer in buildings)
        {
            if (consumer == null) continue;
            if (IsConsumer(consumer) && consumer.currentRawMaterial < consumer.maxStorageCapacity)
            {
                TrySupplyConsumer(consumer);
            }
        }
    }

    private bool IsConsumer(ProductionBuilding b)
    {
        return b.binaTipi == ProductionBuilding.BuildingType.Ahil || 
               b.binaTipi == ProductionBuilding.BuildingType.Firin || 
               b.binaTipi == ProductionBuilding.BuildingType.Sehir ||
               b.binaTipi == ProductionBuilding.BuildingType.Fabrika;
    }

    private void TrySupplyConsumer(ProductionBuilding consumer)
    {
        if (NetworkManager.Instance == null) return;

        foreach (var supplier in buildings)
        {
            if (supplier == null) continue;
            if (IsSupplierFor(supplier, consumer) && supplier.producedItems > 0)
            {
                // 1. Önce doğrudan merkezden aramayı dene
                List<Vector3> path = NetworkManager.Instance.GetPath(supplier.transform.position, consumer.transform.position);
                
                // 2. Eğer yol bulunamazsa, DAİMİ ID BAĞLANTISINI kontrol et (Nakliye Garantisi!)
                if (path == null)
                {
                    Tile sTile = supplier.GetComponentInParent<Tile>();
                    Tile cTile = consumer.GetComponentInParent<Tile>();
                    if (sTile != null && cTile != null && NetworkManager.Instance.AreDirectlyConnected(sTile.tileID, cTile.tileID))
                    {
                        // Yol gridde kopuk olsa bile, KAYITLI VİZÜEL YOLU çek!
                        path = NetworkManager.Instance.GetDirectPath(sTile.tileID, cTile.tileID);
                        
                        // Eğer hala null ise (çok eski bir kayıt olabilir), kuş uçuşu fallback'e dön
                        if (path == null) path = new List<Vector3> { supplier.transform.position, consumer.transform.position };
                         // Debug.Log($"<color=orange>[LOGISTICS]:</color> {supplier.tileID} -> {consumer.tileID} DAİMİ VİZÜEL hattan sevkiyat başladı.");
                    }
                }

                // 3. Eğer hala yol yoksa, Snap Point aramayı dene (Normal Fuzzy arama)
                if (path == null)
                {
                    Vector3 sPos = FindNearestNetworkNode(supplier.transform.position);
                    Vector3 cPos = FindNearestNetworkNode(consumer.transform.position);
                    path = NetworkManager.Instance.GetPath(sPos, cPos);
                    
                    if (path != null)
                    {
                        path.Insert(0, supplier.transform.position);
                        path.Add(consumer.transform.position);
                    }
                }

                if (path != null)
                {
                    // Nakliyeyi Başlat
                    int amountToTransfer = Mathf.Min(supplier.producedItems, 20); 
                    StartTransport(supplier, consumer, path, amountToTransfer);
                    return; 
                }
            }
        }
    }

    private Vector3 FindNearestNetworkNode(Vector3 worldPos)
    {
        if (NetworkManager.Instance == null) return worldPos;
        float halfCS = (RoadManager.Instance != null) ? RoadManager.Instance.cellSize * 0.5f : 250f;

        // Merkezin 4 tarafındaki Snap noktalarına bak (En yakın yolu bul)
        Vector3[] directions = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
        foreach (var dir in directions)
        {
            Vector3 candidate = worldPos + dir * halfCS;
            if (NetworkManager.Instance.IsPathExists(candidate, candidate)) // Node devrede mi?
                return candidate;
        }
        return worldPos; // Bulamazsa merkezi döndür (Fallback)
    }
    private bool IsSupplierFor(ProductionBuilding supplier, ProductionBuilding consumer)
    {
        // TARLA -> Ahıl (Buğday), Fırın (Buğday), Şehir (Buğday)
        if (supplier.binaTipi == ProductionBuilding.BuildingType.Tarla)
        {
            return consumer.binaTipi == ProductionBuilding.BuildingType.Ahil || 
                   consumer.binaTipi == ProductionBuilding.BuildingType.Firin ||
                   consumer.binaTipi == ProductionBuilding.BuildingType.Sehir;
        }
        
        // AHIL -> Fabrika (Süt)
        if (supplier.binaTipi == ProductionBuilding.BuildingType.Ahil && 
            consumer.binaTipi == ProductionBuilding.BuildingType.Fabrika) return true;

        // MEYVE BAHÇESİ -> Fabrika (Aroma)
        if (supplier.binaTipi == ProductionBuilding.BuildingType.MeyveBahcesi && 
            consumer.binaTipi == ProductionBuilding.BuildingType.Fabrika) return true;

        // FIRIN -> Fabrika (Külah)
        if (supplier.binaTipi == ProductionBuilding.BuildingType.Firin && 
            consumer.binaTipi == ProductionBuilding.BuildingType.Fabrika) return true;

        // FABRİKA -> Şehir (Dondurma)
        if (supplier.binaTipi == ProductionBuilding.BuildingType.Fabrika && 
            consumer.binaTipi == ProductionBuilding.BuildingType.Sehir) return true;

        return false;
    }

    private void StartTransport(ProductionBuilding supplier, ProductionBuilding consumer, List<Vector3> path, int amount)
    {
        // Kaynağı tarladan DÜŞ (Kamyona yüklendi)
        supplier.producedItems -= amount;
        
        // Tarladaki iç sayçları da temizleyelim
        if (supplier.binaTipi == ProductionBuilding.BuildingType.Tarla)
        {
            supplier.bugdayAhilIcin = Mathf.Max(0, supplier.bugdayAhilIcin - (amount / 2));
            supplier.bugdayFirinIcin = Mathf.Max(0, supplier.bugdayFirinIcin - (amount / 2));
        }

        GameObject truckObj = new GameObject("Grain_Truck");
        TransportUnit truck = truckObj.AddComponent<TransportUnit>();
        truck.Initialize(path, consumer, amount, truckPrefab);
        
        // Hangi malzeme geldiğini belirle (Fabrika alt depolarına yönlendirme için)
        truck.cargoType = GetCargoType(supplier);
        
        Debug.Log($"<color=green>[LOGISTICS]:</color> {supplier.binaAdi} -> {consumer.binaAdi}: {amount} adet sevkiyat başladı.");
    }

    // Tedarikçinin ne ürettiğini belirle
    private string GetCargoType(ProductionBuilding supplier)
    {
        switch (supplier.binaTipi)
        {
            case ProductionBuilding.BuildingType.Ahil: return "sut";
            case ProductionBuilding.BuildingType.MeyveBahcesi: return "aroma";
            case ProductionBuilding.BuildingType.Firin: return "kulah";
            case ProductionBuilding.BuildingType.Fabrika: return "dondurma";
            default: return "hammadde";
        }
    }
}