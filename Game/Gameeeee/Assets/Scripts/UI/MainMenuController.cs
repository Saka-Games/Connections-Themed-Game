using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    [Header("Sahne Ayarları")]
    [Tooltip("Play (Oyna) tuşuna basıldığında açılacak asıl oyun sahnesinin adı.")]
    public string gameSceneName = "SampleScene";

    [Header("Arayüz Ayarları")]
    [Tooltip("Yükleme başladığında gizlenecek olan butonların bulunduğu Canvas veya Panel.")]
    public GameObject mainMenuUI;

    /// <summary>
    /// "Yeni Oyun" butonuna tıklandığında çalışır.
    /// Hafızadaki verileri sıfırlar veya GameManager'a yeni oyun başlattığımızı söyler.
    /// </summary>
    public void PlayNewGame()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayNewGameSound();
        // GameManager'a kayıtlı oyundan gelmediğimizi söylüyoruz
        GameManager.kayittanDevamEt = false;
        LoadGameScene();
    }

    /// <summary>
    /// "Devam Et" butonuna tıklandığında çalışır.
    /// GameManager'ın verileri yüklemesi gerektiğini söyler.
    /// </summary>
    public void ContinueGame()
    {
        // GameManager'a kayıtlı oyunu yüklemesini söylüyoruz
        GameManager.kayittanDevamEt = true;
        LoadGameScene();
    }

    private void LoadGameScene()
    {
        // Yükleme ekranı açıldığında menü butonlarını gizle
        if (mainMenuUI != null)
        {
            mainMenuUI.SetActive(false);
        }

        // Yükleme Ekranı (LoadingManager) kullanarak hedeflenen sahneyi açar.
        if (LoadingManager.Instance != null)
        {
            LoadingManager.Instance.LoadScene(gameSceneName);
        }
        else
        {
            // Eğer Yükleme ekranı objesini unutursan yedek olarak direkt yüklesin
            Debug.LogWarning("LoadingManager bulunamadı! Direkt geçiş yapılıyor...");
            UnityEngine.SceneManagement.SceneManager.LoadScene(gameSceneName);
        }
    }

    /// <summary>
    /// Oyundan çıkış butonuna tıklandığında çalışır.
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("Oyundan Çıkış İsteği Alındı (Bu sadece Build alındığında uygulamayı kapatır)");
        Application.Quit();
    }
}
