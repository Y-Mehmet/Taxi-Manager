using UnityEngine;

// Bu script, LevelSpawnSO'dan veriyi okur ve ilgili yÃ¶neticilere daÄŸÄ±tarak level'Ä± kurar.
public class LevelSpawner : MonoBehaviour
{
    [Header("Level Data")]
    public LevelSpawnSO levelToSpawn;

    [Header("Scene References")]
    public GridManager gridManager;
    public PassengerSpawnManager passengerSpawnManager;
    public UnderpassManager underpassManager;
    public WagonManager wagonManager;
    public ConveyorManager conveyorManager;

    [Header("Prefabs")]
    public PassengerGroup passengerGroupPrefab;
    public UnderpassController underpassControllerPrefab;
    public MetroWagon metroWagonPrefab;
    public ConveyorBelt conveyorBeltPrefab; // VagonlarÄ±n takip edeceÄŸi yol. Projende bÃ¶yle bir bileÅŸen olduÄŸunu varsayÄ±yorum.

    void Awake()
    {
        // --- LEVEL LOADING ---
        int currentLevel = 1; // Default to level 1
        if (ResourceManager.Instance != null)
        {
            // ResourceManager.CurrentLevel is 0-based (0, 1, 2, 3...)
            // But level files are named Level_1, Level_2, Level_3...
            // So we add 1 to convert from 0-based to 1-based
            currentLevel = ResourceManager.Instance.CurrentLevel + 1;
            Debug.Log($"[LevelSpawner] ResourceManager.CurrentLevel = {ResourceManager.Instance.CurrentLevel}, Loading Level_{currentLevel}");
        }
        else
        {
            Debug.LogError("ResourceManager instance not found!");
            // Hata durumunda varsayÄ±lan olarak 1. level'Ä± yÃ¼klÃ¼yoruz
            currentLevel = 1;
        }

        // Level dosyasÄ±: Resources/Levels/Level_1, Level_2, Level_3...
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
        
        Debug.Log($"[LevelSpawner] Successfully loaded: {levelPath}");
        // --- END LEVEL LOADING ---
    }

    void Start()
    {
        // Ä°lgili yÃ¶neticileri SO'dan gelen veriyle baÅŸlat.
        gridManager.Initialize(levelToSpawn.gridData);
        passengerSpawnManager.Initialize(levelToSpawn.initialPassengerGroups, passengerGroupPrefab, gridManager);
        underpassManager.Initialize(levelToSpawn.underpasses, underpassControllerPrefab, passengerGroupPrefab, gridManager);
        wagonManager.Initialize(levelToSpawn.wagons, metroWagonPrefab);

        // Conditionally spawn conveyor belt and its passengers
        if (levelToSpawn.conveyorPassengers != null && levelToSpawn.conveyorPassengers.Count > 0)
        {
            if (conveyorBeltPrefab != null)
            {
                Instantiate(conveyorBeltPrefab, new Vector3(1.99798131f, 0.01f, -9.10000038f), Quaternion.identity);
                StartCoroutine(conveyorManager.Initialize(levelToSpawn.conveyorPassengers, passengerGroupPrefab));
            }
            else
            {
                Debug.LogError("Conveyor passengers are defined in LevelSpawnSO, but ConveyorBelt prefab is not assigned in LevelSpawner!");
            }
        }

        Debug.Log($"'{levelToSpawn.name}' iÃ§in spawn sÃ¼reci baÅŸladÄ±.");
    }
}
