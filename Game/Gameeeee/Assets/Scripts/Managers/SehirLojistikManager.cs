using UnityEngine;
using System.Collections.Generic;

public class SehirLojistikManager : MonoBehaviour
{
    public ProductionBuilding fabrika;
    public List<SatisDuragi> duraklar; // Şehirdeki 5 durağı buraya sürükle
    
    public int sevkiyatMiktari = 10;
    public float kontrolSuresi = 5f;
    private float timer = 0f;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= kontrolSuresi)
        {
            SehirSevkiyatKontrol();
            timer = 0f;
        }

        // Durakların satış yapmasını sağla
        foreach (var durak in duraklar) durak.SatisYap();
    }

    void SehirSevkiyatKontrol()
    {
        if (fabrika == null || fabrika.producedItems < sevkiyatMiktari) return;

        foreach (var durak in duraklar)
        {
            // Durakta yer varsa ve fabrikada mal varsa gönder
            if (durak.mevcutDondurma + sevkiyatMiktari <= durak.maxKapasite)
            {
                // TRANSFER MANTIĞI
                fabrika.producedItems -= sevkiyatMiktari;
                durak.mevcutDondurma += sevkiyatMiktari;
                
                Debug.Log($"Şehir Lojistiği: {durak.durakAdi} noktasına dondurma ikmal edildi.");
                break; // Her seferinde sadece bir durağa gitsin (Kamyon kısıtı gibi)
            }
        }
    }
}