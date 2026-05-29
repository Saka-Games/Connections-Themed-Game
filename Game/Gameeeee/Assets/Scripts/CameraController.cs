using UnityEngine;
using UnityEngine.InputSystem; // Yeni Input System şart!

public class CameraController : MonoBehaviour
{
    [Header("Hareket Ayarları")]
    public float moveSpeed = 2000f;
    public float rotationSpeed = 100f;
    public float zoomSpeed = 5000f;

    [Header("Zoom Sınırları")]
    public float minY = 200f;
    public float maxY = 3000f;

    [Header("Bölge Sınırları (Map Boundaries)")]
    public float minX = -2000f;
    public float maxX = 12000f;
    public float minZ = -2000f;
    public float maxZ = 12000f;

    [Header("Geçiş Ayarları")]
    public Vector3 menuPosition = new Vector3(392.5f, 175f, -30f);
    public Vector3 menuRotationEuler = new Vector3(0f, 0f, 0f); // İlk başta X:0 açısı istendi
    private Vector3 oyunBaslangicPos;
    private Quaternion oyunBaslangicRot;
    
    private bool isTransitioning = false;
    private float transitionT = 0f;
    public float transitionSpeed = 1.5f;

    void Start()
    {
        // Far Clip Plane özelliğini varsayılan olarak 5000 yap (Haritanın köşeleri silinmesin)
        Camera cam = GetComponent<Camera>();
        if (cam != null)
        {
            cam.farClipPlane = 5000f;
        }

        // Oyun başladığında gitmesi gereken sabit koordinat
        oyunBaslangicPos = new Vector3(4500f, 1000f, -400f);

        // Rotasyonun başı x=30 sabit olacak (Y ve Z açısını inspector'daki mevcuttan alır)
        oyunBaslangicRot = Quaternion.Euler(30f, transform.eulerAngles.y, transform.eulerAngles.z);

        // Hemen menü kamerası konumuna ışınla (X açısı = 0 olarak)
        transform.position = menuPosition;
        transform.rotation = Quaternion.Euler(menuRotationEuler);
    }

    void Update()
    {
        if (Keyboard.current == null || Mouse.current == null) return;
        
        // Geçiş (Kameranın menüden oyuna uçması) devam ediyorsa kontrolleri kilitle
        if (isTransitioning)
        {
            // timeScale 0 olsa bile geçişin çalışması için unscaledDeltaTime kullanılır
            transitionT += Time.unscaledDeltaTime * transitionSpeed;
            transform.position = Vector3.Lerp(menuPosition, oyunBaslangicPos, Mathf.SmoothStep(0, 1, transitionT));
            transform.rotation = Quaternion.Slerp(Quaternion.Euler(menuRotationEuler), oyunBaslangicRot, Mathf.SmoothStep(0, 1, transitionT));

            if (transitionT >= 1f)
            {
                isTransitioning = false;
            }
            return;
        }

        // Oyun durmuşsa kamerayı hareket ettirme
        if (Time.timeScale <= 0.01f) return;

        HandleMovement();
        HandleRotation();
        HandleZoom();
    }

    public void OyunaGecisYap()
    {
        isTransitioning = true;
        transitionT = 0f;
    }

    void HandleMovement()
    {
        Vector3 direction = Vector3.zero;

        // İleri-Geri ve Sağ-Sol (Sadece X-Z düzleminde)
        if (Keyboard.current.wKey.isPressed) direction += Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        if (Keyboard.current.sKey.isPressed) direction -= Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        if (Keyboard.current.aKey.isPressed) direction -= Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
        if (Keyboard.current.dKey.isPressed) direction += Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;

        Vector3 newPos = transform.position + direction.normalized * moveSpeed * Time.unscaledDeltaTime;
        
        // NAN ve SINIR KORUMASI (NaN/Ind Hatasını Önler)
        if (!float.IsNaN(newPos.x) && !float.IsNaN(newPos.y) && !float.IsNaN(newPos.z))
        {
            newPos.x = Mathf.Clamp(newPos.x, minX, maxX);
            newPos.z = Mathf.Clamp(newPos.z, minZ, maxZ);
            transform.position = newPos;
        }
    }

    void HandleRotation()
    {
        float rotationDir = 0;
        float pitchDir = 0;

        // Q ve E ile sağ-sol dönüş (Yaw - Dünya ekseni)
        if (Keyboard.current.qKey.isPressed) rotationDir -= 1f;
        if (Keyboard.current.eKey.isPressed) rotationDir += 1f;

        // R ve F ile bakış açısı değiştirme (Pitch - Yerel eksen)
        if (Keyboard.current.rKey.isPressed) pitchDir -= 0.5f; // Yukarı bak
        if (Keyboard.current.fKey.isPressed) pitchDir += 0.5f; // Aşağı bak

        // Yatay Dönüş
        transform.Rotate(Vector3.up, rotationDir * rotationSpeed * Time.unscaledDeltaTime, Space.World);

        // Dikey Bakış (Pitch)
        float currentPitch = transform.localEulerAngles.x;
        if (currentPitch > 180) currentPitch -= 360; // -180 ile 180 arasına çek

        float newPitch = Mathf.Clamp(currentPitch + pitchDir * rotationSpeed * Time.unscaledDeltaTime, 10f, 85f);
        transform.localRotation = Quaternion.Euler(newPitch, transform.localEulerAngles.y, 0f);
    }

    void HandleZoom()
    {
        // Fare UI'ın üzerindeyken (Örn: Sağ alttaki scroll paneli içinde) Zoom çalışmasın!
        if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;
        if (CityBuilderInfoPanel.Instance != null && CityBuilderInfoPanel.Instance.IsPointerOverPanel()) return;

        // Fare tekerleği değerini alıyoruz
        float scroll = Mouse.current.scroll.ReadValue().y;

        if (Mathf.Abs(scroll) > 0.1f)
        {
            // Tekerlek ileri (scroll > 0) ise aşağı, geri ise yukarı
            Vector3 zoomDir = transform.forward * scroll;
            // Zoom hızını 10 kat artırdık (0.001'den 0.01'e)
            Vector3 newPos = transform.position + zoomDir * zoomSpeed * 0.01f; 

            // Zoom sınırlarını koru (Çok yere girmesin veya uzaya çıkmasın)
            newPos.y = Mathf.Clamp(newPos.y, minY, maxY);
            
            // NAN Koruması (Hata önleyici)
            if (!float.IsNaN(newPos.x) && !float.IsNaN(newPos.y) && !float.IsNaN(newPos.z))
            {
                transform.position = newPos;
            }
        }
    }
}