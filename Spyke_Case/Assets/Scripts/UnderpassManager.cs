using UnityEngine;
using System.Collections.Generic;

// LevelSpawner'dan aldığı veriyle Underpass prefab'larını oluşturur ve yönetir.
public class UnderpassManager : MonoBehaviour
{
    public static UnderpassManager Instance { get; private set; }
    private GridManager gridManager;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private Dictionary<PassengerGroup, UnderpassController> groupToUnderpassMap = new Dictionary<PassengerGroup, UnderpassController>();
    private List<UnderpassController> activeUnderpasses = new List<UnderpassController>();

    public void Initialize(List<UnderpassSpawnData> spawnData, UnderpassController underpassPrefab, PassengerGroup passengerPrefab, GridManager gridManager)
    {
        this.gridManager = gridManager;

        if (underpassPrefab == null || passengerPrefab == null)
        {
            Debug.LogError("UnderpassManager'a gerekli prefablar atanmamış!");
            return;
        }

        foreach (var data in spawnData)
        {
            Vector3 spawnPos = this.gridManager.GetWorldPosition(data.position);
            UnderpassController newUnderpass = Instantiate(underpassPrefab, spawnPos, Quaternion.identity, transform);
            newUnderpass.name = $"Underpass_{data.position.x}_{data.position.y}";
            
            // Her bir alt geçit için SO'dan gelen yön bilgisini ata
            newUnderpass.startCellOffset = data.direction;

            // Controller'ı başlat, o da kendi yolcularını oluştursun
            newUnderpass.Initialize(this.gridManager, data.position, passengerPrefab, data.passengerSequence);
            activeUnderpasses.Add(newUnderpass);

            // Oluşturulan yolcuları, hangi alt geçide ait olduklarını bilmek için haritaya ekle
            foreach (var groupInQueue in newUnderpass.GetQueue())
            {
                groupToUnderpassMap[groupInQueue] = newUnderpass;
            }
        }
    }

    public bool AreAllQueuesEmpty()
    {
        foreach (var underpass in activeUnderpasses)
        {
            if (underpass.GetQueue().Count > 0)
            {
                return false; // Found an underpass with passengers
            }
        }
        return true; // All underpasses are empty
    }
}