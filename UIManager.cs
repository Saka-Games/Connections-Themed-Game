using UnityEngine;
using TMPro; // TextMeshPro kütüphanesini kullanacağımızı Unity'ye söylüyoruz!

public class UIManager : MonoBehaviour
{
    [Header("Verilerin Alınacağı Binalar")]
    // Sayıları okuyacağımız kaynaklar
    public ProductionBuilding ahil;
    public ProductionBuilding firin;
    public ProductionBuilding fabrika;

    [Header("Ekrana Yazdırılacak Metinler")]
    // Arayüzdeki yazı objeleri
    public TextMeshProUGUI ahilText;
    public TextMeshProUGUI firinText;
    public TextMeshProUGUI fabrikaText;

    [Header("Ekonomi")]
    public TextMeshProUGUI bakiyeText;
    public int oyuncuParasi = 1000; // Şimdilik 1000 dolarımız olsun

    // Update her saniye (her karede) çalışır ve yazıları güncel tutar
    void Update()
    {
        // Eğer Ahıl binası ve yazısı bağlandıysa, metni güncelle
        if (ahil != null && ahilText != null)
        {
            // \n kodu, yazıyı bir alt satıra geçirir (Enter tuşu gibi)
            ahilText.text = $"-- AHIL --\nÜretilen Süt: {ahil.producedItems}\nİşçi: {ahil.workerCount}";
        }

        if (firin != null && firinText != null)
        {
            firinText.text = $"-- FIRIN --\nÜretilen Un: {firin.producedItems}\nİşçi: {firin.workerCount}";
        }

        if (fabrika != null && fabrikaText != null)
        {
            fabrikaText.text = $"-- FABRİKA --\nDepodaki Ham Madde: {fabrika.currentRawMaterial}\nÜRETİLEN DONDURMA: {fabrika.producedItems}";
        }

        if (bakiyeText != null)
        {
            bakiyeText.text = $"BÜTÇE: {oyuncuParasi} $";
        }
    }
}