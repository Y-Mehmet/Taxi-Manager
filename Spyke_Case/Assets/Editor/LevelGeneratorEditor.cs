
using UnityEditor;
using UnityEngine;
using System.IO;

public class LevelGeneratorEditor : EditorWindow
{
    private int levelNumber = 1;
    private string jsonPreview = "";
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
            if (levelNumber < 1)
            {
                EditorUtility.DisplayDialog("Error", "Level number must be 1 or greater.", "OK");
                return;
            }

            // Generate the level definition
            LevelDefinition levelDef = LevelGenerator.GenerateLevel(levelNumber);

            // Convert to JSON for preview
            jsonPreview = JsonUtility.ToJson(levelDef, true);
        }

        // Display the JSON preview
        EditorGUILayout.LabelField("JSON Preview:");
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));
        EditorGUILayout.TextArea(jsonPreview, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("Save as LevelSpawnSO"))
        {
            if (string.IsNullOrEmpty(jsonPreview))
            {
                EditorUtility.DisplayDialog("Error", "Please generate a level first before saving.", "OK");
                return;
            }

            // Logic to create and save the ScriptableObject
            SaveLevelSpawnSO(JsonUtility.FromJson<LevelDefinition>(jsonPreview));
        }
    }

    private void SaveLevelSpawnSO(LevelDefinition levelDef)
    {
        // Unfortunately, we can't directly create an instance of a ScriptableObject
        // of a type we don't have direct access to (LevelSpawnSO).
        // This method will need to be implemented within your project's scripts
        // where you have access to the LevelSpawnSO type.

        // The following is placeholder logic.
        // You would replace this with your actual saving code.

        string path = EditorUtility.SaveFilePanelInProject(
            "Save Level",
            $"Level{levelDef.levelNumber}.asset",
            "asset",
            "Please enter a file name to save the level to."
        );

        if (string.IsNullOrEmpty(path))
            return;

        // Example of how you might do it if you had a factory method:
        // LevelSpawnSO newLevelSO = LevelSpawnSO.CreateInstance();
        // newLevelSO.ApplyDefinition(levelDef); // A method you would write
        // AssetDatabase.CreateAsset(newLevelSO, path);

        // For now, we'll just save the JSON to a text file as a placeholder.
        string jsonPath = Path.ChangeExtension(path, ".json");
        File.WriteAllText(jsonPath, jsonPreview);


        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.FocusProjectWindow();
        // Selection.activeObject = newLevelSO; // This would select the new asset

        Debug.Log($"Level data saved as JSON to: {jsonPath}. You will need to implement the final conversion to LevelSpawnSO.", this);
    }
}
