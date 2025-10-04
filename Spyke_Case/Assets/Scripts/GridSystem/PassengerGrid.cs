using UnityEngine;
using System.Collections.Generic;
using GridSystem.Data;

namespace GridSystem
{
    public class PassengerGrid : MonoBehaviour
    {
        [Header("Grid Ayarları")]
        public GridData gridData;
        public GameObject cellCubePrefab;

        [Header("Grid Görünüm")]
        [Tooltip("Grid çizgilerinin görünürlüğü")]
        public bool showGridLines = true;
        [Tooltip("Grid çizgilerinin rengi")]
        public Color gridLineColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

        [Header("Yolcu Sistemi")]
        public GameObject passengerGroupPrefab;

        private bool isInitialized;

    // Backwards-compatible convenience properties for legacy code
    public int gridWidth { get { return gridData != null ? gridData.width : 0; } set { if (gridData != null) gridData.width = value; } }
    public int gridHeight { get { return gridData != null ? gridData.height : 0; } set { if (gridData != null) gridData.height = value; } }
    public float cellSize { get { return gridData != null ? gridData.cellSize : 1f; } set { if (gridData != null) gridData.cellSize = value; } }
    public Vector3 gridWorldOffset { get { return gridData != null ? gridData.worldOffset : Vector3.zero; } set { if (gridData != null) gridData.worldOffset = value; } }

        private void Start()
        {
            if (gridData != null)
            {
                InitializeGrid();
            }
        }

        private void InitializeGrid()
        {
            // Grid başlatma işlemleri
            isInitialized = true;
        }

        // Allow editor scripts to trigger validation from the inspector
        public void OnValidate()
        {
            if (gridData != null)
                gridData.OnValidate();
        }

        public Vector3 GetWorldPosition(Vector2Int gridPos)
        {
            return transform.position + new Vector3(gridPos.x * gridData.cellSize, 0, gridPos.y * gridData.cellSize) + gridData.worldOffset;
        }

        public GridCell GetCell(int x, int y)
        {
            if (gridData == null || x < 0 || y < 0 || x >= gridData.width || y >= gridData.height)
                return null;

            int index = y * gridData.width + x;
            return index < gridData.cells.Count ? gridData.cells[index] : null;
        }

        public bool IsCellWalkable(int x, int y)
        {
            GridCell cell = GetCell(x, y);
            if (cell == null) return false;
            // If a PassengerGroup occupies this grid pos, treat it as not walkable
            var groups = FindObjectsOfType<PassengerGroup>();
            foreach (var g in groups)
            {
                if (g != null && g.gridPos.x == x && g.gridPos.y == y)
                    return false;
            }

            return (cell.cellType == GridCellType.Walkable || 
                    cell.cellType == GridCellType.WaitingArea || 
                    cell.cellType == GridCellType.Stop);
        }

        // Find shortest path from 'from' to the nearest free Stop cell.
        // Allowed traversal types: Walkable and Stop. Cells occupied by PassengerGroup are treated as blocked.
        // Returns a list of grid positions (including the target Stop), or null if none found.
        public List<Vector2Int> FindNearestStopPath(Vector2Int from)
        {
            if (gridData == null) return null;

            int w = gridData.width;
            int h = gridData.height;

            bool[,] visited = new bool[w, h];
            Vector2Int[,] parent = new Vector2Int[w, h];
            Queue<Vector2Int> q = new Queue<Vector2Int>();

            q.Enqueue(from);
            visited[from.x, from.y] = true;

            Vector2Int[] dirs = new Vector2Int[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

            while (q.Count > 0)
            {
                var cur = q.Dequeue();
                var cell = GetCell(cur.x, cur.y);
                if (cell != null && cell.cellType == GridCellType.Stop)
                {
                    // Ensure stop is free (no passenger)
                    bool occupied = false;
                    var groups = FindObjectsOfType<PassengerGroup>();
                    foreach (var g in groups)
                    {
                        if (g != null && g.gridPos == cur)
                        {
                            occupied = true;
                            break;
                        }
                    }
                    if (!occupied)
                    {
                        // Reconstruct path
                        List<Vector2Int> path = new List<Vector2Int>();
                        Vector2Int p = cur;
                        while (p != from)
                        {
                            path.Add(p);
                            p = parent[p.x, p.y];
                        }
                        path.Reverse();
                        return path;
                    }
                }

                // expand neighbors (only Walkable or Stop)
                foreach (var d in dirs)
                {
                    Vector2Int nx = cur + d;
                    if (nx.x < 0 || nx.y < 0 || nx.x >= w || nx.y >= h) continue;
                    if (visited[nx.x, nx.y]) continue;
                    var nc = GetCell(nx.x, nx.y);
                    if (nc == null) continue;
                    // check occupancy
                    bool occ = false;
                    var groups = FindObjectsOfType<PassengerGroup>();
                    foreach (var g in groups)
                    {
                        if (g != null && g.gridPos == nx)
                        {
                            occ = true; break;
                        }
                    }
                    if (occ) continue;

                    if (nc.cellType == GridCellType.Walkable || nc.cellType == GridCellType.Stop)
                    {
                        visited[nx.x, nx.y] = true;
                        parent[nx.x, nx.y] = cur;
                        q.Enqueue(nx);
                    }
                }
            }

            return null;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!showGridLines || gridData == null) return;

            Gizmos.color = gridLineColor;
            
            // Yatay çizgiler
            for (int z = 0; z <= gridData.height; z++)
            {
                Vector3 start = transform.position + new Vector3(0, 0, z * gridData.cellSize) + gridData.worldOffset;
                Vector3 end = transform.position + new Vector3(gridData.width * gridData.cellSize, 0, z * gridData.cellSize) + gridData.worldOffset;
                Gizmos.DrawLine(start, end);
            }
            
            // Dikey çizgiler
            for (int x = 0; x <= gridData.width; x++)
            {
                Vector3 start = transform.position + new Vector3(x * gridData.cellSize, 0, 0) + gridData.worldOffset;
                Vector3 end = transform.position + new Vector3(x * gridData.cellSize, 0, gridData.height * gridData.cellSize) + gridData.worldOffset;
                Gizmos.DrawLine(start, end);
            }
        }
#endif
    }
}