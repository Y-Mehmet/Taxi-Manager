using UnityEngine;

// Bu script, LevelSpawnSO'dan veriyi okur ve ilgili yöneticilere dağıtarak level'ı kurar.
public class LevelSpawner : MonoBehaviour
{
    [Header("Level Data")]
    public LevelSpawnSO levelToSpawn;

    [Header("Scene References")]
    public GridManager gridManager;
    public PassengerSpawnManager passengerSpawnManager;
    public UnderpassManager underpassManager;
    public WagonManager wagonManager;

    [Header("Prefabs")]
    public PassengerGroup passengerGroupPrefab;
    public UnderpassController underpassControllerPrefab;
    public MetroWagon metroWagonPrefab;
    // public PathCreator wagonPath; // Vagonların takip edeceği yol. Projende böyle bir bileşen olduğunu varsayıyorum.

    void Awake()
    {
        // --- LEVEL LOADING ---
        int currentLevel = 0;
        if (ResourceManager.Instance != null)
        {
            // ResourceManager'dan mevcut level'ı alıyoruz. Level'lar 1'den başladığı için 1 ekliyoruz.
            currentLevel = ResourceManager.Instance.CurrentLevel + 1;
        }
        else
        {
            Debug.LogError("ResourceManager instance not found!");
            // Hata durumunda varsayılan olarak 1. level'ı yüklüyoruz.
            currentLevel = 1;
        }

        string levelPath = "Levels/Level_" + currentLevel;
        levelToSpawn = Resources.Load<LevelSpawnSO>(levelPath);
        
        if (levelToSpawn == null)
        {
            Debug.LogError($"Level asset not found at path: {levelPath}. Trying to load Level_1 as a fallback.");
            levelPath = "Levels/Level_1";
            levelToSpawn = Resources.Load<LevelSpawnSO>(levelPath);
            if (levelToSpawn == null)
            {
                Debug.LogError($"Fallback level asset not found at path: {levelPath}. Make sure the level asset exists in the Resources folder.");
                return;
            }
        }
        // --- END LEVEL LOADING ---

        // --- GEMINI-DEBUG: Log SO colors ---
        Debug.LogWarning("--- Logging Underpass Colors from LevelSpawnSO ---");
        for (int i = 0; i < levelToSpawn.underpasses.Count; i++)
        {
            var underpassData = levelToSpawn.underpasses[i];
            if (underpassData.passengerSequence != null)
            {
                string colors = string.Join(", ", underpassData.passengerSequence);
                Debug.LogWarning($"SO Underpass [{i}] at pos {underpassData.position} has sequence: [{colors}]");
            }
            else
            {
                Debug.LogWarning($"SO Underpass [{i}] at pos {underpassData.position} has a NULL sequence or color list.");
            }
        }
        Debug.LogWarning("--- End of LevelSpawnSO Log ---");
        // --- END GEMINI-DEBUG ---

        // İlgili yöneticileri SO'dan gelen veriyle başlat.
        // Her bir yöneticiyi Aşama 3'te güncelledikçe bu satırların yorumunu kaldıracağız.

        gridManager.Initialize(levelToSpawn.gridData);
        passengerSpawnManager.Initialize(levelToSpawn.initialPassengerGroups, passengerGroupPrefab, gridManager);
        underpassManager.Initialize(levelToSpawn.underpasses, underpassControllerPrefab, passengerGroupPrefab, gridManager);
        wagonManager.Initialize(levelToSpawn.wagons, metroWagonPrefab);

        Debug.Log($"'{levelToSpawn.name}' için spawn süreci başladı.");
    }
}
