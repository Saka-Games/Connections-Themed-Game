using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Global Font Ayarı")]
    public TMP_FontAsset yeniFont; // Inspector'dan mavi ikonlu font asset'i buraya sürükle

    [Header("Para ve Genel Ekonomi")]
    public int oyuncuParasi = 1000;
    public TextMeshProUGUI paraMetni;
    public TextMeshProUGUI pazarPayiMetni;
    public TextMeshProUGUI ilgiMetni; // Müşteri Güveni
    public Slider pazarPayiSlider;

    [Header("Dinamik Seçili Bina")]
    public ProductionBuilding activeBuilding;

    [Header("Bina UI Panelleri")]
    public GameObject tarlaPanel;
    public GameObject ahilPanel;
    public GameObject firinPanel;
    public GameObject meyvePanel;
    public GameObject fabrikaPanel;
    
    [Header("Uyarı Pop-up Sistemi")]
    public GameObject warningPanel; 
    public TextMeshProUGUI warningText;
    private Coroutine warningCoroutine; 
    private string guiWarningMessage = ""; 
    private float guiWarningTimer = 0f;
    

    [Header("Panel Metin Referansları")]
    public TextMeshProUGUI firinSeviyeText;
    public TextMeshProUGUI firinIsciText;
    public TextMeshProUGUI firinMakineText;
    public TextMeshProUGUI firinCiktiText;
    public TextMeshProUGUI ahilIsciText;
    public TextMeshProUGUI ahilMakineText;
    public TextMeshProUGUI ahilCiktiText;
    public TextMeshProUGUI tarlaIsciText;
    public TextMeshProUGUI tarlaCiktiText;
    public TextMeshProUGUI meyveIsciText;
    public TextMeshProUGUI meyveCiktiText;
    public TextMeshProUGUI fabrikaIsciText;
    public TextMeshProUGUI fabrikaMakineText;
    public TextMeshProUGUI fabrikaCiktiText;
    public TextMeshProUGUI fabrikaModText;
    public TextMeshProUGUI fabrikaHammaddeText;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Sahnedeki tüm TMP yazılarını seçilen fonta çevir
        FontuTumMetinlereUygula();
    }

    private void FontuTumMetinlereUygula()
    {
        if (yeniFont == null) return;
        
        // 1. UI Metinleri (2D)
        TextMeshProUGUI[] tumUI = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
        foreach (var metin in tumUI) metin.font = yeniFont;

        // 2. Dünya Metinleri (3D - Örn: Yüzen Kartlar)
        TextMeshPro[] tumDunya = Resources.FindObjectsOfTypeAll<TextMeshPro>();
        foreach (var metin in tumDunya) metin.font = yeniFont;

        Debug.Log($"<color=cyan>[UI]:</color> {tumUI.Length} UI ve {tumDunya.Length} Dünya metnine font uygulandı.");
    }

    void Update()
    {
        EkonomiyiGuncelle();
        BinalariGuncelle();
    }

    void EkonomiyiGuncelle()
    {
        if (paraMetni != null)
            paraMetni.text = oyuncuParasi.ToString("N0") + " $";

        if (MarketManager.Instance != null)
        {
            if (pazarPayiMetni != null) pazarPayiMetni.text = $"Pazar Payı: %{MarketManager.Instance.oyuncuPayi:F1}";
            if (pazarPayiSlider != null) pazarPayiSlider.value = MarketManager.Instance.oyuncuPayi / 100f;
            if (ilgiMetni != null) ilgiMetni.text = $"Halkın Güveni: %{MarketManager.Instance.musteriligisi:F0}";
        }
    }

    void BinalariGuncelle()
    {
        if (activeBuilding == null) return;

        if (activeBuilding.binaTipi == ProductionBuilding.BuildingType.Firin)
        {
            if (firinSeviyeText != null) firinSeviyeText.text = "Seviye: " + activeBuilding.machineLevel;
            if (firinIsciText != null) firinIsciText.text = "İşçi Sayısı: " + activeBuilding.workerCount;
            if (firinMakineText != null) firinMakineText.text = "Fırın Sayısı: " + activeBuilding.machineLevel;
            if (firinCiktiText != null) firinCiktiText.text = "Külah: " + activeBuilding.producedItems;
        }
        else if (activeBuilding.binaTipi == ProductionBuilding.BuildingType.Ahil)
        {
            if (ahilIsciText != null) ahilIsciText.text = "İşçi Sayısı: " + activeBuilding.workerCount;
            if (ahilMakineText != null) ahilMakineText.text = "İnek Sayısı: " + activeBuilding.machineLevel;
            if (ahilCiktiText != null) ahilCiktiText.text = "Süt: " + activeBuilding.producedItems;
        }
        else if (activeBuilding.binaTipi == ProductionBuilding.BuildingType.Tarla)
        {
            if (tarlaIsciText != null) tarlaIsciText.text = "İşçi Sayısı: " + activeBuilding.workerCount;
            if (tarlaCiktiText != null) tarlaCiktiText.text = $"Fırın: {activeBuilding.bugdayFirinIcin} | Ahıl: {activeBuilding.bugdayAhilIcin}";
        }
        else if (activeBuilding.binaTipi == ProductionBuilding.BuildingType.Fabrika)
        {
            if (fabrikaIsciText != null) fabrikaIsciText.text = "İşçi Sayısı: " + activeBuilding.workerCount;
            if (fabrikaMakineText != null) fabrikaMakineText.text = "Makine: " + activeBuilding.machineLevel;
            if (fabrikaCiktiText != null) fabrikaCiktiText.text = $"Stok: {activeBuilding.producedItems}/{activeBuilding.maxStorageCapacity}";
            if (fabrikaModText != null) fabrikaModText.text = "Mod: " + (activeBuilding.koruyucuMaddeAktif ? "Kimyasal" : "Doğal");
            if (fabrikaHammaddeText != null) fabrikaHammaddeText.text = $"S:{activeBuilding.mevcutSut} K:{activeBuilding.mevcutKulah} A:{activeBuilding.mevcutAroma}";
        }
        else if (activeBuilding.binaTipi == ProductionBuilding.BuildingType.MeyveBahcesi)
        {
            if (meyveIsciText != null) meyveIsciText.text = "İşçi Sayısı: " + activeBuilding.workerCount;
            if (meyveCiktiText != null) meyveCiktiText.text = "Aroma: " + activeBuilding.producedItems;
        }
    }

    public void PanelToggle(GameObject hedefPanel)
    {
        if (hedefPanel == null) return;
        if (hedefPanel.activeSelf) hedefPanel.SetActive(false);
        else
        {
            TumPanelleriKapat();
            hedefPanel.SetActive(true);
        }
    }

    public void TumPanelleriKapat()
    {
        if(tarlaPanel) tarlaPanel.SetActive(false);
        if(ahilPanel) ahilPanel.SetActive(false);
        if(firinPanel) firinPanel.SetActive(false);
        if(meyvePanel) meyvePanel.SetActive(false);
        if(fabrikaPanel) fabrikaPanel.SetActive(false);
        activeBuilding = null;
    }

    public void OpenBuildingPanel(ProductionBuilding building)
    {
        if (building == null) return;
        if (activeBuilding == building) { TumPanelleriKapat(); return; }
        
        TumPanelleriKapat();
        activeBuilding = building;

        switch (building.binaTipi)
        {
            case ProductionBuilding.BuildingType.Tarla: if (tarlaPanel) tarlaPanel.SetActive(true); break;
            case ProductionBuilding.BuildingType.Ahil: if (ahilPanel) ahilPanel.SetActive(true); break;
            case ProductionBuilding.BuildingType.Firin: if (firinPanel) firinPanel.SetActive(true); break;
            case ProductionBuilding.BuildingType.MeyveBahcesi: if (meyvePanel) meyvePanel.SetActive(true); break;
            case ProductionBuilding.BuildingType.Fabrika: if (fabrikaPanel) fabrikaPanel.SetActive(true); break;
        }
        BinalariGuncelle();
    }

    public void ShowWarning(string message)
    {
        // OTOMATİK BULMA: Eğer atanmamışsa isimden bulmaya çalış
        if (warningPanel == null) {
            Transform t = transform.Find("WarningPanel");
            if (t != null) warningPanel = t.gameObject;
        }
        if (warningText == null && warningPanel != null) {
            warningText = warningPanel.GetComponentInChildren<TextMeshProUGUI>();
        }

        if (warningPanel == null || warningText == null)
        {
            Debug.LogWarning($"<color=orange>[UI YARDIMI]:</color> '{message}' için panel bulunamadı. GUI Fallback kullanılıyor.");
            guiWarningMessage = message;
            guiWarningTimer = 2.5f;
            return;
        }

        if (warningCoroutine != null) StopCoroutine(warningCoroutine);
        warningCoroutine = StartCoroutine(WarningShowRoutine(message));
    }

    private void OnGUI()
    {
        if (guiWarningTimer > 0)
        {
            guiWarningTimer -= Time.deltaTime;
            GUIStyle style = new GUIStyle();
            style.fontSize = 30;
            style.normal.textColor = Color.red;
            style.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(0, Screen.height * 0.2f, Screen.width, 50), guiWarningMessage, style);
        }
    }

    private System.Collections.IEnumerator WarningShowRoutine(string msg)
    {
        warningText.text = msg;
        warningPanel.SetActive(true);
        yield return new WaitForSeconds(2.5f);
        warningPanel.SetActive(false);
    }
}