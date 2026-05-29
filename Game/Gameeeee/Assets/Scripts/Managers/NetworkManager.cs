using UnityEngine;
using System.Collections.Generic;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance;

    // Atomik Grid Düğümleri: (x,z) -> List<(nx,nz)>
    // Her bir birim = cellSize / 2 kadardır.
    // --- DAİMİ ID-TABANLI BAĞLANTILAR (Hatasız Lojistik ve Gidiş/Geliş Şekli için) ---
    // [System.Serializable] sayesinde Unity Inspector'da bu listeyi görebiliriz.
    [System.Serializable]
    public class RoadConnection
    {
        public string id1;
        public string id2;
        public List<Vector3> visualPath;
    }

    public List<RoadConnection> activeConnections = new List<RoadConnection>();
    
    // Geçmiş verilerle uyumluluk için sözlüğü silmiyoruz (Performans artısı için arkada kullanılır)
    private Dictionary<string, List<Vector3>> directIdLinks = new Dictionary<string, List<Vector3>>();

    // --- Daimi Bağlantı Kayıt Listesi (Persistent Registry) ---
    // Bir binayı yolla tıkladığında, ID'si buraya girer ve "Ebedi Bağlantı" tescillenir.
    public HashSet<string> connectedBuildingIds = new HashSet<string>();

    public void RegisterDirectLink(string id1, string id2, List<Vector3> visualPath)
    {
        if (string.IsNullOrEmpty(id1) || string.IsNullOrEmpty(id2) || visualPath == null) return;
        
        // Sözlüğe de ekle (Hızlı kargo rotalaması için)
        directIdLinks[id1 + ":" + id2] = new List<Vector3>(visualPath);
        
        List<Vector3> reversePath = new List<Vector3>(visualPath);
        reversePath.Reverse();
        directIdLinks[id2 + ":" + id1] = reversePath;

        // --- YENİ: İKİLİ (PAIRWISE) LİSTEYE EKLE ---
        // Eğer bu iki ID arasında daha önce kayıt varsa boşuna ekleme yapma
        bool exists = false;
        foreach (var conn in activeConnections)
        {
            if ((conn.id1 == id1 && conn.id2 == id2) || (conn.id1 == id2 && conn.id2 == id1))
            {
                exists = true;
                break;
            }
        }

        if (!exists)
        {
            activeConnections.Add(new RoadConnection { id1 = id1, id2 = id2, visualPath = new List<Vector3>(visualPath) });
        }
    }

    public List<Vector3> GetDirectPath(string id1, string id2)
    {
        string key = id1 + ":" + id2;
        if (directIdLinks.ContainsKey(key)) return directIdLinks[key];
        return null;
    }

    public bool AreDirectlyConnected(string id1, string id2)
    {
        if (string.IsNullOrEmpty(id1) || string.IsNullOrEmpty(id2)) return false;
        return directIdLinks.ContainsKey(id1 + ":" + id2);
    }

    private Dictionary<Vector2Int, HashSet<Vector2Int>> pointGraph = new Dictionary<Vector2Int, HashSet<Vector2Int>>();

    public bool HasNode(Vector2Int node) => pointGraph.ContainsKey(node);

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Dünya pozisyonunu alıp tam tamsayı grid indeksine çevirir (CS/2 hassasiyetinde)
    public Vector2Int WorldToGridNode(Vector3 worldPos)
    {
        float CS = 10f; 
        if (RoadManager.Instance != null && RoadManager.Instance.cellSize > 0.1f) 
            CS = RoadManager.Instance.cellSize;
        
        int gx = Mathf.RoundToInt(worldPos.x / (CS * 0.5f));
        int gz = Mathf.RoundToInt(worldPos.z / (CS * 0.5f));
        return new Vector2Int(gx, gz);
    }

    public void RegisterLink(Vector3 p1, Vector3 p2)
    {
        RegisterLink(WorldToGridNode(p1), WorldToGridNode(p2));
    }

    public void RegisterLink(Vector2Int n1, Vector2Int n2)
    {
        if (n1 == n2) return;

        if (!pointGraph.ContainsKey(n1)) pointGraph[n1] = new HashSet<Vector2Int>();
        if (!pointGraph.ContainsKey(n2)) pointGraph[n2] = new HashSet<Vector2Int>();

        pointGraph[n1].Add(n2);
        pointGraph[n2].Add(n1);

        // Debug.Log($"<color=cyan>[NETWORK]:</color> Link: {n1} <-> {n2}");
    }

    public void RemoveLink(Vector3 p1, Vector3 p2)
    {
        RemoveLink(WorldToGridNode(p1), WorldToGridNode(p2));
    }

    public void RemoveLink(Vector2Int n1, Vector2Int n2)
    {
        if (pointGraph.ContainsKey(n1)) pointGraph[n1].Remove(n2);
        if (pointGraph.ContainsKey(n2)) pointGraph[n2].Remove(n1);
    }

    // --- AKILLI AURA TESCİLİ (YENİ) ---
    // Yol inşa edildiği anda etraftaki binaları mühürler. Artık her karede radar çalışmasına gerek yok!
    public void RegisterNearbyBuildings(Vector3 pos)
    {
        // 500-600 birimlik bir "Aura" içinde binaları ara
        Collider[] hits = Physics.OverlapSphere(pos, 550f, ~0);
        foreach (var h in hits)
        {
            Tile t = h.GetComponent<Tile>();
            if (t == null) t = h.GetComponentInParent<Tile>();

            if (t != null && !string.IsNullOrEmpty(t.tileID))
            {
                // Eğer bu bir yol veya çayır değilse (Bina ise), mühürle!
                if (t.type != TileType.Road && t.type != TileType.Meadow)
                {
                    connectedBuildingIds.Add(t.tileID);
                    // Debug.Log($"<color=cyan>[NETWORK]:</color> {t.name} (ID:{t.tileID}) Aura ile mühürlendi.");
                }
            }
        }
    }

    public bool IsTileConnected(Tile tile)
    {
        if (tile == null) return false;

        // DEBUG: Bu metod çağrılıyor mu ve ne arıyor?
        Debug.Log($"<color=magenta>[DEBUG IsTileConnected]:</color> Tile={tile.name}, ID={tile.tileID}, connectedIds={connectedBuildingIds.Count}, activeConns={activeConnections.Count}");

        if (!string.IsNullOrEmpty(tile.tileID))
        {
            // 1. İkili Kayıt Kontrolü (A <-> B)
            foreach (var conn in activeConnections)
            {
                if (conn.id1 == tile.tileID || conn.id2 == tile.tileID)
                {
                    Debug.Log($"<color=green>[BAĞLANTI BULUNDU]:</color> {tile.name} activeConnections'da!");
                    return true;
                }
            }

            // 2. Tekli Kayıt Kontrolü (HashSet)
            if (connectedBuildingIds.Contains(tile.tileID))
            {
                Debug.Log($"<color=green>[BAĞLANTI BULUNDU]:</color> {tile.name} connectedBuildingIds'da!");
                return true;
            }

            // 3. Geriye Dönük Lojistik Sözlüğü
            foreach (var key in directIdLinks.Keys)
            {
                if (key.StartsWith(tile.tileID + ":") || key.EndsWith(":" + tile.tileID))
                {
                    Debug.Log($"<color=green>[BAĞLANTI BULUNDU]:</color> {tile.name} directIdLinks'de!");
                    return true;
                }
            }
        }

        Debug.Log($"<color=red>[BAĞLANTI YOK]:</color> {tile.name} (ID:{tile.tileID}) hiçbir listede bulunamadı!");
        return false;
    }
     public List<Vector3> GetPath(Vector3 start, Vector3 end)
    {
        Vector2Int nStart = WorldToGridNode(start);
        Vector2Int nEnd = WorldToGridNode(end);
        
        if (nStart == nEnd) return new List<Vector3> { start, end };
        if (!pointGraph.ContainsKey(nStart) || !pointGraph.ContainsKey(nEnd)) return null;

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> parent = new Dictionary<Vector2Int, Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        queue.Enqueue(nStart);
        visited.Add(nStart);

        bool found = false;
        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            if (current == nEnd) { found = true; break; }

            foreach (Vector2Int neighbor in pointGraph[current])
            {
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    parent[neighbor] = current;
                    queue.Enqueue(neighbor);
                }
            }
        }

        if (!found) return null;

        // Yolu Geriye Doğru Oluştur
        List<Vector3> path = new List<Vector3>();
        Vector2Int curr = nEnd;
        float halfCS = (RoadManager.Instance != null) ? RoadManager.Instance.cellSize * 0.5f : 10f;

        while (curr != nStart)
        {
            path.Add(new Vector3(curr.x * halfCS, 0, curr.y * halfCS));
            curr = parent[curr];
        }
        path.Add(new Vector3(nStart.x * halfCS, 0, nStart.y * halfCS));
        path.Reverse();

        // Hassasiyet için başlangıç ve bitişi gerçek bina merkezlerine çek
        if (path.Count > 0) path[0] = start;
        if (path.Count > 0) path[path.Count - 1] = end;

        return path;
    }

    public bool IsPathExists(Vector3 start, Vector3 end)
    {
        return GetPath(start, end) != null;
    }

    public HashSet<Vector2Int> GetNeighbors(Vector2Int node)
    {
        if (pointGraph.ContainsKey(node)) return pointGraph[node];
        return new HashSet<Vector2Int>();
    }

    public void ClearAll() { pointGraph.Clear(); }

    // --- BAĞLI BİNALARI BULMA (BFS) ---
    public List<string> GetReachableBuildingNames(Tile startTile)
    {
        List<string> results = new List<string>();
        if (startTile == null) return results;

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        HashSet<ProductionBuilding> foundBuildings = new HashSet<ProductionBuilding>();

        // Başlangıç Noktaları: 7x7 Alan (49 nokta)
        Vector2Int centerNode = WorldToGridNode(startTile.GetVisualCenter());
        for (int x = centerNode.x - 3; x <= centerNode.x + 3; x++)
        {
            for (int z = centerNode.y - 3; z <= centerNode.y + 3; z++)
            {
                Vector2Int node = new Vector2Int(x, z);
                if (pointGraph.ContainsKey(node) && !visited.Contains(node))
                {
                    queue.Enqueue(node);
                    visited.Add(node);
                }
            }
        }
         MapCreator creator = Object.FindAnyObjectByType<MapCreator>();
         while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            // Bu node bir binaya mı ait? (Snap point değilse merkezdir)
            if (creator != null)
            {
                // Node koordinatından Tile koordinatına (250 birim -> 500 birim)
                int tx = Mathf.RoundToInt(current.x / 2f);
                int tz = Mathf.RoundToInt(current.y / 2f);
                Vector2Int tileKey = new Vector2Int(tx, tz);

                if (creator.gridData.TryGetValue(tileKey, out Tile tile))
                {
                    ProductionBuilding pb = tile.GetComponent<ProductionBuilding>();
                    if (pb == null) pb = tile.GetComponentInChildren<ProductionBuilding>();
                    
                    if (pb != null && tile != startTile)
                    {
                        foundBuildings.Add(pb);
                    }
                }
            }

            foreach (Vector2Int neighbor in pointGraph[current])
            {
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }

        foreach (var b in foundBuildings) results.Add(b.binaAdi);
        // --- DAİMİ BAĞLANTILAR (ID Bazlı) ---
        if (!string.IsNullOrEmpty(startTile.tileID))
        {
            foreach (var key in directIdLinks.Keys)
            {
                if (key.StartsWith(startTile.tileID + ":"))
                {
                    string targetId = key.Split(':')[1];
                    // Bu ID'ye sahip binayı bul
                    foreach (var b in LogisticsManager.Instance.GetBuildings())
                    {
                        if (b != null && b.GetComponentInParent<Tile>()?.tileID == targetId)
                        {
                            results.Add(b.binaAdi != "" ? b.binaAdi : b.binaTipi.ToString());
                        }
                    }
                }
            }
        }

        return results;
    }
}
