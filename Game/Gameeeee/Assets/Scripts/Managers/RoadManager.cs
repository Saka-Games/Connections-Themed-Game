using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class RoadManager : MonoBehaviour
{
    public static RoadManager Instance;

    [Header("--- YOL PREFABLARI ---")]
    public GameObject roadPrefab; 
    public GameObject cornerPrefab; 
    public GameObject tJunctionPrefab; 
    public GameObject crossroadPrefab; 

    [Header("--- GRID AYARLARI ---")]
    public float cellSize = 500f; 
    public float manualCellSize = 0f; 
    
    private bool isCellSizeInitialized = false;
    private List<GameObject> roadsList = new List<GameObject>();
    private List<GameObject> ghostRoads = new List<GameObject>();
    private Dictionary<Vector2Int, GameObject> activeRoadPieces = new Dictionary<Vector2Int, GameObject>(); 
    private Dictionary<GameObject, List<Vector4>> roadNetworkLinks = new Dictionary<GameObject, List<Vector4>>(); 

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    [ContextMenu("Yol Prefabını Bul")]
    public void AutoFindPrefabs()
    {
#if UNITY_EDITOR
        string[] guids = AssetDatabase.FindAssets("Road Lane_01 t:Prefab");
        if (guids.Length > 0)
        {
            roadPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[0]));
            isCellSizeInitialized = false;
        }
