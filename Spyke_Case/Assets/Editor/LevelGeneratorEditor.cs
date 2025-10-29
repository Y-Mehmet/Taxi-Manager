using UnityEditor;
using UnityEngine;
using System.IO;

public class LevelGeneratorEditor : EditorWindow
{
    private int levelNumber = 1;
    
    // Override fields
    private bool overrideNumUnderpasses = false;
    private int manualNumUnderpasses = 0;
    private bool overrideNumConveyor = false;
    private int manualNumConveyor = 0;
    private bool overrideNumPassengers = false;
    private int manualNumPassengers = 0;
    private bool overrideNumColors = false;
    private int manualNumColors = 3;

    [MenuItem("Tools/Level Generator")]
    public static void ShowWindow()
    {
        GetWindow<LevelGeneratorEditor>("Level Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Level Generation", EditorStyles.boldLabel);

        levelNumber = EditorGUILayout.IntField("Level Number", levelNumber);
        
        EditorGUILayout.Space();
        GUILayout.Label("Overrides", EditorStyles.boldLabel);

        // Override UI
        overrideNumUnderpasses = EditorGUILayout.Toggle("Override Underpasses", overrideNumUnderpasses);
        GUI.enabled = overrideNumUnderpasses;
        manualNumUnderpasses = EditorGUILayout.IntField("Number of Underpasses", manualNumUnderpasses);
        GUI.enabled = true;

        overrideNumConveyor = EditorGUILayout.Toggle("Override Conveyor Passengers", overrideNumConveyor);
        GUI.enabled = overrideNumConveyor;
        manualNumConveyor = EditorGUILayout.IntField("Number of Conveyor Passengers", manualNumConveyor);
        GUI.enabled = true;

        overrideNumPassengers = EditorGUILayout.Toggle("Override Start Passengers", overrideNumPassengers);
        GUI.enabled = overrideNumPassengers;
        manualNumPassengers = EditorGUILayout.IntField("Number of Start Passengers", manualNumPassengers);
        GUI.enabled = true;

        overrideNumColors = EditorGUILayout.Toggle("Override Color Count", overrideNumColors);
        GUI.enabled = overrideNumColors;
        manualNumColors = EditorGUILayout.IntField("Number of Colors", manualNumColors);
        GUI.enabled = true;

        EditorGUILayout.Space();

        // Combined Generate and Save Button
        if (GUILayout.Button("Generate & Save Level"))
        {
            if (levelNumber < 1)
            {
                EditorUtility.DisplayDialog("Error", "Level number must be 1 or greater.", "OK");
                return;
            }

            // Set override values to null if not toggled
            int? underpassOverride = overrideNumUnderpasses ? (int?)manualNumUnderpasses : null;
            int? conveyorOverride = overrideNumConveyor ? (int?)manualNumConveyor : null;
            int? passengerOverride = overrideNumPassengers ? (int?)manualNumPassengers : null;
            int? colorOverride = overrideNumColors ? (int?)manualNumColors : null;

            // 1. Generate the level definition
            LevelDefinition levelDef = LevelGenerator.GenerateLevel(levelNumber, underpassOverride, conveyorOverride, passengerOverride, colorOverride);

            // 2. Automatically save the generated definition
            SaveLevelSpawnSO(levelDef);
        }
    }

    private void SaveLevelSpawnSO(LevelDefinition levelDef)
    {
        string directoryPath = "Assets/Resources/Levels";
        string fileName = $"Level_{levelDef.levelNumber}.asset";
        string path = Path.Combine(directoryPath, fileName);

        // Ensure the directory exists
        Directory.CreateDirectory(directoryPath);

        LevelSpawnSO existingLevelSO = AssetDatabase.LoadAssetAtPath<LevelSpawnSO>(path);

        if (existingLevelSO != null)
        {
            // Update existing asset
            existingLevelSO.initialPassengerGroups = levelDef.initialPassengerGroups;
            existingLevelSO.underpasses = levelDef.underpasses;
            existingLevelSO.wagons = levelDef.wagons;
            existingLevelSO.conveyorPassengers = levelDef.conveyorPassengers;
            EditorUtility.SetDirty(existingLevelSO);
            Debug.Log($"Updated existing LevelSpawnSO at: {path}", this);
        }
        else
        {
            // Create new asset
            LevelSpawnSO newLevelSO = CreateInstance<LevelSpawnSO>();
            newLevelSO.initialPassengerGroups = levelDef.initialPassengerGroups;
            newLevelSO.underpasses = levelDef.underpasses;
            newLevelSO.wagons = levelDef.wagons;
            newLevelSO.conveyorPassengers = levelDef.conveyorPassengers;

            AssetDatabase.CreateAsset(newLevelSO, path);
            Debug.Log($"Created new LevelSpawnSO at: {path}", this);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<LevelSpawnSO>(path);
    }
}
