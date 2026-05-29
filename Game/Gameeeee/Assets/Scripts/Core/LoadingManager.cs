using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LoadingManager : MonoBehaviour
{
    public static LoadingManager Instance;

    [Header("UI Referansları")]
    [Tooltip("Tüm yükleme ekranını kaplayan arkaplan veya panel.")]
    public GameObject loadingScreen; 
    
    [Tooltip("Yükleme durumunu gösteren kaydırma çubuğu.")]
    public Slider progressBar; 
    
    [Tooltip("Yükleme yüzdesini (%45 vb.) gösterecek TextMeshPro metni.")]
    public TextMeshProUGUI progressText; 

    private void Awake()
    {
        // Singleton Deseni - Sahneler arası objenin silinmemesi için
        if (Instance == null)
        {
            Instance = this;
            // Sahneler arası geçerken UI dahil bu objenin kalmasını sağlar
            transform.SetParent(null); 
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Oyun başladığında yükleme ekranı yanlışlıkla açıksa gizli kalsın
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(false);
        }
    }

    /// <summary>
    /// Herhangi bir sahneyi arka planda donmadan yüklemeyi başlatır.
    /// Kullanımı: LoadingManager.Instance.LoadScene("SahneAdi");
    /// </summary>
    /// <param name="sceneName">Gidilecek sahnenin tam adı e.g. "GameScene"</param>
    public void LoadScene(string sceneName)
    {
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(true);
        }

        if (progressBar != null)
        {
            progressBar.value = 0f;
        }

        StartCoroutine(LoadSceneAsynchronously(sceneName));
    }

    private IEnumerator LoadSceneAsynchronously(string sceneName)
    {
        // Asenkron olarak yükleme işlemini Command olarak başlat
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);

        if (operation == null)
        {
            Debug.LogError("Sahne bulunamadı! Build Settings'e eklediğinizden emin olun: " + sceneName);
            yield break;
        }

        // Yükleme sırasında Unity arkada frame atlar
        while (!operation.isDone)
        {
            // Unity'de operation.progress 0 ile 0.9 arasında dolar. Son aşama 1'dir.
            float progress = Mathf.Clamp01(operation.progress / 0.9f);

            // Slider'ı güncelle (Değer 0 ile 1 aralığında)
            if (progressBar != null)
            {
                progressBar.value = progress;
            }

            // Yüzde Metnini güncelle
            if (progressText != null)
            {
                progressText.text = "%" + (progress * 100f).ToString("F0");
            }

            yield return null; // Update the loop every frame
        }

        // Yükleme tamamen bitince ekranı kapat, normal sahneyi göster
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(false);
        }
    }
}
