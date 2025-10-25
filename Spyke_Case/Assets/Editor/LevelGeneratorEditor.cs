
using UnityEditor;
using UnityEngine;
using System.IO;

public class LevelGeneratorEditor : EditorWindow
{
    private int levelNumber = 1;
    private bool overrideNumUnderpasses = false;
    private int manualNumUnderpasses = 0;
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

        // Manual override for underpasses
        overrideNumUnderpasses = EditorGUILayout.Toggle("Override Underpasses", overrideNumUnderpasses);
        GUI.enabled = overrideNumUnderpasses;
        manualNumUnderpasses = EditorGUILayout.IntField("Number of Underpasses", manualNumUnderpasses);
        GUI.enabled = true;

        if (GUILayout.Button("Generate and Preview JSON"))
        {
            if (levelNumber < 1)
            {
                EditorUtility.DisplayDialog("Error", "Level number must be 1 or greater.", "OK");
                return;
            }

            // Use the override if it's toggled, otherwise pass null.
            int? underpassOverride = overrideNumUnderpasses ? (int?)manualNumUnderpasses : null;

            // Generate the level definition
            LevelDefinition levelDef = LevelGenerator.GenerateLevel(levelNumber, underpassOverride);

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
        string path = EditorUtility.SaveFilePanelInProject(
            "Save Level Asset",
            $"Level_{levelDef.levelNumber}.asset",
            "asset",
            "Please enter a file name to save the level to."
        );

        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        // Check if an asset already exists at the path.
        LevelSpawnSO existingLevelSO = AssetDatabase.LoadAssetAtPath<LevelSpawnSO>(path);

        if (existingLevelSO != null)
        {
            // If it exists, update it.
            existingLevelSO.initialPassengerGroups = levelDef.initialPassengerGroups;
            existingLevelSO.underpasses = levelDef.underpasses;
            existingLevelSO.wagons = levelDef.wagons;
            EditorUtility.SetDirty(existingLevelSO);
            Debug.Log($"Updated existing LevelSpawnSO at: {path}", this);
        }
        else
        {
            // If it doesn't exist, create a new one.
            LevelSpawnSO newLevelSO = CreateInstance<LevelSpawnSO>();
            newLevelSO.initialPassengerGroups = levelDef.initialPassengerGroups;
            newLevelSO.underpasses = levelDef.underpasses;
            newLevelSO.wagons = levelDef.wagons;

            AssetDatabase.CreateAsset(newLevelSO, path);
            Debug.Log($"Created new LevelSpawnSO at: {path}", this);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<LevelSpawnSO>(path);
    }
}
