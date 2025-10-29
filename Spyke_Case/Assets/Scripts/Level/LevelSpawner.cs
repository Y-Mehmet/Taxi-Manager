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
    public ConveyorManager conveyorManager;

    [Header("Prefabs")]
    public PassengerGroup passengerGroupPrefab;
    public UnderpassController underpassControllerPrefab;
    public MetroWagon metroWagonPrefab;
    public ConveyorBelt conveyorBeltPrefab; // Vagonların takip edeceği yol. Projende böyle bir bileşen olduğunu varsayıyorum.

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
    }

    void Start()
    {
        // İlgili yöneticileri SO'dan gelen veriyle başlat.
        gridManager.Initialize(levelToSpawn.gridData);
        passengerSpawnManager.Initialize(levelToSpawn.initialPassengerGroups, passengerGroupPrefab, gridManager);
        underpassManager.Initialize(levelToSpawn.underpasses, underpassControllerPrefab, passengerGroupPrefab, gridManager);
        wagonManager.Initialize(levelToSpawn.wagons, metroWagonPrefab);

        // Conditionally spawn conveyor belt and its passengers
        if (levelToSpawn.conveyorPassengers != null && levelToSpawn.conveyorPassengers.Count > 0)
        {
            if (conveyorBeltPrefab != null)
            {
                Instantiate(conveyorBeltPrefab, new Vector3(1.99798131f,0.547583222f,-9.10000038f), Quaternion.identity);
                StartCoroutine(conveyorManager.Initialize(levelToSpawn.conveyorPassengers, passengerGroupPrefab));
            }
            else
            {
                Debug.LogError("Conveyor passengers are defined in LevelSpawnSO, but ConveyorBelt prefab is not assigned in LevelSpawner!");
            }
        }

        Debug.Log($"'{levelToSpawn.name}' için spawn süreci başladı.");
    }
}
