using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class IhaleManager : MonoBehaviour
{
    public static IhaleManager Instance;

    public enum IhaleScreen { NONE, QUESTION, BIDDING, RESULT }
    private IhaleScreen currentScreen = IhaleScreen.NONE;

    [Header("İhale Ayarları")]
    public float kararSuresi = 15f;
    public float oyuncuTeklifi = 0f;
    public float rakipEnDusukTeklif = 0f;
    private string suAnkiLiderRakip = "";
    private string[] rakipler = { "Algitla", "Tennis", "Manda" };
    
    private bool ihaleAktifMi = false;
    private bool oyunBasladiMi = false; 
    private float ihaleZamanlayici = 0f;
    private string sonucMesaji = "";

    // UI Styles
    private GUIStyle windowStyle;
    private GUIStyle labelStyle;
    private GUIStyle buttonStyle;
    private GUIStyle titleStyle;
    private bool stylesInitialized = false;

    private void Awake() => Instance = this;

    public void IhaleleriAktifEt()
    {
        oyunBasladiMi = true;
        InvokeRepeating("IhaleSorgusuBaslat", 30f, 90f); 
        Debug.Log("<color=green>[İHALE SİSTEMİ]:</color> Zamanlayıcı aktifleşti.");
    }

    void IhaleSorgusuBaslat()
    {
        if (oyunBasladiMi && !ihaleAktifMi)
        {
            currentScreen = IhaleScreen.QUESTION;
            ihaleAktifMi = true;
            Debug.Log("<color=cyan>[İHALE]:</color> Yeni ihale sorusu ekrana geldi (OnGUI).");
        }
    }

    private void OnGUI()
    {
        if (currentScreen == IhaleScreen.NONE) return;

        if (!stylesInitialized) InitializeStyles();

        // --- RESPONSIVE SCALING (1080p Base) ---
        float scale = Screen.height / 1080f;
        if (scale < 0.1f) scale = 1f;
        
        Matrix4x4 oldMat = GUI.matrix;
        float virtualWidth = Screen.width / scale;
        float virtualHeight = 1080f;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1));

        // Pencere Boyutu (Sanal 1080p üzerinde)
        float w = 600f;
        float h = 400f;
        Rect windowRect = new Rect((virtualWidth - w) * 0.5f, (virtualHeight - h) * 0.5f, w, h);

        GUI.Window(99, windowRect, DrawIhaleWindow, "", windowStyle);
        GUI.matrix = oldMat;
    }

    private void InitializeStyles()
    {
        // SAHNE GEÇİŞİ FIX: Arka plan tekstürü silinmişse (null ise) her şeyi baştan kur!
        if (windowStyle != null && windowStyle.normal.background != null && labelStyle != null) return;

        windowStyle = new GUIStyle(GUI.skin.window);
        // Arkaplan için düz renk (Texture2D oluşturarak)
        Texture2D bg = new Texture2D(1, 1);
        bg.SetPixel(0, 0, new Color(0.1f, 0.1f, 0.15f, 0.95f));
        bg.Apply();
        windowStyle.normal.background = bg;
        windowStyle.border = new RectOffset(5, 5, 5, 5);

        titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 32;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = new Color(0f, 0.86f, 1f); // Cyan

        labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 24;
        labelStyle.alignment = TextAnchor.MiddleCenter;
        labelStyle.normal.textColor = Color.white;
        labelStyle.richText = true;

        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 22;
        buttonStyle.fontStyle = FontStyle.Bold;
        buttonStyle.fixedHeight = 50;

        stylesInitialized = true;
    }

    private void DrawIhaleWindow(int windowID)
    {
        GUILayout.BeginArea(new Rect(20, 20, 560, 360));
        
        switch (currentScreen)
        {
            case IhaleScreen.QUESTION:
                GUILayout.Space(20);
                GUILayout.Label("YENİ PROJE İHALESİ!", titleStyle);
                GUILayout.Space(30);
                GUILayout.Label("Büyük bir lojistik projesi açıldı.\nKatılmak için ön teklif sunmak ister misiniz?", labelStyle);
                GUILayout.FlexibleSpace();
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("İHALEYE KATIL", buttonStyle)) IhaleyeKatil();
                if (GUILayout.Button("PAS GEÇ", buttonStyle)) PasGec();
                GUILayout.EndHorizontal();
                break;

            case IhaleScreen.BIDDING:
                GUILayout.Label("İHALE DEVAM EDİYOR", titleStyle);
                GUILayout.Space(20);
                GUILayout.Label($"<color=#FFDD00>Kalan Süre: {Mathf.CeilToInt(ihaleZamanlayici)}s</color>", labelStyle);
                GUILayout.Space(20);
                GUILayout.Label($"<b>Lider:</b> <color=#00DDFF>{suAnkiLiderRakip}</color> ({rakipEnDusukTeklif:F0}$)", labelStyle);
                GUILayout.Label($"<b>Senin Teklifin:</b> <color=#00FF99>{oyuncuTeklifi:F0}$</color>", labelStyle);
                
                GUILayout.FlexibleSpace();
                GUILayout.Label("TEKLİFİ DÜŞÜR (Daha cazip ol):", labelStyle);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("-10$", buttonStyle)) OyuncuTeklifDusur(10);
                if (GUILayout.Button("-50$", buttonStyle)) OyuncuTeklifDusur(50);
                if (GUILayout.Button("-150$", buttonStyle)) OyuncuTeklifDusur(150);
                GUILayout.EndHorizontal();
                break;

            case IhaleScreen.RESULT:
                GUILayout.Space(20);
                GUILayout.Label("İHALE SONUCU", titleStyle);
                GUILayout.Space(40);
                GUILayout.Label(sonucMesaji, labelStyle);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("TAMAM", buttonStyle)) { currentScreen = IhaleScreen.NONE; ihaleAktifMi = false; }
                break;
        }

        GUILayout.EndArea();
    }

    public void IhaleyeKatil()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(AudioManager.Instance.ihaleBaslangicSesi);
        currentScreen = IhaleScreen.BIDDING;
        ihaleZamanlayici = kararSuresi;
        oyuncuTeklifi = 600f;
        rakipEnDusukTeklif = 550f;
        suAnkiLiderRakip = rakipler[Random.Range(0, rakipler.Length)];

        StopAllCoroutines();
        StartCoroutine(IhaleDongusu());
    }

    public void PasGec()
    {
        currentScreen = IhaleScreen.NONE;
        ihaleAktifMi = false;
        Debug.Log("<color=gray>[İHALE]:</color> Oyuncu katılmayı reddetti.");
    }

    IEnumerator IhaleDongusu()
    {
        float nextTickTime = ihaleZamanlayici - 1f;
        while (ihaleZamanlayici > 0)
        {
            ihaleZamanlayici -= Time.deltaTime;

            if (ihaleZamanlayici <= nextTickTime)
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(AudioManager.Instance.ihaleSureSesi);
                nextTickTime -= 1f;
            }

            if (Random.value < 0.12f && rakipEnDusukTeklif > 150f)
            {
                rakipEnDusukTeklif -= Random.Range(5, 15);
                string hamleYapan = rakipler[Random.Range(0, rakipler.Length)];
                if (rakipEnDusukTeklif < oyuncuTeklifi) suAnkiLiderRakip = hamleYapan;
            }
            yield return null;
        }
        IhaleyiBitir();
    }

    public void OyuncuTeklifDusur(float miktar)
    {
        // Teklifin 200'ün altına düşmesini engelleyelim (Bedavaya ihale olmaz ustam!)
        if (oyuncuTeklifi - miktar >= 200)
        {
            oyuncuTeklifi -= miktar;
            if (oyuncuTeklifi < rakipEnDusukTeklif) suAnkiLiderRakip = "OYUNCU";
        }
        else
        {
            // Eğer 200'ün altına düşecekse, tam 200'e sabitle (Eğer mevcut teklif 200'den büyükse)
            if (oyuncuTeklifi > 200)
            {
                oyuncuTeklifi = 200;
                if (oyuncuTeklifi < rakipEnDusukTeklif) suAnkiLiderRakip = "OYUNCU";
            }
        }
    }

    void IhaleyiBitir()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(AudioManager.Instance.ihaleBitisSesi);
        currentScreen = IhaleScreen.RESULT;

        if (suAnkiLiderRakip == "OYUNCU")
        {
            int odulPara = Random.Range(1500, 3000);
            float odulItibar = 5.0f;
            if (UIManager.Instance != null) UIManager.Instance.oyuncuParasi += odulPara;
            if (MarketManager.Instance != null) MarketManager.Instance.musteriligisi += odulItibar;
            sonucMesaji = $"<color=#00FF99>TEBRİKLER!</color>\nİhaleyi kazandın.\nKazanç: {odulPara}$ | İtibar: +{odulItibar}%";
        }
        else
        {
            sonucMesaji = $"<color=#FF4444>MÜÜSEKSEKE!</color>\nİhaleyi {suAnkiLiderRakip} kazandı.\nDaha düşük bir fiyat vermen gerekiyordu.";
        }
    }
}