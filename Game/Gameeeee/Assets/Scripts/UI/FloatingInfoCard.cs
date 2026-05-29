using UnityEngine;
using TMPro; // TextMeshPro kütüphanesi 3D yazılar için

public class FloatingInfoCard : MonoBehaviour
{
    private TextMeshPro textMesh;
    private ProductionBuilding building;

    public void Setup(ProductionBuilding b)
    {
        building = b;

        // Obje oluştuğu an üzerine bir 3D TextMeshPro ekleyelim
        textMesh = gameObject.AddComponent<TextMeshPro>();

        // FONT GÜNCELLEMESİ: UIManager'daki fontu uygula
        if (UIManager.Instance != null && UIManager.Instance.yeniFont != null)
        {
            textMesh.font = UIManager.Instance.yeniFont;
        }

        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.fontSize = 200; // Harita çok büyük (500 birim) olduğu için yazı uzaktan okunabilsin
        
        // Opsiyonel Görsellik: Yazının daha güzel gözükmesi için renk/seçenek eklenebilir
        textMesh.color = Color.white;
        textMesh.fontStyle = FontStyles.Bold; // Kalın punto

        // Yazının objenin ne kadar üstünde duracağını hizalayalım
        // Binanın konumundan "y" ekseninde biraz tepeye kaydırıyoruz
        transform.position = b.transform.position + Vector3.up * 180f;
    }

    void Update()
    {
        if (building == null) return;

        // HER SANİYE BİLGİYİ GÜNCELLEYELİM
        string info = $"<color=yellow><size=130%>{building.binaTipi}</size></color>\n";
        info += $"Seviye: {building.machineLevel}  |  İşçi: {building.workerCount}\n";
        
        // Üretim verisi her bina tipine göre ayrışır:
        if (building.binaTipi == ProductionBuilding.BuildingType.Tarla)
        {
            info += $"<color=orange>Fırına: {building.bugdayFirinIcin}</color> | <color=green>Ahıla: {building.bugdayAhilIcin}</color>";
        }
        else if (building.binaTipi == ProductionBuilding.BuildingType.Fabrika)
        {
            string tur = building.koruyucuMaddeAktif ? "<color=red>Kimyasal</color>" : "<color=#00FF00>Doğal</color>";
            info += $"Dondurma ({tur}): {building.producedItems}/{building.maxStorageCapacity}\n";
            info += $"<size=80%>Süt:{building.mevcutSut} Külah:{building.mevcutKulah} Aroma:{building.mevcutAroma}</size>";
        }
        else
        {
            info += $"Üretim: <color=cyan>{building.producedItems}/{building.maxStorageCapacity}</color>";
        }

        textMesh.text = info;

        // BILLBOARD ETKİSİ: Yazı her zaman oyuncunun kamerasına TERS dönmeli ki düz okunabilsin.
        if (Camera.main != null)
        {
            transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
        }
    }
}