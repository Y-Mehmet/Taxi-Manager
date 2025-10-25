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

    void Start()
    {
        if (levelToSpawn == null)
        {
            Debug.LogError("LevelSpawner'a spawn edilecek bir LevelSpawnSO atanmamış!");
            return;
        }

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