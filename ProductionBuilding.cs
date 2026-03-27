using UnityEngine;

public class ProductionBuilding : MonoBehaviour
{
    [Header("Bina Kapasiteleri ve İşçiler")]
    public int workerCount = 5;
    public int machineLevel = 1;
    public int maxStorageCapacity = 100;

    [Header("Girdi (Ham Madde)")]
    public int currentRawMaterial = 0;
    public int requiredRawMaterialPerCycle = 2; // Üretim için gereken ham madde

    [Header("Özel Gereksinimler (Sadece Fabrika için vs.)")]
    public int currentPackaging = 0;
    public int requiredPackagingPerCycle = 1;

    [Header("Ahıl İçin Özel (Şimdilik pasif kalabilir)")]
    public int cowCount = 0;
    public int vetCount = 0;
    public int shepherdCount = 0;

    [Header("Çıktı (Üretilen Ürün)")]
    public int producedItems = 0;

    [Header("Üretim Ayarları")]
    public float productionTickRate = 3f; // 3 saniyede bir kontrol et
    private float timer = 0f;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= productionTickRate)
        {
            Produce();
            timer = 0f;
        }
    }

    void Produce()
    {
        // MİNİMUM KURALI: İşçi var mı? Makine var mı?
        if (workerCount <= 0 || machineLevel <= 0) return;

        // Ham madde ve Paketleme yetiyor mu? (Minimum kuralı)
        if (currentRawMaterial >= requiredRawMaterialPerCycle && 
            currentPackaging >= requiredPackagingPerCycle)
        {
            // Depoda yer var mı?
            if (producedItems < maxStorageCapacity)
            {
                // Üretim Gerçekleşiyor
                currentRawMaterial -= requiredRawMaterialPerCycle;
                currentPackaging -= requiredPackagingPerCycle;
                producedItems += (1 * machineLevel); // Makine seviyesine göre üret
                
                Debug.Log(gameObject.name + " üretim yaptı! Mevcut Ürün: " + producedItems);
            }
        }
        else
        {
            Debug.LogWarning(gameObject.name + " Üretim Durdu! Ham madde veya Paketleme yetersiz.");
        }
    }
}