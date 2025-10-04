using UnityEngine;
using UnityEditor;
using GridSystem;

[CustomEditor(typeof(PassengerGrid))]
public class PassengerGridEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        PassengerGrid grid = (PassengerGrid)target;
        if (GUILayout.Button("Gridi Yenile (OnValidate)") )
        {
            grid.OnValidate();
            EditorUtility.SetDirty(grid);
        }
    }

    void OnSceneGUI()
    {
        PassengerGrid grid = (PassengerGrid)target;
        if (grid == null) return;
        Handles.color = Color.gray;
        for (int y = 0; y < grid.gridHeight; y++)
        {
            for (int x = 0; x < grid.gridWidth; x++)
            {
                Vector3 pos = new Vector3(x * grid.cellSize, 0, y * grid.cellSize);
                Handles.DrawWireCube(pos, Vector3.one * grid.cellSize * 0.95f);
            }
        }
    }
}
