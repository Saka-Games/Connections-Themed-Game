using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Sahneler Arası Aktarım")]
    public static bool kayittanDevamEt = false; // Ana Menüden gelen tıklandı verisi

    [Header("UI Panelleri")]
    public GameObject oyunHUD; // Para, pazar payı vb. duran ana arayüz

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f; // Artık oyun sahnesi başladığı gibi zaman aksın
    }

    void Start()
    {
        if (oyunHUD != null) oyunHUD.SetActive(true);
        
        // Ana Menüden (MainMenuController'dan) gelen duruma göre başla
        if (kayittanDevamEt)
        {
            KayitliOyunYukle(); // Kullanıcı Ana Menüde Devam Et e basmış!
        }
        else
        {
            YeniOyunBaslat(); // Kullanıcı Ana Menüde Yeni Oyun'a basmış!
        }
    }

    public void YeniOyunBaslat()
    {
        // Harita oyuna başlandığı an oluşturulsun!
        MapCreator mc = FindAnyObjectByType<MapCreator>();
        if (mc != null) mc.GenerateRandomMap();

        if(oyunHUD != null) oyunHUD.SetActive(true);

        // Kamerayı menüden ana pozisyona kaydırarak geçişi başlat
        if (Camera.main != null)
        {
            CameraController camCtrl = Camera.main.GetComponent<CameraController>();
            if (camCtrl != null)
            {
                camCtrl.OyunaGecisYap();
            }
        }

        // İHALELERİ BURADA TETİKLİYORUZ
        if(IhaleManager.Instance != null)
        {
            IhaleManager.Instance.IhaleleriAktifEt();
        }
    }

    public void KayitliOyunYukle()
    {
        Debug.Log("<color=yellow>[SİSTEM]:</color> Kayıtlı veriler yükleniyor...");
        
        // PlayerPrefs kullanarak basit bir kayıt sistemi:
        if (PlayerPrefs.HasKey("KayitliPara"))
        {
            UIManager.Instance.oyuncuParasi = PlayerPrefs.GetInt("KayitliPara");
            // Diğer binaların işçi sayılarını da buradan yükleyebiliriz
            
            if(oyunHUD != null) oyunHUD.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Kayıtlı oyun bulunamadı!");
        }
    }
    public void OyunuKaydet()
{
    PlayerPrefs.SetInt("KayitliPara", UIManager.Instance.oyuncuParasi);
    PlayerPrefs.Save();
    Debug.Log("<color=cyan>[DEVLOG]:</color> Oyun başarıyla kaydedildi.");
}
    public void OyundanCik()
    {
        Application.Quit();
        Debug.Log("Oyun kapatıldı.");
    }
}