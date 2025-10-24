using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using GridSystem.Data;

public class GridManager : MonoBehaviour
{
    public GridData gridData;
    public GridVisualizer gridVisualizer;

    private bool needsRebuild = false;

    public void Initialize(GridData data)
    {
        if (data == null || gridVisualizer == null)
        {
            Debug.LogError("Initialize için GridData veya GridVisualizer atanmamış!");
            return;
        }
        this.gridData = data;
        gridVisualizer.Initialize(this.gridData);
        RebuildGrid();
    }

    public void OnValidate()
    {
        if (gridData == null) return;

        if (gridData.cells == null || gridData.cells.Count != gridData.gridWidth * gridData.gridHeight)
        {
            needsRebuild = true;
        }

        if (Application.isPlaying && needsRebuild)
        {
            RebuildGrid();
        }
        else if (Application.isPlaying && gridVisualizer != null)
        {
            gridVisualizer.UpdateGridVisuals();
        }
    }

    private void RebuildGrid()
    {
        StartCoroutine(RebuildGridDelayed());
    }

    private IEnumerator RebuildGridDelayed()
    {
        // Eski görselleri temizle
        foreach (Transform child in gridVisualizer.transform)
        {
            Destroy(child.gameObject);
        }
        
        yield return null; // Bir frame bekle

        // Eğer cells listesi boşsa yeni hücreler WaitingArea olacak şekilde oluştur
        List<GridCell> newCells = new List<GridCell>();
        for (int y = 0; y < gridData.height; y++)
        {
            for (int x = 0; x < gridData.width; x++)
            {
                int index = y * gridData.width + x;
                if (gridData.cells != null && index < gridData.cells.Count)
                {
                    // Mevcut hücreyi koru (pozisyonu güncelle)
                    var existing = gridData.cells[index];
                    existing.position = new Vector2Int(x, y);
                    newCells.Add(existing);
                }
                else
                {
                    // Yeni hücre: kenar hücreleri Walkable, iç hücreleri WaitingArea yap
                    bool isBorder = (x == 0 || y == 0 || x == gridData.width - 1 || y == gridData.height - 1);
                    var defaultType = isBorder ? GridCellType.Walkable : GridCellType.WaitingArea;
                    newCells.Add(new GridCell(new Vector2Int(x, y), defaultType));
                }
            }
        }
        gridData.cells = newCells;

        // Görselleri oluştur
        foreach (var cell in gridData.cells)
        {
            gridVisualizer.CreateCellVisual(cell);
        }
        needsRebuild = false;
    }

    public Vector3 GetWorldPosition(Vector2Int gridPosition)
    {
        return new Vector3(gridPosition.x * gridData.cellSize, 0, gridPosition.y * gridData.cellSize) + gridData.gridWorldOffset;
    }

    [ContextMenu("Yolcu Gruplarını Otomatik Spawn Et")]
    public void AutoSpawnPassengerGroups()
    {
        if (gridData.passengerGroupPrefab == null)
        {
            Debug.LogWarning("PassengerGroup prefabı atanmadı!");
            return;
        }
        foreach (var pos in gridData.autoSpawnPassengerPositions)
        {
            var cell = gridData.GetCell(pos.x, pos.y);
            if (cell != null)
            {
                // Eğer o konumda zaten manuel bir PassengerGroup varsa spawn etme
                bool occupied = false;
                var existing = FindObjectsByType<PassengerGroup>(FindObjectsSortMode.None);
                foreach (var g in existing)
                {
                    if (g != null && g.gridPos == pos)
                    {
                        occupied = true;
                        break;
                    }
                }
                if (occupied) continue;

                Vector3 worldPos = cell.cellTransform != null ? cell.cellTransform.position : GetWorldPosition(pos);
                GameObject group = Instantiate(gridData.passengerGroupPrefab, worldPos, Quaternion.identity, this.transform);
                var groupScript = group.GetComponent<PassengerGroup>();
                if (groupScript != null)
                {
                    // Initialize spawned group's gridPos and position
                    groupScript.gridPos = pos;
                    groupScript.useGridPosition = true;
                    group.transform.position = worldPos;
                }
            }
        }
    }
}
