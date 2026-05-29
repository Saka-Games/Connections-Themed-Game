using UnityEngine;

public class MarketManager : MonoBehaviour
{
    public static MarketManager Instance;

    [Header("Referanslar")]
    public ProductionBuilding fabrika;

    [Header("Ekonomi Ayarları")]
    public float satisPeriyodu = 5f;
    public int standartBirimFiyat = 50;  // Doğal dondurma fiyatı
    public int ucuzBirimFiyat = 30;     // Koruyuculu dondurma fiyatı
    
    [Header("Pazar & Nüfus Verileri")]
    [Range(0, 100)]
    public float oyuncuPayi = 15f;
    [Range(0, 100)]
    public float musteriligisi = 50f;  // Halkın markaya olan güveni %50 ile başlar
    public float bolgeNufusu = 10000f;  // Satış potansiyelini belirleyen çarpan
    public bool oyunBittiMi = false;

    private float timer = 0f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Update()
    {
        if (oyunBittiMi) return;

        timer += Time.deltaTime;
        if (timer >= satisPeriyodu)
        {
            SatisGerceklestir();
            timer = 0f;
        }

        // Müşteri ilgisi çok düşerse pazar payı kendiliğinden erimeye başlar (Boykot etkisi)
        if (musteriligisi < 20f && oyuncuPayi > 0)
        {
            oyuncuPayi -= 0.05f * Time.deltaTime;
        }
    }

    void SatisGerceklestir()
    {
        if (fabrika != null && fabrika.producedItems >= 1)
        {
            // 1. Üretim tipini fabrikadan kontrol et
            bool koruyucuKullanildi = fabrika.koruyucuMaddeAktif;
            
            // 2. Fiyat ve İlgi Hesabı
            int aktifFiyat = koruyucuKullanildi ? ucuzBirimFiyat : standartBirimFiyat;
            
            // Koruyucu madde ilgiyi düşürür, doğal üretim ise çok yavaş geri kazandırır
            if (koruyucuKullanildi)
            {
                musteriligisi = Mathf.Max(0, musteriligisi - 1.5f); // Sert düşüş
            }
            else
            {
                musteriligisi = Mathf.Min(100, musteriligisi + 0.2f); // Yavaş toparlanma
            }

            // 3. Satış Miktarı Hesabı (İlgi düştükçe satış oranı azalır)
            // Yanılsama: Nüfusun yüzde kaçı dondurma alıyor?
            float satisOrani = musteriligisi / 100f; 
            int satilacakMiktar = Mathf.FloorToInt(fabrika.producedItems * satisOrani);

            if (satilacakMiktar <= 0 && fabrika.producedItems > 0)
            {
                Debug.LogWarning("<color=red>UYARI:</color> Müşteri ilgisi çok düşük, kimse dondurma almıyor!");
                return;
            }

            int toplamKazanc = satilacakMiktar * aktifFiyat;
            fabrika.producedItems -= satilacakMiktar; 

            // 4. Kasayı Güncelle
            if (UIManager.Instance != null)
            {
                UIManager.Instance.oyuncuParasi += toplamKazanc;
                string renk = koruyucuKullanildi ? "yellow" : "green";
                Debug.Log($"<color={renk}>SATIŞ:</color> {satilacakMiktar} adet dondurma ({aktifFiyat}$). İlgi: %{musteriligisi:F1}");
            }
            
            // Pazar payı artışı (İlgi ile doğru orantılı)
            if (oyuncuPayi < 100f) oyuncuPayi += 0.5f * satisOrani;
        }
    }

    // VanManager veya diğer dış satışlar için
    public void SehirSatisiniGuncelle(int miktar)
    {
        // Şehir satışları da genel müşteri ilgisinden etkilenir
        float etkiliMiktar = miktar * (musteriligisi / 100f);
        UIManager.Instance.oyuncuParasi += Mathf.RoundToInt(etkiliMiktar);
        
        if (oyuncuPayi < 100f) oyuncuPayi += 0.1f; 
    }
}