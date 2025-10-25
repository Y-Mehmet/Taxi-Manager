using UnityEngine;
using UnityEditor;

public class LevelGeneratorEditor : EditorWindow
{
    private int levelNumber = 1;
    private string generatedJson = "";
    private Vector2 scrollPosition;

    [MenuItem("Tools/Level Generator")]
    public static void ShowWindow()
    {
        GetWindow<LevelGeneratorEditor>("Level Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Level Generation", EditorStyles.boldLabel);

        levelNumber = EditorGUILayout.IntField("Level Number", levelNumber);

        if (GUILayout.Button("Generate and Preview JSON"))
        {
            // Call the generator from Phase 1, Task 1.2
            LevelDefinition levelDef = LevelGenerator.GenerateLevel(levelNumber);

            // Serialize the result to JSON for preview
            generatedJson = JsonUtility.ToJson(levelDef, true);
            
            Debug.Log($"Generated Level {levelNumber} Data:\n{generatedJson}");
        }

        EditorGUILayout.Space();

        GUILayout.Label("Generated JSON Preview", EditorStyles.boldLabel);

        // Display the JSON in a scrollable text area
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));
        EditorGUILayout.LongField(generatedJson, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();

        // We will add the "Save as LevelSpawnSO" button in Phase 4
    }
}