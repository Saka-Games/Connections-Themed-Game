using UnityEngine;

public class SatisDuragi : MonoBehaviour
{
    public string durakAdi;
    public int mevcutDondurma = 0;
    public int maxKapasite = 50;
    public int satisFiyati = 50;

    public void SatisYap()
    {
        if (mevcutDondurma > 0)
        {
            mevcutDondurma--;
            UIManager.Instance.oyuncuParasi += satisFiyati;
            // Opsiyonel: MarketManager.Instance.oyuncuPayi += 0.01f;
        }
    }
}