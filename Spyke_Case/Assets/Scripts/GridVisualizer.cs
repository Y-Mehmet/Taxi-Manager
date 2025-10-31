using UnityEngine;
using System.Collections.Generic;
using GridSystem.Data;

public class GridVisualizer : MonoBehaviour
{
    [Header("Görsel Ayarlar")]
    public GameObject cellCubePrefab;
    public bool showGridLines = true;
    public Color gridLineColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);

    private GridData gridData;
    private Material[] cellTypeMaterials;
    private bool materialsInitialized = false;

    public void Initialize(GridData data)
    {
        gridData = data;
        InitializeMaterials();
    }

    private void InitializeMaterials()
    {
        if (materialsInitialized) return;

        cellTypeMaterials = new Material[System.Enum.GetValues(typeof(GridCellType)).Length];
        string[] materialNames = { "GridEmpty", "GridBlocked", "GridWalkable", "GridWaitingArea", "GridStop" };

        for (int i = 0; i < materialNames.Length; i++)
        {
            Material mat = Resources.Load<Material>($"Materials/Grid/{materialNames[i]}");
            if (mat != null)
            {
                cellTypeMaterials[i] = new Material(mat);
            }
            else
            {
                mat = new Material(Shader.Find("Standard"));
                mat.name = materialNames[i];
                switch ((GridCellType)i)
                {
                    case GridCellType.Empty: mat.color = Color.black; break;
                    case GridCellType.Blocked: mat.color = Color.red; break;
                    case GridCellType.Walkable: mat.color = Color.white; break;
                    case GridCellType.WaitingArea: mat.color = Color.clear; break;
                    case GridCellType.Stop: mat.color = Color.blue; break;
                }
                cellTypeMaterials[i] = mat;
            }
        }
        materialsInitialized = true;
    }

    public void CreateCellVisual(GridCell cell)
    {
        if (cellCubePrefab == null) return;

        GameObject cellObj = new GameObject($"GridCell_{cell.position.x}_{cell.position.y}");
        cellObj.transform.parent = transform;
        cellObj.transform.position = new Vector3(cell.position.x * gridData.cellSize, 0, cell.position.y * gridData.cellSize) + gridData.worldOffset;
        cell.cellTransform = cellObj.transform;

        cell.cellCube = Instantiate(cellCubePrefab, cellObj.transform.position, Quaternion.identity, cellObj.transform);
        cell.cellCube.name = $"CellCube_{cell.position.x}_{cell.position.y}";
        UpdateCellCubeColor(cell);
    }

    public void UpdateCellCubeColor(GridCell cell)
    {
        if (cell == null || cell.cellCube == null) return;
        var renderer = cell.cellCube.GetComponent<Renderer>();
        if (renderer == null) return;

        if (!materialsInitialized) InitializeMaterials();

        int typeIndex = (int)cell.cellType;
        if (typeIndex >= 0 && typeIndex < cellTypeMaterials.Length && cellTypeMaterials[typeIndex] != null)
        {
            renderer.sharedMaterial = new Material(cellTypeMaterials[typeIndex]);
        }
    }

    public void UpdateGridVisuals()
    {
        if (gridData == null || gridData.cells == null) return;

        foreach (var cell in gridData.cells)
        {
            if (cell == null) continue;
            if (cell.cellTransform != null)
            {
                cell.cellTransform.position = new Vector3(cell.position.x * gridData.cellSize, 0, cell.position.y * gridData.cellSize) + gridData.worldOffset;
            }
            if (cell.cellCube != null)
            {
                cell.cellCube.transform.position = cell.cellTransform.position;
                UpdateCellCubeColor(cell);
            }
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!showGridLines || gridData == null) return;

        Gizmos.color = gridLineColor;
        for (int z = 0; z <= gridData.height; z++)
        {
            Vector3 start = transform.position + new Vector3(0, 0, z * gridData.cellSize) + gridData.worldOffset;
            Vector3 end = transform.position + new Vector3(gridData.width * gridData.cellSize, 0, z * gridData.cellSize) + gridData.worldOffset;
            Gizmos.DrawLine(start, end);
        }
        for (int x = 0; x <= gridData.width; x++)
        {
            Vector3 start = transform.position + new Vector3(x * gridData.cellSize, 0, 0) + gridData.worldOffset;
            Vector3 end = transform.position + new Vector3(x * gridData.cellSize, 0, gridData.height * gridData.cellSize) + gridData.worldOffset;
            Gizmos.DrawLine(start, end);
        }
    }
#endif
}
