using UnityEngine;
using System.Collections.Generic;

public enum GridCellType
{
    Empty,
    Blocked,
    Walkable,
    WaitingArea,
    Stop
}

[System.Serializable]
public class GridCell
{
    public GridCellType cellType = GridCellType.Empty;
    public Vector2Int gridPos;
    public Transform cellTransform; // Hücreye karşılık gelen obje (görsel için)
}

public class PassengerGrid : MonoBehaviour
{
    [Header("Grid offseti (dünya pozisyonu)")]
    public Vector3 gridWorldOffset = Vector3.zero;

    [Header("Otomatik yolcu grubu spawn listesi (grid pozisyonları)")]
    public List<Vector2Int> autoSpawnPassengerPositions = new List<Vector2Int>();

    [Header("Yolcu grubu prefabı (otomatik spawn için)")]
    public GameObject passengerGroupPrefab;

    // Inspector'dan çağrılabilir: Griddeki tüm autoSpawnPassengerPositions için yolcu grubu spawn et
    [ContextMenu("Yolcu Gruplarını Otomatik Spawn Et")]
    public void AutoSpawnPassengerGroups()
    {
        if (passengerGroupPrefab == null)
        {
            Debug.LogWarning("PassengerGroup prefabı atanmadı!");
            return;
        }
        foreach (var pos in autoSpawnPassengerPositions)
        {
            var cell = GetCell(pos.x, pos.y);
            if (cell != null)
            {
                Vector3 worldPos = cell.cellTransform != null ? cell.cellTransform.position : new Vector3(pos.x * cellSize, 0, pos.y * cellSize);
                worldPos += gridWorldOffset;
                GameObject group = Instantiate(passengerGroupPrefab, worldPos, Quaternion.identity, this.transform);
                var groupScript = group.GetComponent<PassengerGroup>();
                if (groupScript != null)
                {
                    groupScript.grid = this;
                    groupScript.gridPos = pos;
                }
            }
        }
    }
    [Header("Bekleme Alanı Slotları (grid pozisyonları)")]
    public List<Vector2Int> waitingAreaSlots = new List<Vector2Int>();

    [Header("Durak Slotları (grid pozisyonları)")]
    public List<Vector2Int> stopSlots = new List<Vector2Int>();
    [Header("Grid Boyutu (X: sütun, Y: satır)")]
    public int gridWidth = 10;
    public int gridHeight = 10;
    [Header("Grid Hücreleri")] 
    public List<GridCell> cells = new List<GridCell>();

    public float cellSize = 1.0f;

    public void OnValidate()
    {
        // Grid boyutu değişirse otomatik güncelle
        if (cells.Count != gridWidth * gridHeight)
        {
            // Eski hücre objelerini sil
            foreach (var cell in cells)
            {
                if (cell.cellTransform != null)
                    DestroyImmediate(cell.cellTransform.gameObject);
            }
            cells.Clear();
            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    GridCell cell = new GridCell();
                    cell.gridPos = new Vector2Int(x, y);
                    cell.cellType = GridCellType.Walkable; // Varsayılan olarak Walkable
                    GameObject cellObj = new GameObject($"GridCell_{x}_{y}");
                    cellObj.transform.parent = this.transform;
                    cellObj.transform.position = new Vector3(x * cellSize, 0, y * cellSize) + gridWorldOffset;
                    cell.cellTransform = cellObj.transform;
                    cells.Add(cell);
                }
            }
        }
        else
        {
            // Sadece offset veya cellSize değiştiyse pozisyonları güncelle
            for (int i = 0; i < cells.Count; i++)
            {
                var cell = cells[i];
                int x = cell.gridPos.x;
                int y = cell.gridPos.y;
                if (cell.cellTransform != null)
                {
                    cell.cellTransform.position = new Vector3(x * cellSize, 0, y * cellSize) + gridWorldOffset;
                }
            }
        }
    }

    public GridCell GetCell(int x, int y)
    {
        if (x < 0 || y < 0 || x >= gridWidth || y >= gridHeight) return null;
        return cells[y * gridWidth + x];
    }
}