#endif
    }

    private void InitializeCellSize()
    {
        MapCreator creator = Object.FindAnyObjectByType<MapCreator>();
        if (creator != null) { cellSize = creator.cellSize; isCellSizeInitialized = true; return; }
        if (manualCellSize > 0.01f) { cellSize = manualCellSize; isCellSizeInitialized = true; return; }
        cellSize = 500f;
        isCellSizeInitialized = true;
    }

    private void SafeDestroy(GameObject obj)
    {
        if (obj == null) return;
        if (Application.isPlaying) Destroy(obj);
        else 
        {
#if UNITY_EDITOR
            if (Selection.activeGameObject == obj) Selection.activeGameObject = null;
            DestroyImmediate(obj);
#else
            Destroy(obj);
#endif
        }
    }

    public bool AddRoadLine(Vector3 start, Vector3 end)
    {
        if (!isCellSizeInitialized) InitializeCellSize();

        List<Vector3> path = CalculateEdgePath(start, end);
        if (path.Count < 2) return false;

        // 1. PARA KONTROLÜ (Kabaca her 10 birimi 1 birim yol sayalım)
        float totalDist = 0;
        for (int i = 0; i < path.Count - 1; i++) totalDist += Vector3.Distance(path[i], path[i+1]);
        int segmentCount = Mathf.RoundToInt(totalDist / 10f) + 5; 
        int totalCost = segmentCount * BuildManager.Instance.roadCost;

        if (UIManager.Instance != null && UIManager.Instance.oyuncuParasi < totalCost)
        {
            UIManager.Instance.ShowWarning("Yol inşa etmek için yeterli paranız yok!");
            ClearGhostPreview();
            return false;
        }
        if (UIManager.Instance != null) UIManager.Instance.oyuncuParasi -= totalCost;

        // 2. İNŞA ET
        GameObject lineParent = new GameObject("Road_Network_Group");
        lineParent.transform.SetParent(this.transform);
        roadsList.Add(lineParent);
        roadNetworkLinks[lineParent] = new List<Vector4>();

        for (int i = 0; i < path.Count - 1; i++)
        {
            RegisterPathNetwork(path[i], path[i+1], lineParent);
            AddStraightSegment(path[i], path[i+1], lineParent.transform);
        }

        // 3. KAVŞAKLARI VE KÖŞELERİ TAZELA (Sadece Dönüşlerde Özel Prefab)
        RefreshNetworkVisualsForPath(path);
        RefreshAffectedTiles(path);
        
        // --- DAİMİ BAĞLANTI KAYDI (ID Bazlı Nakliye Garantisi) ---
        if (NetworkManager.Instance != null)
        {
            Tile startTile = GetTileAtPos(start);
            Tile endTile = GetTileAtPos(end);
            if (startTile != null && endTile != null && startTile != endTile)
            {
                if (startTile.tileID != "" && endTile.tileID != "")
                    NetworkManager.Instance.RegisterDirectLink(startTile.tileID, endTile.tileID, path);
            }
        }

        return true;
    }

    private void RefreshAffectedTiles(List<Vector3> path)
    {
        // NOT: Bağlantı durumu artık SelectionManager tarafından ID bazlı yönetiliyor.
        // Burada isConnected'ı değiştirmiyoruz, aksi halde SelectionManager'ın kayıtlarını ezeriz.
    }

    private Tile GetTileAtPos(Vector3 pos)
    {
        // 25 birim yarıçap (daha geniş tarama) ve her katmanı tarama
        Collider[] hits = Physics.OverlapSphere(pos, 25f, ~0);
        foreach (var hit in hits)
        {
            Tile t = hit.GetComponentInParent<Tile>();
            if (t != null) return t;
        }
        return null;
    }

    private void AddStraightSegment(Vector3 p1, Vector3 p2, Transform parent)
    {
        float dist = Vector3.Distance(p1, p2);
        if (dist < 0.1f) return;

        float pLen = 10f; // Varsayılan parça boyu
        Renderer r = roadPrefab.GetComponentInChildren<Renderer>();
        if (r != null) pLen = Mathf.Max(r.bounds.size.x, r.bounds.size.z);
        if (pLen < 0.5f) pLen = 10f;

        // ÜST ÜSTE BİNDİRME (OVERLAP): Boşluk kalmaması için i <= count yapıyoruz
        int count = Mathf.RoundToInt(dist / pLen);
        Vector3 dir = (p2 - p1).normalized;

        for (int i = 0; i <= count; i++) 
        {
            Vector3 pos = p1 + (dir * (i * pLen));
            GameObject segment = Instantiate(roadPrefab, pos + Vector3.up * 0.12f, Quaternion.LookRotation(dir), parent);
            
            // FİZİKSEL TESPİT İÇİN COLLIDER GARANTİSİ
            if (segment.GetComponent<Collider>() == null && segment.GetComponentInChildren<Collider>() == null)
            {
                BoxCollider bc = segment.AddComponent<BoxCollider>();
                bc.size = new Vector3(pLen, 1f, pLen);
            }

            SetLayerRecursive(segment, 2);
            ApplyMaterialCleanup(segment, false);
        }
    }

    public void UpdateGhostPreview(Vector3 start, Vector3 end)
    {
        if (!isCellSizeInitialized) InitializeCellSize();
        ClearGhostPreview();
        if (Vector3.Distance(start, end) < 0.1f) return;

        List<Vector3> path = CalculateEdgePath(start, end);
        for (int i = 0; i < path.Count - 1; i++) AddStraightGhost(path[i], path[i+1]);
    }

    private void AddStraightGhost(Vector3 p1, Vector3 p2)
    {
        float dist = Vector3.Distance(p1, p2);
        if (dist < 0.1f) return;

        float pLen = 10f;
        Renderer r = roadPrefab.GetComponentInChildren<Renderer>();
        if (r != null) pLen = Mathf.Max(r.bounds.size.x, r.bounds.size.z);
        if (pLen < 0.5f) pLen = 10f;

        int count = Mathf.RoundToInt(dist / pLen);
        Vector3 dir = (p2 - p1).normalized;

        for (int i = 0; i <= count; i++)
        {
            Vector3 pos = p1 + (dir * (i * pLen));
            GameObject ghost = Instantiate(roadPrefab, pos + Vector3.up * 0.14f, Quaternion.LookRotation(dir), this.transform);
            SetLayerRecursive(ghost, 2);
            ApplyMaterialCleanup(ghost, true);
            ghostRoads.Add(ghost);
        }
    }

    private void RegisterPathNetwork(Vector3 p1, Vector3 p2, GameObject parent)
    {
        if (NetworkManager.Instance == null) return;
        float hCS = cellSize * 0.5f;
        Vector3 dir = (p2 - p1).normalized;
        float distance = Vector3.Distance(p1, p2);
        int steps = Mathf.RoundToInt(distance / hCS);

        Vector2Int prevNode = NetworkManager.Instance.WorldToGridNode(p1);

        for (int i = 1; i <= steps; i++)
        {
            Vector3 target = p1 + dir * (i * hCS);
            Vector2Int currNode = NetworkManager.Instance.WorldToGridNode(target);
            
            NetworkManager.Instance.RegisterLink(prevNode, currNode);
            roadNetworkLinks[parent].Add(new Vector4(
                prevNode.x * hCS, prevNode.y * hCS,
                currNode.x * hCS, currNode.y * hCS
            ));
            
            prevNode = currNode;
        }

        // --- ZİNCİR GARANTİSİ ---
        // Eğer son step p2'ye tam yetişmediyse veya geçtiyse, 
        // son node'u p2'nin node'una zorla bağla.
        Vector2Int finalNode = NetworkManager.Instance.WorldToGridNode(p2);
        if (prevNode != finalNode)
        {
            NetworkManager.Instance.RegisterLink(prevNode, finalNode);
        }
    }

    private void RefreshNetworkVisualsForPath(List<Vector3> path)
    {
        if (NetworkManager.Instance == null) return;
        HashSet<Vector2Int> nodes = new HashSet<Vector2Int>();
        float hCS = cellSize * 0.5f;

        foreach (var p in path) nodes.Add(NetworkManager.Instance.WorldToGridNode(p));
        
        // Komşuları da ekle ki değişen kavşaklar güncellensin
        List<Vector2Int> toUpdate = new List<Vector2Int>(nodes);
        foreach (var node in nodes)
        {
            foreach (var neighbor in NetworkManager.Instance.GetNeighbors(node)) toUpdate.Add(neighbor);
        }

        foreach (var node in toUpdate) UpdateNodeVisual(node);
    }

    public void DeleteRoadAtPiece(GameObject piece)
    {
        if (piece == null) return;
        GameObject parent = (piece.transform.parent != null && piece.transform.parent != transform) ? piece.transform.parent.gameObject : piece;

        HashSet<Vector2Int> affectedNodes = new HashSet<Vector2Int>();

        if (roadNetworkLinks.ContainsKey(parent))
        {
            foreach (Vector4 link in roadNetworkLinks[parent])
            {
                Vector3 s = new Vector3(link.x, 0, link.y);
                Vector3 n = new Vector3(link.z, 0, link.w);
                NetworkManager.Instance.RemoveLink(s, n);
                affectedNodes.Add(NetworkManager.Instance.WorldToGridNode(s));
                affectedNodes.Add(NetworkManager.Instance.WorldToGridNode(n));
            }
            roadNetworkLinks.Remove(parent);
        }

        if (roadsList.Contains(parent)) roadsList.Remove(parent);
        SafeDestroy(parent);

        // Kavşakları güncelle
        foreach (var node in affectedNodes) UpdateNodeVisual(node);
        RefreshAllTiles();
    }

    public void ResetRoadPoints() { }

    private void UpdateNodeVisual(Vector2Int node)
    {
        if (NetworkManager.Instance == null) return;
        
        if (activeRoadPieces.ContainsKey(node))
        {
            SafeDestroy(activeRoadPieces[node]);
            activeRoadPieces.Remove(node);
        }

        HashSet<Vector2Int> neighbors = NetworkManager.Instance.GetNeighbors(node);
        if (neighbors.Count == 0) return;

        float hCS = cellSize * 0.5f;
        Vector3 pos = new Vector3(Mathf.Round(node.x * hCS), 0.12f, Mathf.Round(node.y * hCS));
        
        List<Vector3> nPos = new List<Vector3>();
        foreach (var n in neighbors) nPos.Add(new Vector3(Mathf.Round(n.x * hCS), 0.12f, Mathf.Round(n.y * hCS)));
         GameObject prefab = null;
        Quaternion rot = Quaternion.identity;

        if (neighbors.Count == 2)
        {
            Vector3 d1 = (nPos[0] - pos).normalized;
            Vector3 d2 = (nPos[1] - pos).normalized;
            if (Vector3.Angle(d1, d2) < 170f) // Köşe
            {
                prefab = cornerPrefab;
                // Rotasyon: İki komşunun ortasına bakarak çapraz yerleşir
                rot = Quaternion.LookRotation(d1 + d2);
            }
        }
        else if (neighbors.Count == 3)
        {
            prefab = tJunctionPrefab;
            Vector3 sum = Vector3.zero;
            foreach (var n in nPos) sum += (n - pos).normalized;
            rot = Quaternion.LookRotation(sum);
        }
        else if (neighbors.Count == 4)
        {
            prefab = crossroadPrefab;
        }

        if (prefab != null)
        {
            GameObject piece = Instantiate(prefab, pos, rot, this.transform);
            piece.transform.localScale = Vector3.one; // ÖLÇEKLENDİRME YOK
            SetLayerRecursive(piece, 2);
            ApplyMaterialCleanup(piece, false);
            activeRoadPieces[node] = piece;
        }
    }

    private void RefreshAllTiles()
    {
        MapCreator creator = Object.FindAnyObjectByType<MapCreator>();
        if (creator != null && NetworkManager.Instance != null)
        {
            foreach (var pair in creator.gridData) pair.Value.isConnected = NetworkManager.Instance.IsTileConnected(pair.Value);
        }
    }

    public List<Vector3> CalculateEdgePath(Vector3 start, Vector3 end)
    {
        float hCS = cellSize * 0.5f;
        List<Vector3> pts = new List<Vector3> { start };

        float dx = end.x - start.x;
        float dz = end.z - start.z;

        // YÖN VE OFSET BELİRLEME
        Vector3 dirX = (Mathf.Abs(dx) > 1f) ? new Vector3(Mathf.Sign(dx) * hCS, 0, 0) : new Vector3(hCS, 0, 0); 
        Vector3 dirZ = (Mathf.Abs(dz) > 1f) ? new Vector3(0, 0, Mathf.Sign(dz) * hCS) : new Vector3(0, 0, hCS);

        // CASE 1: AYNI SATIRDA (dz == 0) -> Üstten/Alttan U-Dönüşü yap
        if (Mathf.Abs(dz) < 1f)
        {
            pts.Add(start + dirZ); 
            pts.Add(new Vector3(Mathf.Round(end.x), start.y, Mathf.Round(start.z + dirZ.z)));
        }
        // CASE 2: AYNI SÜTUNDA (dx == 0) -> Sağdan/Soldan U-Dönüşü yap
        else if (Mathf.Abs(dx) < 1f)
        {
            pts.Add(start + dirX);
            pts.Add(new Vector3(Mathf.Round(start.x + dirX.x), start.y, Mathf.Round(end.z)));
        }
        // CASE 3: DİYAGONAL (L-Shape) -> Köşeden (Vertex) dön
        else
        {
            pts.Add(start + dirX); // Exit to Edge
            pts.Add(new Vector3(Mathf.Round(start.x + dirX.x), 0, Mathf.Round(end.z - dirZ.z))); // Vertex
            pts.Add(new Vector3(Mathf.Round(end.x), 0, Mathf.Round(end.z - dirZ.z))); // Entry to Edge
        }

        pts.Add(end);
        return pts;
    }

    public void ClearGhostPreview() {
        foreach (var g in ghostRoads) SafeDestroy(g);
        ghostRoads.Clear();
    }

    public void ClearRoads() {
        foreach (var r in roadsList) SafeDestroy(r);
        roadsList.Clear();
        roadNetworkLinks.Clear();
    }

    private void SetLayerRecursive(GameObject obj, int layer) {
        if (obj == null) return; 
        obj.layer = layer;
        foreach (Transform child in obj.transform) SetLayerRecursive(child.gameObject, layer);
    }

    private void ApplyMaterialCleanup(GameObject obj, bool isGhost) {
        foreach (var r in obj.GetComponentsInChildren<Renderer>()) {
            foreach (var mat in r.materials) {
                Shader s = Shader.Find("Universal Render Pipeline/Lit");
                if (s != null) mat.shader = s;
                if (isGhost) {
                    mat.SetFloat("_Surface", 1);
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.renderQueue = 3000;
                    Color c = mat.color; c.a = 0.5f; mat.color = c;
                }
            }
        }
    }
}
