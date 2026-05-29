using UnityEngine;

public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance;

    [Header("Birim Maliyetler")]
    public int isciMaliyeti = 200;
    public int makineSatinAlmaMaliyeti = 500;   // Yeni bir makine eklemek
    public int makineGelistirmeMaliyeti = 750; // Mevcut makineleri hızlandırmak
    public int yeniFabrikaMaliyeti = 5000;

    [Header("Hammadde BİRİM Satış Fiyatları")]
    public int sutBirimFiyat = 2;     // 5 Süt * 2$ = 10$ Maliyet
    public int kulahBirimFiyat = 3;   // 1 Külah * 3$ = 3$ Maliyet
    public int meyveBirimFiyat = 6;   // 2 Aroma * 6$ = 12$ Maliyet
    public int bugdayBirimFiyat = 1;  // 1 Buğday * 1$ = En temel hammade (Tarladan satış)
    // TOPLAM Ham Maliyet: 25$. Fabrikada işlenmiş Dondurma İse: 50$ (Satış değerine göre %100 Katma değer/Kâr)

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // OYUNCU NOTU FIX: Unity Inspector penceresi, eski kayıtlı ayarları koddan üstün tuttuğu için 
        // fiyatların değişmediğini görmenizin sebebi buydu. Bunu kodla ZORLA eziyoruz:
        sutBirimFiyat = 2;
        kulahBirimFiyat = 3;
        meyveBirimFiyat = 6;
    }

    // --- 1. İŞÇİ SATIN ALMA ---
    public void IsciAl(ProductionBuilding bina)
    {
        if (bina == null) return;
        if (UIManager.Instance.oyuncuParasi >= isciMaliyeti)
        {
            UIManager.Instance.oyuncuParasi -= isciMaliyeti;
            bina.workerCount++;
            Debug.Log($"{bina.binaAdi}: İşçi alındı. Toplam: {bina.workerCount}");
        }
        else
        {
            UIManager.Instance.ShowWarning("İşçi almak için yeterli paranız yok!");
        }
    }

    // --- 2. YENİ MAKİNE SATIN ALMA (ADET) ---
    // Bu fonksiyon "Fırın Sayısı: 2" kısmını artırır.
    public void MakineSatinAl(ProductionBuilding bina)
    {
        if (bina == null) return;
        if (UIManager.Instance.oyuncuParasi >= makineSatinAlmaMaliyeti)
        {
            UIManager.Instance.oyuncuParasi -= makineSatinAlmaMaliyeti;
            bina.machineLevel++; // Makine sayısını artırır
            bina.maxStorageCapacity += 50; // YENİ: Her yeni kapasite birimi depoyu 50 birim büyütür
            
            // Tycoon Dengesi: Her yeni makinede fiyat %20 artar
            makineSatinAlmaMaliyeti = Mathf.RoundToInt(makineSatinAlmaMaliyeti * 1.2f);
            Debug.Log($"{bina.binaAdi}: Yeni makine ve depo kapasitesi eklendi. Toplam: {bina.machineLevel}, Kapasite: {bina.maxStorageCapacity}");
        }
        else
        {
            UIManager.Instance.ShowWarning("Kapasite birimi eklemek için yeterli paranız yok!");
        }
    }

    // --- 3. MAKİNE GELİŞTİRME (SEVİYE/HIZ) ---
    // Bu fonksiyon "Seviye: 1" kısmını artırır ve üretim süresini kısaltır.
    public void MakineGelistir(ProductionBuilding bina)
    {
        if (bina == null) return;

        // SEVİYE KONTROLÜ (Maksimum 10)
        if (bina.upgradeLevel >= bina.maxUpgradeLevel)
        {
            Debug.LogWarning($"{bina.binaAdi}: Zaten maksimum seviyede (10)!");
            return;
        }

        if (UIManager.Instance.oyuncuParasi >= bina.nextUpgradeCost)
        {
            UIManager.Instance.oyuncuParasi -= bina.nextUpgradeCost;
            
            // Seviye atla
            bina.upgradeLevel++;

            // Üretim hızını artırmak için 'productionTickRate' süresini düşürüyoruz
            // Formül: Başlangıç 3.0s, her seviyede 0.2s düşer (Lvl 10 = 1.2s)
            bina.productionTickRate = Mathf.Max(0.2f, 3.0f - (bina.upgradeLevel - 1) * 0.2f);
            
            // Bu binaya ÖZEL maliyeti artır (%40)
            bina.nextUpgradeCost = Mathf.RoundToInt(bina.nextUpgradeCost * 1.4f);
            
            Debug.Log($"{bina.binaAdi}: Modernize edildi! Yeni Seviye: {bina.upgradeLevel}, Yeni Hız: {bina.productionTickRate:F1}s");
        }
        else
        {
            UIManager.Instance.ShowWarning("Ekipmanı modernize etmek için paranız yetersiz!");
        }
    }

    // --- 4. HAMMADDE YIĞININI (TÜMÜNÜ) SATIŞI ---
    // Depodaki tüm mallarını BirimFiyat ile çarpıp anında satar
    public void BugdaySat(ProductionBuilding tarla)
    {
        if (tarla != null && tarla.producedItems > 0)
        {
            int kazanc = tarla.producedItems * bugdayBirimFiyat;
            
            // Tüm buğday yığınını sıfırla
            tarla.producedItems = 0;
            tarla.bugdayAhilIcin = 0;
            tarla.bugdayFirinIcin = 0;
            
            UIManager.Instance.oyuncuParasi += kazanc;
            Debug.Log($"<color=cyan>[EKONOMİ]:</color> Tarladaki Tüm Buğdaylar satıldı. Kazanılan: {kazanc}$");
        }
    }

    public void SutSat(ProductionBuilding ahil)
    {
        if (ahil != null && ahil.producedItems > 0)
        {
            int kazanc = ahil.producedItems * sutBirimFiyat;
            ahil.producedItems = 0;
            UIManager.Instance.oyuncuParasi += kazanc;
            Debug.Log($"<color=cyan>[EKONOMİ]:</color> Tüm Sütler satıldı. Kazanılan: {kazanc}$");
        }
    }

    public void KulahSat(ProductionBuilding firin)
    {
        if (firin != null && firin.producedItems > 0)
        {
            int kazanc = firin.producedItems * kulahBirimFiyat;
            firin.producedItems = 0;
            UIManager.Instance.oyuncuParasi += kazanc;
            Debug.Log($"<color=cyan>[EKONOMİ]:</color> Tüm Külahlar satıldı. Kazanılan: {kazanc}$");
        }
    }
    
    public void MeyveSat(ProductionBuilding meyveBahcesi)
    {
        if (meyveBahcesi != null && meyveBahcesi.producedItems > 0)
        {
            int kazanc = meyveBahcesi.producedItems * meyveBirimFiyat;
            meyveBahcesi.producedItems = 0;
            UIManager.Instance.oyuncuParasi += kazanc;
            
            // DEVLOG: Satış işlemini konsoldan takip et
            Debug.Log($"<color=magenta>[EKONOMİ]:</color> Tüm Meyve Extreleri satıldı. Kazanılan: {kazanc}$");
        }
        else
        {
            Debug.LogWarning("[EKONOMİ]: Satılacak ürün yok!");
        }
    }
    // --- 5. YENİ BİNA/FABRİKA SATIN ALMA ---
    public void FabrikaSatinAl(GameObject pasifFabrika)
    {
        if (pasifFabrika != null && UIManager.Instance.oyuncuParasi >= yeniFabrikaMaliyeti)
        {
            if (!pasifFabrika.activeSelf)
            {
                UIManager.Instance.oyuncuParasi -= yeniFabrikaMaliyeti;
                pasifFabrika.SetActive(true);
            }
        }
        else if (UIManager.Instance.oyuncuParasi < yeniFabrikaMaliyeti)
        {
            UIManager.Instance.ShowWarning("Yeni tesis satın almak için paranız yetersiz!");
        }
    }
}