using UnityEngine;

public class ProductionBuilding : MonoBehaviour
{
    public enum BuildingType { Tarla, Ahil, Firin, Fabrika, KimyaMerkezi, MeyveBahcesi, Sehir }

    [Header("Bina Kimliği")]
    public BuildingType binaTipi;
    public string binaAdi;

    [Header("Personel ve Üretim Gücü")]
    public int workerCount = 5;      
    public int machineLevel = 1;      // Kapasite seviyesi (Kapasite Birimi Ekle ile artar)
    public int upgradeLevel = 1;      // Hız seviyesi (Modernize Et ile artar)
    public int nextUpgradeCost = 750; // Bu binanın bir sonraki hız geliştirme maliyeti
    public int maxUpgradeLevel = 10;
    public int maxStorageCapacity = 100;

    [Header("Tarla Ayrıştırma (Sadece Tarla İçin)")]
    public int bugdayFirinIcin = 0;
    public int bugdayAhilIcin = 0;

    [Header("Girdi ve Çıktı")]
    public int currentRawMaterial = 0; 
    public int producedItems = 0; 
    
    [Header("Fabrika Özel Depolar")]
    public int mevcutSut;
    public int mevcutAroma;
    public int mevcutKulah;
    public bool koruyucuMaddeAktif = false; // MarketManager'ın okuduğu değişken

    [Header("Şehir (City) Müşteri Mekaniği")]
    public int population = 0;
    public float targetCustomerRate = 0f;
    public int potentialCustomers = 0;

    [Header("Zamanlama")]
    public float productionTickRate = 3f; 
    private float timer = 0f;

    void Start()
    {
        if (LogisticsManager.Instance != null) LogisticsManager.Instance.RegisterBuilding(this);
    }

    void OnDestroy()
    {
        if (LogisticsManager.Instance != null) LogisticsManager.Instance.UnregisterBuilding(this);
    }

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
        // Temel çalışma kontrolü
        if (workerCount <= 0 || machineLevel <= 0) return;
        
        // Depo doluysa hiç uğraşma
        if (producedItems >= maxStorageCapacity) return;

        // Üretkenlik Kapasitesi = İşçi Sayısı * Makine Seviyesi
        // (Örn: 5 işçi = 5 üretim şansı. 6 işçi alırsanız 6'yaçıkar! Tam istediğiniz gibi)
        int kapasite = workerCount * machineLevel;

        if (binaTipi == BuildingType.Tarla)
        {
            // Tarla en temel üreticidir. 1 Kapasite = 2 Buğday üretir.
            int uretim = kapasite * 2;
            bugdayFirinIcin += uretim / 2;
            bugdayAhilIcin += uretim / 2;
            producedItems = bugdayFirinIcin + bugdayAhilIcin;
        }
        else if (binaTipi == BuildingType.MeyveBahcesi)
        {
            // 6 İşçi = 6 Meyve Extresi ! Tam 1:1 Oran.
            producedItems += kapasite;
        }
        else if (binaTipi == BuildingType.KimyaMerkezi)
        {
            // Kimyasal biraz daha zor/yavaş üretilir. 
            producedItems += Mathf.Max(1, kapasite / 2);
        }
        else if (binaTipi == BuildingType.Ahil)
        {
            // AHIL: Süt üretir (1 İnek/Kapasite = 1 Süt). Ama yemesi için 1 Buğday gereklidir.
            int maxYapilabilecek = currentRawMaterial / 1; // Elimizde kaç buğday var?
            int gercektenUretilecek = Mathf.Min(kapasite, maxYapilabilecek); // İşçi mi yetersiz, Buğday mı?

            if (gercektenUretilecek > 0)
            {
                currentRawMaterial -= gercektenUretilecek;
                producedItems += gercektenUretilecek;
            }
        }
        else if (binaTipi == BuildingType.Firin || binaTipi == BuildingType.Sehir)
        {
            // FIRIN / ŞEHİR (Bakery): Külah üretir. 1 Külah = 2 Buğday Unu gerektirir.
            int maxYapilabilecek = currentRawMaterial / 2;
            int gercektenUretilecek = Mathf.Min(kapasite, maxYapilabilecek);

            if (gercektenUretilecek > 0)
            {
                currentRawMaterial -= gercektenUretilecek * 2;
                producedItems += gercektenUretilecek;
            }
        }
        else if (binaTipi == BuildingType.Fabrika)
        {
            // FABRİKA: 1 Dondurma = 5 Süt, 1 Külah, 2 Aroma
            int sutPorsiyonu = mevcutSut / 5;
            int kulahPorsiyonu = mevcutKulah / 1;
            int aromaPorsiyonu = mevcutAroma / 2;

            int maxYapilabilecek = Mathf.Min(sutPorsiyonu, Mathf.Min(kulahPorsiyonu, aromaPorsiyonu));
            int gercektenUretilecek = Mathf.Min(kapasite, maxYapilabilecek);

            if (gercektenUretilecek > 0)
            {
                // Malzemeleri düş
                mevcutSut -= gercektenUretilecek * 5;
                mevcutKulah -= gercektenUretilecek * 1;
                mevcutAroma -= gercektenUretilecek * 2;

                // Koruyucu hesaplaması (Kimya maddeleri varsa harca)
                for (int i = 0; i < gercektenUretilecek; i++)
                {
                    if (currentRawMaterial > 0)
                    {
                        currentRawMaterial--;
                        koruyucuMaddeAktif = true;
                    }
                    else
                    {
                        koruyucuMaddeAktif = false;
                    }
                }

                producedItems += gercektenUretilecek;
            }
        }

        // Depo taşma kontrolü (En sonda garantiye al)
        producedItems = Mathf.Clamp(producedItems, 0, maxStorageCapacity);
    }
}