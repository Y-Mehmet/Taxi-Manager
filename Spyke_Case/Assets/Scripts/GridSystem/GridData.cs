using UnityEngine;
using System.Collections.Generic;

namespace GridSystem.Data
{
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
        public GridCellType cellType;
        public Vector2Int position;
        // Visual helpers used by GridVisualizer and legacy code
        [System.NonSerialized]
        public Transform cellTransform;
        [System.NonSerialized]
        public GameObject cellCube;
        // Backwards-compatible alias used in older scripts
        public Vector2Int gridPos { get { return position; } set { position = value; } }
        public int stopIndex = -1;

        // Parameterless constructor for object-initializer usages in legacy code
        public GridCell()
        {
            position = Vector2Int.zero;
            cellType = GridCellType.WaitingArea; // default to WaitingArea as requested
        }

        public GridCell(Vector2Int pos, GridCellType type = GridCellType.Empty)
        {
            position = pos;
            cellType = type;
        }
    }

    [CreateAssetMenu(fileName = "New GridData", menuName = "Grid System/Grid Data")]
    public class GridData : ScriptableObject
    {
        [Header("Grid Boyutu")]
        public int width = 10;
        public int height = 10;
        public float cellSize = 1.0f;

        // Backwards-compatible properties for older scripts that expect these names
        public int gridWidth { get { return width; } set { width = value; } }
        public int gridHeight { get { return height; } set { height = value; } }

        [Header("Grid Hücreleri")]
        public List<GridCell> cells = new List<GridCell>();

        [Header("Grid Offset")]
        public Vector3 worldOffset = Vector3.zero;

    // Backwards-compatible name
    public Vector3 gridWorldOffset { get { return worldOffset; } set { worldOffset = value; } }

        [Header("Yolcu Spawn Ayarları")]
        public GameObject passengerGroupPrefab;
        public List<Vector2Int> autoSpawnPassengerPositions = new List<Vector2Int>();

        [Header("Slotlar")]
        public List<Vector2Int> waitingAreaSlots = new List<Vector2Int>();
        public List<Vector2Int> stopSlots = new List<Vector2Int>();

        // Exposed so editor scripts can trigger validation programmatically
        public void OnValidate()
        {
            // Grid boyutu değiştiğinde hücreleri güncelle
            if (cells.Count != width * height)
            {
                ResizeGrid();
            }
        }

        private void ResizeGrid()
        {
            List<GridCell> newCells = new List<GridCell>();
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Mevcut hücreyi koru veya yeni oluştur
                    int index = y * width + x;
                    if (index < cells.Count)
                    {
                        var cell = cells[index];
                        cell.position = new Vector2Int(x, y);
                        newCells.Add(cell);
                    }
                    else
                    {
                        // Set border cells to Walkable, inner cells to WaitingArea
                        bool isBorder = (x == 0 || y == 0 || x == width - 1 || y == height - 1);
                        var defaultType = isBorder ? GridCellType.Walkable : GridCellType.WaitingArea;
                        newCells.Add(new GridCell(new Vector2Int(x, y), defaultType));
                    }
                }
            }
            
            cells = newCells;
        }

        public GridCell GetCell(int x, int y)
        {
            if (x < 0 || y < 0 || x >= width || y >= height || cells == null || cells.Count == 0)
            {
                return null;
            }
            int index = y * width + x;
            if (index >= 0 && index < cells.Count)
            {
                return cells[index];
            }
            return null;
        }
    }
}