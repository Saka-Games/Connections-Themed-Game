using UnityEngine;
using System.Collections.Generic;

public class CityBuilderInfoPanel : MonoBehaviour
{
    public static CityBuilderInfoPanel Instance;

    [Header("Font Ayarı (Ham .ttf dosyası)")]
    public Font panelFontu; // Inspector'dan ham font dosyasını buraya sürükle

    private Tile selectedTile;
    private ProductionBuilding selectedBuilding;
    private bool showPanel = false;
    private Vector2 scrollPosition = Vector2.zero;
    
    // Pencere boyutu ve konumu (Sağ Alt Köşe)
    private Rect windowRect;
    private GUIStyle windowStyle;
    private GUIStyle topBarStyle;
    private GUIStyle labelStyle;
    private GUIStyle topBarLabelStyle;
    private GUIStyle titleStyle;
    
    // --- CACHING FOR PERFORMANCE ---
    private List<string> cachedConnections = new List<string>();
    private float nextRefreshTime = 0f;
    
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Yüksekliği kısalttık ve konumu ekranın altına/orta-sağına daha yakın yaptık (üstten aşağı 'çektik')
        windowRect = new Rect(Screen.width - 340, Screen.height - 420, 320, 400);
    }

    public bool IsPointerOverPanel()
    {
        float scale = Screen.height / 1080f;
        if (scale < 0.1f) scale = 1f;
        
        // 1. Üst Bar Kontrolü (Sanal 1080p üzerinden 40 piksel)
        float flippedY = (Screen.height - Input.mousePosition.y) / scale;
        if (flippedY >= 0 && flippedY <= 45) return true;

        if (!showPanel || selectedTile == null) return false;

        // Flipped Mouse Pos (Sanal koordinata çevir)
        Vector2 flippedMousePos = new Vector2(Input.mousePosition.x / scale, flippedY);
        
        // windowRect artık sanal 1080p koordinatlarında
        return windowRect.Contains(flippedMousePos);
    }

    private void BuildStyles()
    {
        // --- SAHNE GEÇİŞİ FIX: Sadece stillerin varlığı yetmez; içindeki tekstür silinmiş mi bak! ---
        if (topBarLabelStyle != null && windowStyle != null && topBarStyle != null && windowStyle.normal.background != null && titleStyle != null) return;
         // Arka plan dokusu (Eğer style gelmezse diye garanti)
        Texture2D darkTex = new Texture2D(1, 1);
        darkTex.SetPixel(0, 0, new Color(0.1f, 0.1f, 0.12f, 0.95f));
        darkTex.Apply();

        // Transport Fever / Cities Skylines stili Yarı saydam koyu renkli pencere
        windowStyle = new GUIStyle();
        windowStyle.normal.background = darkTex;
        if (panelFontu != null) windowStyle.font = panelFontu;
        windowStyle.fontSize = 18;
        windowStyle.normal.textColor = Color.white;
        windowStyle.padding = new RectOffset(20, 20, 40, 20); // Pencere için geniş padding

        // Üst Bar için özel (Padding'siz) versiyon
        topBarStyle = new GUIStyle(windowStyle);
        topBarStyle.padding = new RectOffset(10, 10, 0, 0); // Bar içeriği sıkışmasın

        labelStyle = new GUIStyle();
        if (panelFontu != null) labelStyle.font = panelFontu;
        labelStyle.fontSize = 18;
        labelStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
        labelStyle.richText = true;

        topBarLabelStyle = new GUIStyle(labelStyle);
        topBarLabelStyle.fontSize = 16;
        topBarLabelStyle.alignment = TextAnchor.MiddleLeft;

        titleStyle = new GUIStyle(labelStyle);
        titleStyle.fontSize = 24;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.normal.textColor = new Color(0.0f, 0.8f, 1f);
        titleStyle.alignment = TextAnchor.MiddleCenter;
    }

    public void ShowPanel(Tile tile, ProductionBuilding building)
    {
        selectedTile = tile;
        selectedBuilding = building;
        showPanel = true;

        // Panel açıldığında bağlantı durumunu kontrol et
        // AMA: Eğer SelectionManager zaten true yapmışsa, ezme!
        if (tile != null && !tile.isConnected && NetworkManager.Instance != null)
            tile.isConnected = NetworkManager.Instance.IsTileConnected(tile);

        Debug.Log($"<color=green>[PANEL]:</color> {tile.name} (ID:{tile.tileID}) için panel açıldı. isConnected={tile.isConnected}");
    }

    public void HidePanel()
    {
        showPanel = false;
        selectedTile = null;
        selectedBuilding = null;
    }

    private void OnGUI()
    {
        GUI.depth = 0;
        BuildStyles();

        // --- RESPONSIVE SCALING (1080p Base) ---
        float scale = Screen.height / 1080f;
        if (scale < 0.1f) scale = 1f;
        
        Matrix4x4 oldMat = GUI.matrix;
        // TRX: Screen.width / scale diyerek X ekseninde sanal genişliği buluyoruz
        float virtualWidth = Screen.width / scale;
        float virtualHeight = 1080f;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1));

        // 1. EKONOMİ BANDI (Sanal genişlik kullanıyoruz)
        DrawTopBar(virtualWidth);

        if (showPanel && selectedTile != null)
        {
            // Bina Kartı artık daha büyük ve ferah (450x650)
            float targetWidth = 450;
            float targetHeight = 650;
            windowRect.width = targetWidth;
            windowRect.height = targetHeight;
            
            // Sağ alt köşeye hizala (Sanal koordinatlarla)
            windowRect.x = Mathf.Clamp(virtualWidth - targetWidth - 50, 0, virtualWidth - targetWidth);
            windowRect.y = Mathf.Clamp(virtualHeight - targetHeight - 80, 0, virtualHeight - targetHeight);

            windowRect = GUI.Window(0, windowRect, DrawPanelContent, "", windowStyle);
        }

        GUI.matrix = oldMat; // Matrisi geri yükle
    }

    private void DrawTopBar(float virtualWidth)
    {
        // En üste karanlık şerit (TopBarStyle kullanarak padding hatasını önle)
        GUI.Box(new Rect(0, 0, virtualWidth, 45), "", topBarStyle);
        GUILayout.BeginArea(new Rect(20, 5, virtualWidth - 40, 45));
        GUILayout.BeginHorizontal();

        // UIManager ve MarketManager'dan verileri çek
        int para = 0;
        if (UIManager.Instance != null) para = UIManager.Instance.oyuncuParasi;

        float pazarPayi = 0f;
        float itibar = 0f;
        if (MarketManager.Instance != null)
        {
            pazarPayi = MarketManager.Instance.oyuncuPayi;
            itibar = MarketManager.Instance.musteriligisi;
        }

        GUILayout.Label($"<b><color=#00FF99>💰 KASA:</color></b> {para:N0} $", topBarLabelStyle, GUILayout.Width(180));
        GUILayout.FlexibleSpace();
        GUILayout.Label($"<b><color=#FFDD00>📊 PAZAR:</color></b> %{pazarPayi:F1}", topBarLabelStyle, GUILayout.Width(160));
        GUILayout.FlexibleSpace();
        GUILayout.Label($"<b><color=#00DDFF>⭐ GÜVEN:</color></b> %{itibar:F1}", topBarLabelStyle, GUILayout.Width(160));
        GUILayout.FlexibleSpace();

        // ====== YOL YAPMA TOOLU (C:S Stili) ======
        if (BuildManager.Instance != null)
        {
            GUI.color = BuildManager.Instance.isRoadMode ? Color.yellow : Color.white;
            if (GUILayout.Button(BuildManager.Instance.isRoadMode ? "[YOL: AÇIK]" : "[YOL ARACI (100$)]", GUILayout.Width(140)))
            {
                BuildManager.Instance.ToggleRoadMode();
            }
            GUI.color = Color.white;
        }

        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    private void DrawPanelContent(int windowID)
    {
        GUILayout.Space(10);
        
        // Başlık (Bina Tipi veya Özel Adı)
        string title = (selectedBuilding != null && !string.IsNullOrEmpty(selectedBuilding.binaAdi)) ? selectedBuilding.binaAdi : selectedTile.type.ToString();
        GUILayout.Label(title.ToUpper(), titleStyle);
        
        // --- BAĞLANTI DURUMU (YENİ BİLGİLENDİRME) ---
        if (selectedTile.type != TileType.Road && selectedTile.type != TileType.Meadow)
        {
            string baglantiDurumu = selectedTile.isConnected ? "<color=#00FF99>✔ YOLA BAĞLI</color>" : "<color=#FF4444>✘ YOL BAĞLANTISI YOK</color>";
            GUILayout.Label($"<b>DRM:</b> {baglantiDurumu}", labelStyle);
            
            // --- BAĞLI BİNALAR LİSTESİ (CACHED) ---
            if (selectedTile.isConnected && NetworkManager.Instance != null)
            {
                if (Time.time > nextRefreshTime)
                {
                    var raw = NetworkManager.Instance.GetReachableBuildingNames(selectedTile);
                    // Duplikeleri temizle
                    HashSet<string> unique = new HashSet<string>(raw);
                    cachedConnections = new List<string>(unique);
                    nextRefreshTime = Time.time + 1.2f;
                }

                if (cachedConnections.Count > 0)
                {
                    GUILayout.Space(5);
                    int maxShow = 5;
                    GUILayout.Label($"<b>► GİDEN YOLLAR ({cachedConnections.Count}):</b>", labelStyle);
                    for (int i = 0; i < Mathf.Min(cachedConnections.Count, maxShow); i++)
                    {
                        GUILayout.Label($"   <color=#00DDFF>• {cachedConnections[i]}</color>", labelStyle);
                    }
                    if (cachedConnections.Count > maxShow)
                        GUILayout.Label($"   <color=#888888>... +{cachedConnections.Count - maxShow} daha</color>", labelStyle);
                }
            }
        }

        GUILayout.Space(10);
        
        // İhtiyaç yoksa Scroll gösterme, Yatay scroll'u ise asla gösterme (GUIStyle.none)
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, false, GUIStyle.none, GUI.skin.verticalScrollbar);

        // Şehir Planlamacısı Tarzı Bina Verileri
        if (selectedTile.type != TileType.City && selectedBuilding != null)
        {
            GUILayout.Label($"<b>Seviye:</b> {selectedBuilding.machineLevel}", labelStyle);
            GUILayout.Label($"<b>İşçi Kapasitesi:</b> {selectedBuilding.workerCount}", labelStyle);
            GUILayout.Space(25);
        }
        else if (selectedTile.type == TileType.City)
        {
            // Şehir ise işçi sayısı vs göstermeye gerek yok
            GUILayout.Space(5);
        }
        
        if (selectedTile.type == TileType.Farm && selectedBuilding != null)
        {
            GUILayout.Label($"<b>Üretim Hacmi (Toplam Stok):</b> {selectedBuilding.producedItems} / {selectedBuilding.maxStorageCapacity}", labelStyle);
            GUILayout.Label($"<b><color=#FFAA00>Fırına Ayrılan Buğday:</color></b> {selectedBuilding.bugdayFirinIcin}", labelStyle);
            GUILayout.Label($"<b><color=#00FF00>Ahıla Ayrılan Buğday:</color></b> {selectedBuilding.bugdayAhilIcin}", labelStyle);
        }
        else if (selectedTile.type == TileType.Factory && selectedBuilding != null)
        {
            string tur = selectedBuilding.koruyucuMaddeAktif ? "<color=red>Kimyasal</color>" : "<color=#00FF00>Doğal (Saf)</color>";
            GUILayout.Label($"<b>Dondurma Stok ({tur}):</b> {selectedBuilding.producedItems} / {selectedBuilding.maxStorageCapacity}", labelStyle);
            GUILayout.Space(15);
            GUILayout.Label("<b>Hammadde Merkez Deposu:</b>", labelStyle);
            GUILayout.Label($"- Gelen Süt: {selectedBuilding.mevcutSut}", labelStyle);
            GUILayout.Label($"- Külah: {selectedBuilding.mevcutKulah}", labelStyle);
            GUILayout.Label($"- Çeşit Aroma: {selectedBuilding.mevcutAroma}", labelStyle);
        }
        else if (selectedTile.type == TileType.City && selectedBuilding != null)
        {
            // Şehir & Nüfus (Normal Şehir Tipi Özellikleri)
            GUILayout.Label($"<b><color=#00FFAA>Bölge Nüfusu:</color></b> {selectedBuilding.population} Kişi", labelStyle);
            GUILayout.Label($"<b><color=#FFFFAA>Yerel Marka Sadakati:</color></b> %{selectedBuilding.targetCustomerRate:F1}", labelStyle);
            GUILayout.Space(5);
            GUILayout.Label($"<b><color=#00BBFF>Pazar Payı Hacmi:</color></b>\n{selectedBuilding.potentialCustomers} Potansiyel Müşteri", titleStyle);

            // C:S 2 "Mixed Use" tarzı Görsel Ayrım Çizgisi (Separator)
            GUILayout.Space(15);
            Rect separatorRect = GUILayoutUtility.GetRect(10, 2, GUILayout.ExpandWidth(true));
            GUI.color = new Color(0.6f, 0.6f, 0.6f, 0.5f); // Saydam gri, şık bir ayraç
            GUI.DrawTexture(separatorRect, Texture2D.whiteTexture);
            GUI.color = Color.white; // Rengi normale döndür
            GUILayout.Space(10);

            // Şehir Fırınları (Bakery Asset Özellikleri)
            GUILayout.Label($"<b><color=#EEAA33>► YEREL FIRIN (BAKERY) BİLGİLERİ</color></b>", labelStyle);
            GUILayout.Label($"<b>Üretilen Külah Stoku:</b> {selectedBuilding.producedItems} / {selectedBuilding.maxStorageCapacity}", labelStyle);
            GUILayout.Label($"<b>Fırın Buğday/Un Deposu:</b> {selectedBuilding.currentRawMaterial}", labelStyle);
        }
        else if (selectedTile.type == TileType.Road)
        {
            GUILayout.Label("<b>Tip:</b> Asfalt Yol", labelStyle);
            GUILayout.Label("<color=#AAAAAA>Bu yol üretim tesislerini birbirine bağlar.</color>", labelStyle);
            GUILayout.Space(10);
            GUILayout.Label("<i>Yol üzerinde bina kurulamaz.</i>", labelStyle);
        }
        else if (selectedBuilding != null)
        {
            GUILayout.Label($"<b>Üretim Hacmi:</b> {selectedBuilding.producedItems} / {selectedBuilding.maxStorageCapacity}", labelStyle);
            
            // Eğer hammaddesi varsa listeliyoruz
            if (selectedTile.type != TileType.Orchard && selectedTile.type != TileType.Meadow)
            {
                GUILayout.Label($"<b>Hammadde Stoğu:</b> {selectedBuilding.currentRawMaterial}", labelStyle);
            }
        }

        // ====== EKONOMİ AKŞİYON BUTONLARI ======
        if (EconomyManager.Instance != null && selectedBuilding != null)
        {
            GUILayout.Space(15);

            // Buton stili
            GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
            if (panelFontu != null) btnStyle.font = panelFontu; // FONT ATAMASI
            btnStyle.fontSize = 15;
            btnStyle.fontStyle = FontStyle.Bold;
            btnStyle.normal.textColor = Color.white;
            btnStyle.fixedHeight = 32f; // Butonların boyunu biraz kıstık sığsınlar diye.

            if (selectedBuilding != null && GUILayout.Button($"İşçi İşe Al (-{EconomyManager.Instance.isciMaliyeti}$)", btnStyle))
            {
                EconomyManager.Instance.IsciAl(selectedBuilding);
            }

            // EKİPMANI MODERNİZE ET (SEVİYE ATLATMA)
            if (selectedBuilding.upgradeLevel >= selectedBuilding.maxUpgradeLevel)
            {
                GUI.enabled = false;
                GUILayout.Button("EKİPMAN: MAKSİMUM SEVİYE (10)", btnStyle);
                GUI.enabled = true;
            }
            else
            {
                string upgradeText = $"Modernize Et: Sev. {selectedBuilding.upgradeLevel} -> {selectedBuilding.upgradeLevel + 1} (-{selectedBuilding.nextUpgradeCost}$)";
                if (GUILayout.Button(upgradeText, btnStyle))
                {
                    EconomyManager.Instance.MakineGelistir(selectedBuilding);
                }
            }

            // KAPASİTE ARTIRMA (DEPO VE ÇIKTI): Tüm üretim binaları için geçerli olmalı
            bool isProductionBuilding = selectedTile.type == TileType.Farm || 
                                       selectedTile.type == TileType.Pasture ||
                                       selectedTile.type == TileType.Orchard ||
                                       selectedTile.type == TileType.Factory ||
                                       (selectedBuilding != null && selectedBuilding.binaTipi == ProductionBuilding.BuildingType.Firin) ||
                                       (selectedBuilding != null && selectedBuilding.binaTipi == ProductionBuilding.BuildingType.KimyaMerkezi);

            if (selectedBuilding != null && isProductionBuilding)
            {
                if (GUILayout.Button($"Kapasite Birimi Ekle (-{EconomyManager.Instance.makineSatinAlmaMaliyeti}$)", btnStyle))
                {
                    EconomyManager.Instance.MakineSatinAl(selectedBuilding);
                }
            }

            // Hızlı Nakit Çevirme: Süt / Külah / Meyve / Buğday (Sell All Mechanices)
            if (selectedTile.type == TileType.Farm)
            {
                int tahminiKazanc = selectedBuilding.producedItems * EconomyManager.Instance.bugdayBirimFiyat;
                GUI.enabled = selectedBuilding.producedItems > 0;
                if (GUILayout.Button($"Tüm Buğdayı Sat (+{tahminiKazanc}$)", btnStyle))
                    EconomyManager.Instance.BugdaySat(selectedBuilding);
                GUI.enabled = true;
            }
            else if (selectedTile.type == TileType.Pasture)
            {
                int tahminiKazanc = selectedBuilding.producedItems * EconomyManager.Instance.sutBirimFiyat;
                GUI.enabled = selectedBuilding.producedItems > 0;
                if (GUILayout.Button($"Tüm Sütleri Sat (+{tahminiKazanc}$)", btnStyle))
                    EconomyManager.Instance.SutSat(selectedBuilding);
                GUI.enabled = true;
            }
            else if (selectedBuilding != null && (selectedBuilding.binaTipi == ProductionBuilding.BuildingType.Firin || selectedBuilding.binaTipi == ProductionBuilding.BuildingType.Sehir))
            {
                int tahminiKazanc = selectedBuilding.producedItems * EconomyManager.Instance.kulahBirimFiyat;
                GUI.enabled = selectedBuilding.producedItems > 0;
                if (GUILayout.Button($"Tüm Külahları Sat (+{tahminiKazanc}$)", btnStyle))
                    EconomyManager.Instance.KulahSat(selectedBuilding);
                GUI.enabled = true;
            }
            else if (selectedTile.type == TileType.Orchard)
            {
                int tahminiKazanc = selectedBuilding.producedItems * EconomyManager.Instance.meyveBirimFiyat;
                GUI.enabled = selectedBuilding.producedItems > 0;
                if (GUILayout.Button($"Tüm Meyve Özlerini Sat (+{tahminiKazanc}$)", btnStyle))
                    EconomyManager.Instance.MeyveSat(selectedBuilding);
                GUI.enabled = true;
            }
        }

        GUILayout.EndScrollView();

        // X (Kapat) Butonu - En Sağ Üst Köşede minik ve şık kırmızı çarpı
        GUIStyle closeButtonStyle = new GUIStyle(GUI.skin.button);
        if (panelFontu != null) closeButtonStyle.font = panelFontu; // FONT ATAMASI
        closeButtonStyle.normal.textColor = Color.red;
        closeButtonStyle.fontStyle = FontStyle.Bold;
        if (GUI.Button(new Rect(windowRect.width - 35, 10, 25, 25), "X", closeButtonStyle))
        {
            if (SelectionManager.Instance != null) SelectionManager.Instance.ClearSelection();
        }
    }
}