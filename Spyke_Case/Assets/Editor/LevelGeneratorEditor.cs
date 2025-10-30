using UnityEditor;
using UnityEngine;
using System.IO;

public class LevelGeneratorEditor : EditorWindow
{
    private int levelNumber = 1;
    private int batchStartLevel = 1;
    private int batchEndLevel = 100;
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
        EditorGUILayout.LabelField("Single Level Generation", EditorStyles.boldLabel);
        levelNumber = EditorGUILayout.IntField("Level Number", levelNumber);
        EditorGUILayout.Space();
        GUILayout.Label("Overrides (for Single Level)", EditorStyles.boldLabel);
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
        if (levelNumber < 1) { EditorUtility.DisplayDialog("Error", "Level number must be 1 or greater.", "OK"); return; }
        int? underpassOverride = overrideNumUnderpasses ? (int?)manualNumUnderpasses : null;
        int? conveyorOverride = overrideNumConveyor ? (int?)manualNumConveyor : null;
        int? passengerOverride = overrideNumPassengers ? (int?)manualNumPassengers : null;
        int? colorOverride = overrideNumColors ? (int?)manualNumColors : null;
        LevelDefinition levelDef = LevelGenerator.GenerateLevel(levelNumber, underpassOverride, conveyorOverride, passengerOverride, colorOverride);
        if (levelDef == null)
        {
            EditorUtility.DisplayDialog("Generation Failed", $"Failed to generate a valid layout for Level {levelNumber} after multiple attempts. Please try different parameters or a different level number.", "OK");
        }
        else
        {
            SaveLevelSpawnSO(levelDef);
            EditorUtility.DisplayDialog("Success", $"Successfully generated and saved Level {levelNumber}.", "OK");
        }
    }

    private void GenerateBatchLevels()
    {
        if (batchStartLevel < 1 || batchEndLevel < batchStartLevel) { EditorUtility.DisplayDialog("Error", "Invalid level range.", "OK"); return; }
        int successCount = 0;
        bool generationFailed = false;
        try
        {
            AssetDatabase.StartAssetEditing();
            int totalLevels = (batchEndLevel - batchStartLevel) + 1;
            for (int i = 0; i < totalLevels; i++)
            {
                int currentLevel = batchStartLevel + i;
                EditorUtility.DisplayProgressBar("Generating Levels", $"Processing Level {currentLevel}...", (float)i / totalLevels);
                LevelDefinition levelDef = LevelGenerator.GenerateLevel(currentLevel, null, null, null, null);
                if (levelDef == null)
                {
                    EditorUtility.DisplayDialog("Batch Generation Failed", $"Failed to generate a valid layout for Level {currentLevel} after multiple attempts. Stopping batch process. {successCount} levels were generated successfully.", "OK");
                    generationFailed = true;
                    break;
                }
                SaveLevelSpawnSO(levelDef);
                successCount++;
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!generationFailed)
            {
                EditorUtility.DisplayDialog("Success", $"Successfully generated and saved {successCount} levels from {batchStartLevel} to {batchEndLevel}.", "OK");
            }
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