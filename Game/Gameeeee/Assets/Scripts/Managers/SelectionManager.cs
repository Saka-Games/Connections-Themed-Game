using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class SelectionManager : MonoBehaviour
{
    public static SelectionManager Instance;
    
    public Color hoverColor = new Color(0.12f, 0.45f, 1f, 1f); 
    
    private Tile currentlySelectedTile;
    private Dictionary<Renderer, Color> originalColors = new Dictionary<Renderer, Color>();
    private LineRenderer selectionOutline; // Hover/Genel Seçim
    private LineRenderer roadStartOutline; // Yol Başlangıç Noktası (Kalıcı)

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        
        if (FindAnyObjectByType<BuildManager>() == null) gameObject.AddComponent<BuildManager>();
        if (FindAnyObjectByType<RoadManager>() == null) gameObject.AddComponent<RoadManager>();
        if (FindAnyObjectByType<NetworkManager>() == null) gameObject.AddComponent<NetworkManager>();

        CreateOutlineRenderers();
    }

    private void CreateOutlineRenderers()
    {
        if (transform.Find("TileSelectionOutline") == null)
        {
            GameObject outlineObj = new GameObject("TileSelectionOutline");
            outlineObj.transform.SetParent(this.transform);
            selectionOutline = CreateOutlineOnObject(outlineObj, Color.red);
        }

        if (transform.Find("RoadStartOutline") == null)
        {
            GameObject roadObj = new GameObject("RoadStartOutline");
            roadObj.transform.SetParent(this.transform);
            roadStartOutline = CreateOutlineOnObject(roadObj, new Color(1f, 0.5f, 0f)); // Turuncu
        }
    }

    private LineRenderer CreateOutlineOnObject(GameObject obj, Color color)
    {
        LineRenderer lr = obj.AddComponent<LineRenderer>();
        lr.positionCount = 5; lr.loop = true;
        lr.startWidth = 15f; lr.endWidth = 15f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = color; lr.endColor = color;
        lr.useWorldSpace = true;
        lr.enabled = false;
        return lr;
    }

    void Update()
    {
        // 1. UI KONTROLÜ: Eğer fare bir UI elemanı üzerindeyse seçimi tamamen engelle
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) 
        {
            if (selectionOutline != null && !BuildManager.Instance.isWaitingForSecondClick) 
                selectionOutline.enabled = false;
            return;
        }

        if (Mouse.current == null) return;
        if (CityBuilderInfoPanel.Instance != null && CityBuilderInfoPanel.Instance.IsPointerOverPanel()) return;

        HandleWorldSelection();
    }

    private void HandleWorldSelection()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        
        // HATA ÖNLEME: Eğer fare pozisyonu NaN (geçersiz) ise ScreenPointToRay hata verir. 
        if (float.IsNaN(mousePos.x) || float.IsNaN(mousePos.y)) return;
        if (Camera.main == null) return;

        Ray ray = Camera.main.ScreenPointToRay(mousePos);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100000f))
        {
            Transform hitTransform = hit.collider.transform;

            if (BuildManager.Instance != null && BuildManager.Instance.isRoadMode)
            {
                Tile hoverTile = FindComponentUpwards<Tile>(hitTransform);
                Vector3 snappedPoint = GetSnappedPoint(hit.point);
                Vector3 hoverPoint = (hoverTile != null) ? hoverTile.GetVisualCenter() : snappedPoint;

                if (hoverTile != null) DrawOutlineAtPoint(selectionOutline, hoverTile, hoverPoint);
                else if (selectionOutline != null) selectionOutline.enabled = false;

                if (BuildManager.Instance.isWaitingForSecondClick)
                {
                    if (RoadManager.Instance != null)
                        RoadManager.Instance.UpdateGhostPreview(BuildManager.Instance.firstRoadPoint, snappedPoint);
                }
            }

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (BuildManager.Instance != null && BuildManager.Instance.isRoadMode)
                {
                    // --- YOL SİLME KONTROLÜ ---
                    bool isRoadHit = hitTransform.name.Contains("Road") || hitTransform.parent?.name.Contains("Road") == true;
                    if (isRoadHit)
                    {
                        RoadManager.Instance.DeleteRoadAtPiece(hitTransform.gameObject);
                        return;
                    }

                    Vector3 clickPoint = GetSnappedPoint(hit.point);
                    ExecuteRoadPlacement(clickPoint, FindComponentUpwards<Tile>(hitTransform));
                    return;
                }
                ProcessSelection(hit, hitTransform);
            }
        }
        else
        {
            if (selectionOutline != null) selectionOutline.enabled = false;
            if (Mouse.current.leftButton.wasPressedThisFrame) ClearSelection();
        }
    }

    private void ExecuteRoadPlacement(Vector3 point, Tile tile)
    {
        if (BuildManager.Instance == null || RoadManager.Instance == null) return;

        if (!BuildManager.Instance.isWaitingForSecondClick)
        {
            // --- İLK TIKLAMA: Başlangıç noktasını kaydet ---
            BuildManager.Instance.firstRoadPoint = point;
            BuildManager.Instance.firstRoadTile = tile;
            BuildManager.Instance.isWaitingForSecondClick = true;
            
            // Köprü Koruması: BAŞLANGIÇ karosunun merkeziyle tıkladığımız kenarı ağda bağla
            if (tile != null && NetworkManager.Instance != null)
                NetworkManager.Instance.RegisterLink(tile.GetVisualCenter(), point);

            // DEBUG: Başlangıç tile'ını logla
            Debug.Log($"<color=yellow>[YOL]:</color> Başlangıç tile: {(tile != null ? tile.name + " (ID:" + tile.tileID + ")" : "NULL - Boş alana tıklandı")}");

            // Başlangıç noktasını kalıcı olarak işaretle
            DrawOutlineAtPoint(roadStartOutline, tile, point);
        }
        else
        {
            // --- İKİNCİ TIKLAMA: Yolu inşa et ---
            if (Vector3.Distance(BuildManager.Instance.firstRoadPoint, point) < 0.1f) return;

            // Köprü Koruması: BİTİŞ karosunun merkeziyle tıkladığımız kenarı ağda bağla
            if (tile != null && NetworkManager.Instance != null)
                NetworkManager.Instance.RegisterLink(tile.GetVisualCenter(), point);
            
            // Köprü Koruması (Garanti): İlk karoyu tekrar bağlayalım
            if (BuildManager.Instance.firstRoadTile != null && NetworkManager.Instance != null)
                NetworkManager.Instance.RegisterLink(BuildManager.Instance.firstRoadTile.GetVisualCenter(), BuildManager.Instance.firstRoadPoint);

            // DEBUG: Bitiş tile'ını logla
            Debug.Log($"<color=yellow>[YOL]:</color> Bitiş tile: {(tile != null ? tile.name + " (ID:" + tile.tileID + ")" : "NULL - Boş alana tıklandı")}");

            bool success = RoadManager.Instance.AddRoadLine(BuildManager.Instance.firstRoadPoint, point);
            BuildManager.Instance.isWaitingForSecondClick = false;
            
            if (success)
            {
                // --- DAİMİ BAĞLANTI KAYDI ---
                if (NetworkManager.Instance != null)
                {
                    // 1. KESİN TESCİL: Tile'ların ID'sini doğrudan kaydet.
                    Tile startTile = BuildManager.Instance.firstRoadTile;
                    
                    if (startTile != null && !string.IsNullOrEmpty(startTile.tileID))
                    {
                        NetworkManager.Instance.connectedBuildingIds.Add(startTile.tileID);
                        startTile.isConnected = true;
                        Debug.Log($"<color=green>[BAĞLANTI]:</color> {startTile.name} (ID:{startTile.tileID}) BAĞLI olarak mühürlendi!");
                    }
                    
                    if (tile != null && !string.IsNullOrEmpty(tile.tileID))
                    {
                        NetworkManager.Instance.connectedBuildingIds.Add(tile.tileID);
                        tile.isConnected = true;
                        Debug.Log($"<color=green>[BAĞLANTI]:</color> {tile.name} (ID:{tile.tileID}) BAĞLI olarak mühürlendi!");
                    }

                    // 2. İKİLİ (PAIRWISE) KAYIT
                    if (startTile != null && tile != null)
                    {
                        string id1 = startTile.tileID;
                        string id2 = tile.tileID;
                        if (!string.IsNullOrEmpty(id1) && !string.IsNullOrEmpty(id2))
                        {
                            List<Vector3> visualPath = RoadManager.Instance.CalculateEdgePath(BuildManager.Instance.firstRoadPoint, point);
                            NetworkManager.Instance.RegisterDirectLink(id1, id2, visualPath);
                        }
                    }
                }

                if (UIManager.Instance != null) UIManager.Instance.ShowWarning("Yol İnşası Tamamlandı!");
                if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(AudioManager.Instance.yolYapmaSesi);
                
                // Yol yapıldıktan sonra paneli tazele
                SelectObject(tile, FindComponentUpwards<ProductionBuilding>(tile.transform)); 
            }
        }
    }

    private void ProcessSelection(RaycastHit hit, Transform hitTransform)
    {
        Tile tile = FindComponentUpwards<Tile>(hitTransform);
        ProductionBuilding building = FindComponentUpwards<ProductionBuilding>(hitTransform);
        if (tile != null || building != null) SelectObject(tile, building);
    }

    private void SelectObject(Tile tile, ProductionBuilding building)
    {
        ClearSelection();
        if (tile == null) return;

        if (AudioManager.Instance != null)
        {
            if (tile.type == TileType.City) AudioManager.Instance.PlaySFX(AudioManager.Instance.sehirSesi);
            else if (tile.type == TileType.Factory) AudioManager.Instance.PlaySFX(AudioManager.Instance.fabrikaSesi);
            else if (tile.type == TileType.Farm) AudioManager.Instance.PlayRandomSFX(AudioManager.Instance.tarlaSesleri);
            else if (tile.type == TileType.Pasture) AudioManager.Instance.PlayRandomSFX(AudioManager.Instance.meraSesleri);
            
            if (building != null && building.binaTipi == ProductionBuilding.BuildingType.Firin)
                AudioManager.Instance.PlaySFX(AudioManager.Instance.firinSesi);
        }

        if (UIManager.Instance != null && building != null) UIManager.Instance.OpenBuildingPanel(building);

        currentlySelectedTile = tile;
        HighlightTile(tile);
        DrawOutlineAtPoint(selectionOutline, tile, tile.GetVisualCenter());

        if (CityBuilderInfoPanel.Instance != null) CityBuilderInfoPanel.Instance.ShowPanel(tile, building);
    }

    private void HighlightTile(Tile tile)
    {
        if (BuildManager.Instance != null && BuildManager.Instance.isRoadMode) return;
        foreach (Renderer rend in tile.GetComponentsInChildren<Renderer>())
        {
            if (!originalColors.ContainsKey(rend) && rend.material.HasProperty("_Color"))
            {
                originalColors[rend] = rend.material.color;
                rend.material.color = Color.Lerp(rend.material.color, hoverColor, 0.6f);
            }
        }
    }

    private void DrawOutlineAtPoint(LineRenderer outline, Tile tile, Vector3 centerPoint)
    {
        if (outline == null || tile == null) return;
        
        float yOffset = 25f; 
        float h = tile.cellSize / 2f;
        float yPos = centerPoint.y + yOffset;

        Vector3 p1 = new Vector3(centerPoint.x - h, yPos, centerPoint.z - h);
        Vector3 p2 = new Vector3(centerPoint.x - h, yPos, centerPoint.z + h);
        Vector3 p3 = new Vector3(centerPoint.x + h, yPos, centerPoint.z + h);
        Vector3 p4 = new Vector3(centerPoint.x + h, yPos, centerPoint.z - h);

        outline.SetPosition(0, p1);
        outline.SetPosition(1, p2);
        outline.SetPosition(2, p3);
        outline.SetPosition(3, p4);
        outline.SetPosition(4, p1);
        outline.enabled = true;
    }

    public void ClearSelection()
    {
        foreach (var kvp in originalColors) { if (kvp.Key != null) kvp.Key.material.color = kvp.Value; }
        originalColors.Clear();
        currentlySelectedTile = null;
        if (selectionOutline != null) selectionOutline.enabled = false;
        if (roadStartOutline != null) roadStartOutline.enabled = false;
        if (CityBuilderInfoPanel.Instance != null) CityBuilderInfoPanel.Instance.HidePanel();
    }

    private T FindComponentUpwards<T>(Transform current) where T : Component
    {
        while (current != null) { T comp = current.GetComponent<T>(); if (comp != null) return comp; current = current.parent; }
        return null;
    }

    private Vector3 GetSnappedPoint(Vector3 point)
    {
        float CS = 10f;
        if (RoadManager.Instance != null) CS = RoadManager.Instance.cellSize;
        
        float snapUnit = CS * 0.5f;
        float x = Mathf.Round(point.x / snapUnit) * snapUnit;
        float z = Mathf.Round(point.z / snapUnit) * snapUnit;
        return new Vector3(x, point.y, z);
    }
}