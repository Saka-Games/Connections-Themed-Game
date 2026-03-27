using UnityEngine;

public class LogisticsManager : MonoBehaviour
{
    // AÇIKLAMA 1: Referanslar
    // Bu değişkenler Unity arayüzünde boş birer yuva (slot) açar. 
    // Unity'de sahnedeki Ahıl objesini tutup bu yuvaya sürüklediğimizde, 
    // bu kod artık doğrudan o objenin içindeki verilere (üretilen süt sayısına vs.) erişebilir.
    [Header("Tesis Referansları")]
    public ProductionBuilding ahil;
    public ProductionBuilding firin;
    public ProductionBuilding fabrika;

    [Header("Lojistik Ayarları")]
    public int availableTrucks = 5;
    public float transferCheckRate = 5f; // Kamyonlar her 5 saniyede bir yola çıkmayı denesin
    private float timer = 0f;

    // AÇIKLAMA 2: Update Döngüsü
    // Update fonksiyonu oyun çalıştığı sürece her saniye defalarca (FPS hızında) çalışır.
    void Update()
    {
        // Time.deltaTime, son kareden bu yana geçen süredir. Bunu toplayarak gerçek zamanlı bir kronometre elde ederiz.
        timer += Time.deltaTime; 
        
        // Eğer kronometre 5 saniyeyi (transferCheckRate) geçerse, transferleri başlat.
        if (timer >= transferCheckRate)
        {
            // ROTALAR BURADA BELİRLENİYOR
            // Kaynak: Ahıl, Hedef: Fabrika, Miktar: Tek seferde 5 birim ham madde taşı.
            TryTransferResources(ahil, fabrika, 5);
            
            // Kaynak: Fırın, Hedef: Fabrika, Miktar: 5 birim.
            TryTransferResources(firin, fabrika, 5);

            // Transfer denemeleri bittikten sonra kronometreyi sıfırla ki bir 5 saniye daha saysın.
            timer = 0f; 
        }
    }

    // AÇIKLAMA 3: Transfer Motoru
    // Bu fonksiyon parametre olarak aldığı binalar arasında güvenlik kontrolleri yaparak sayı aktarır.
    public void TryTransferResources(ProductionBuilding sourceBuilding, ProductionBuilding destinationBuilding, int transferAmount)
    {
        if (sourceBuilding.producedItems >= transferAmount) // Kaynakta yeterli mal var mı?
        {
            if (destinationBuilding.currentRawMaterial + transferAmount <= destinationBuilding.maxStorageCapacity) // Hedefte yer var mı?
            {
                if (availableTrucks > 0) // Kamyon var mı?
                {
                    // ŞARTLAR SAĞLANDI! Sayıları (verileri) bir objeden diğerine taşıyoruz.
                    availableTrucks--; 
                    sourceBuilding.producedItems -= transferAmount; 
                    destinationBuilding.currentRawMaterial += transferAmount; 
                    availableTrucks++; // İşlem anında bittiği için kamyonu hemen boşa çıkarıyoruz.

                    Debug.Log($"KAMYON YOLA ÇIKTI! {sourceBuilding.name}'dan {destinationBuilding.name}'a {transferAmount} ürün taşındı.");
                }
                else { Debug.LogWarning("Kamyon yetersiz!"); }
            }
            else { Debug.LogWarning(destinationBuilding.name + " deposu dolu, mal kabul etmiyor!"); }
        }
    }
}