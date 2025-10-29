using UnityEditor;
using UnityEngine;
using System.IO;

public class LevelGeneratorEditor : EditorWindow
{
    // Single Generation
    private int levelNumber = 1;
    
    // Batch Generation
    private int batchStartLevel = 1;
    private int batchEndLevel = 100;

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
        // --- SINGLE LEVEL GENERATION ---
        EditorGUILayout.LabelField("Single Level Generation", EditorStyles.boldLabel);
        levelNumber = EditorGUILayout.IntField("Level Number", levelNumber);
        
        EditorGUILayout.Space();
        GUILayout.Label("Overrides (for Single Level)", EditorStyles.boldLabel);

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

        if (GUILayout.Button("Generate & Save Single Level"))
        {
            GenerateSingleLevel();
        }

        // --- BATCH GENERATION ---
        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("Batch Level Generation", EditorStyles.boldLabel);
        batchStartLevel = EditorGUILayout.IntField("Start Level", batchStartLevel);
        batchEndLevel = EditorGUILayout.IntField("End Level", batchEndLevel);

        if (GUILayout.Button("Generate Level Range"))
        {
            GenerateBatchLevels();
        }
    }

    private void GenerateSingleLevel()
    {
        if (levelNumber < 1)
        {
            EditorUtility.DisplayDialog("Error", "Level number must be 1 or greater.", "OK");
            return;
        }

        int? underpassOverride = overrideNumUnderpasses ? (int?)manualNumUnderpasses : null;
        int? conveyorOverride = overrideNumConveyor ? (int?)manualNumConveyor : null;
        int? passengerOverride = overrideNumPassengers ? (int?)manualNumPassengers : null;
        int? colorOverride = overrideNumColors ? (int?)manualNumColors : null;

        LevelDefinition levelDef = LevelGenerator.GenerateLevel(levelNumber, underpassOverride, conveyorOverride, passengerOverride, colorOverride);
        SaveLevelSpawnSO(levelDef);
    }

    private void GenerateBatchLevels()
    {
        if (batchStartLevel < 1 || batchEndLevel < batchStartLevel)
        {
            EditorUtility.DisplayDialog("Error", "Invalid level range. Start level must be 1 or greater and less than or equal to end level.", "OK");
            return;
        }

        try
        {
            AssetDatabase.StartAssetEditing();
            int totalLevels = (batchEndLevel - batchStartLevel) + 1;
            for (int i = 0; i < totalLevels; i++)
            {
                int currentLevel = batchStartLevel + i;
                EditorUtility.DisplayProgressBar(
                    "Generating Levels", 
                    $"Processing Level {currentLevel}...", 
                    (float)i / totalLevels
                );
                // For batch generation, we don't use overrides.
                LevelDefinition levelDef = LevelGenerator.GenerateLevel(currentLevel, null, null, null, null);
                SaveLevelSpawnSO(levelDef);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Success", $"Successfully generated and saved levels from {batchStartLevel} to {batchEndLevel}.", "OK");
        }
    }

    private void SaveLevelSpawnSO(LevelDefinition levelDef)
    {
        string directoryPath = "Assets/Resources/Levels";
        string fileName = $"Level_{levelDef.levelNumber}.asset";
        string path = Path.Combine(directoryPath, fileName);

        Directory.CreateDirectory(directoryPath);

        LevelSpawnSO existingLevelSO = AssetDatabase.LoadAssetAtPath<LevelSpawnSO>(path);

        if (existingLevelSO != null)
        {
            existingLevelSO.initialPassengerGroups = levelDef.initialPassengerGroups;
            existingLevelSO.underpasses = levelDef.underpasses;
            existingLevelSO.wagons = levelDef.wagons;
            existingLevelSO.conveyorPassengers = levelDef.conveyorPassengers;
            EditorUtility.SetDirty(existingLevelSO);
        }
        else
        {
            LevelSpawnSO newLevelSO = CreateInstance<LevelSpawnSO>();
            newLevelSO.initialPassengerGroups = levelDef.initialPassengerGroups;
            newLevelSO.underpasses = levelDef.underpasses;
            newLevelSO.wagons = levelDef.wagons;
            newLevelSO.conveyorPassengers = levelDef.conveyorPassengers;
            AssetDatabase.CreateAsset(newLevelSO, path);
        }
    }
}