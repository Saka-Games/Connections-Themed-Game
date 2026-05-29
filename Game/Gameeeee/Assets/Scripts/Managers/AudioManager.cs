using UnityEngine;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Müzik (Arka Plan)")]
    public AudioClip anaTemaMuzigi;
    
    [Header("Genel Sesler")]
    public AudioClip etkilesimTusuSesi;
    public AudioClip yeniOyunTuSesi;
    public AudioClip paraSesiArtis;
    public AudioClip paraSesiAzalis;
    public AudioClip yolYapmaSesi;
    
    [Header("Bina Sesleri")]
    public AudioClip fabrikaSesi;
    public AudioClip firinSesi;
    public AudioClip sehirSesi;
    public AudioClip[] tarlaSesleri;
    public AudioClip[] meraSesleri;

    [Header("İhale Sesleri")]
    public AudioClip ihaleBaslangicSesi;
    public AudioClip ihaleSureSesi;
    public AudioClip ihaleBitisSesi;

    private AudioSource bgmSource;
    private AudioSource sfxSource;

    private int lastOyuncuParasi = 0;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            // Ana menüden oyun sahnesine geçerken müzik kesilmesin!
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Create Audio Sources
        bgmSource = gameObject.AddComponent<AudioSource>();
        sfxSource = gameObject.AddComponent<AudioSource>();

        bgmSource.loop = true;
        bgmSource.playOnAwake = false;
        bgmSource.volume = 0.5f; // BGM'i biraz kisalim

        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
    }

    private void Start()
    {
        // Arka plan muzigini baslat
        if (anaTemaMuzigi != null)
        {
            bgmSource.clip = anaTemaMuzigi;
            bgmSource.Play();
        }

        if (UIManager.Instance != null)
        {
            lastOyuncuParasi = UIManager.Instance.oyuncuParasi;
        }

        // Sahnedeki butonlara otomatik ses ata
        AttachInteractionSoundToButtons();
    }

    private void Update()
    {
        // Para kontrol sistemi: Her frame uzerinden parayi test et. Degisim varsa sesi cal.
        if (UIManager.Instance != null)
        {
            if (UIManager.Instance.oyuncuParasi > lastOyuncuParasi)
            {
                PlaySFX(paraSesiArtis);
            }
            else if (UIManager.Instance.oyuncuParasi < lastOyuncuParasi)
            {
                PlaySFX(paraSesiAzalis);
            }
            lastOyuncuParasi = UIManager.Instance.oyuncuParasi;
        }
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    public void PlayRandomSFX(AudioClip[] clips)
    {
        if (clips != null && clips.Length > 0)
        {
            int index = Random.Range(0, clips.Length);
            PlaySFX(clips[index]);
        }
    }

    public void PlayInteractionSound()
    {
        PlaySFX(etkilesimTusuSesi);
    }
    
    public void PlayNewGameSound()
    {
        PlaySFX(yeniOyunTuSesi);
    }

    // Sahnedeki tum butonlari bul ve etkilesim sesi ekle (UI yonetimi)
    private void AttachInteractionSoundToButtons()
    {
#pragma warning disable CS0618
        Button[] tumButonlar = FindObjectsOfType<Button>(true);
#pragma warning restore CS0618
        foreach (Button btn in tumButonlar)
        {
            btn.onClick.AddListener(PlayInteractionSound);
        }
    }
}
