using UnityEngine;

public class BuildManager : MonoBehaviour
{
    public static BuildManager Instance;

    [Header("Build States")]
    public bool isRoadMode = false;
    public int roadCost = 100;

    [Header("Linear Road Build (C:S Style)")]
    public Vector3 firstRoadPoint;
    public Tile firstRoadTile;
    public bool isWaitingForSecondClick = false;

    [Header("Road Prefab (Optional)")]
    public GameObject roadPrefab;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void ToggleRoadMode()
    {
        isRoadMode = !isRoadMode;
        isWaitingForSecondClick = false; // Her seferinde sıfırdan başla
        
        if (RoadManager.Instance != null)
            RoadManager.Instance.ClearGhostPreview();

        // Eğer yol moduna girdiysek mevcut bina seçimini temizleyelim (Karışıklık olmasın)
        if (isRoadMode && SelectionManager.Instance != null)
        {
            SelectionManager.Instance.ClearSelection();
        }
        else if (!isRoadMode && RoadManager.Instance != null)
        {
            RoadManager.Instance.ResetRoadPoints();
        }

        Debug.Log($"<color=cyan>[BUILD]:</color> Yol Modu: {(isRoadMode ? "AÇIK" : "KAPALI")}");
    }
}
