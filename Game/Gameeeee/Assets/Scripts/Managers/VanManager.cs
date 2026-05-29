using UnityEngine;
using System.Collections.Generic;

public class VanManager : MonoBehaviour
{
    [Header("Araç Ayarları")]
    public List<GameObject> dondurmaAraclari; // 5 adet aracını buraya sürükle
    public float hareketHizi = 2f;
    public int satisMiktari = 100; // Her duruşta kazanılacak para

    [Header("Zaman Ayarları")]
    public float minDurmaSuresi = 5f;
    public float maxDurmaSuresi = 15f;

    private void Start()
    {
        foreach (GameObject arac in dondurmaAraclari)
        {
            StartCoroutine(AracDongusu(arac));
        }
    }

    System.Collections.IEnumerator AracDongusu(GameObject arac)
    {
        while (true)
        {
            // 1. HAREKET ET (Yanılsama: Şehri geziyor)
            float geziSuresi = Random.Range(10f, 20f);
            float timer = 0;
            Vector3 rastgeleYon = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;

            while (timer < geziSuresi)
            {
                arac.transform.Translate(rastgeleYon * hareketHizi * Time.deltaTime);
                timer += Time.deltaTime;
                yield return null;
            }

            // 2. DUR VE SATIŞ YAP (Yanılsama: Müşterilere dondurma veriyor)
            Debug.Log(arac.name + " bir mahallede durdu, satış başlıyor...");
            
            // Para kazanma efekti
            if (UIManager.Instance != null)
            {
                UIManager.Instance.oyuncuParasi += satisMiktari;
            }

            yield return new WaitForSeconds(Random.Range(minDurmaSuresi, maxDurmaSuresi));
        }
    }
}