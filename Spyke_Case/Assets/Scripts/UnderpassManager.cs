using UnityEngine;
using System.Collections.Generic;

// LevelSpawner'dan aldÄ±ÄŸÄ± veriyle Underpass prefab'larÄ±nÄ± oluÅŸturur ve yÃ¶netir.
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
            Debug.LogError("UnderpassManager'a gerekli prefablar atanmamÄ±ÅŸ!");
            return;
        }

        foreach (var data in spawnData)
        {
            Vector3 spawnPos = this.gridManager.GetWorldPosition(data.position);
            UnderpassController newUnderpass = Instantiate(underpassPrefab, spawnPos, Quaternion.identity, transform);
            newUnderpass.name = $"Underpass_{data.position.x}_{data.position.y}";
            
            // Her bir alt geÃ§it iÃ§in SO'dan gelen yÃ¶n bilgisini ata
            newUnderpass.startCellOffset = data.direction;

            // Controller'Ä± baÅŸlat, o da kendi yolcularÄ±nÄ± oluÅŸtursun
            newUnderpass.Initialize(this.gridManager, data.position, passengerPrefab, data.passengerSequence);
            activeUnderpasses.Add(newUnderpass);

            // OluÅŸturulan yolcularÄ±, hangi alt geÃ§ide ait olduklarÄ±nÄ± bilmek iÃ§in haritaya ekle
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
