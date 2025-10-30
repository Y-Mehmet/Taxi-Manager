using UnityEngine;
using System.Collections.Generic;

// LevelSpawner'dan aldığı veriyle Underpass prefab'larını oluşturur ve yönetir.
public class UnderpassManager : MonoBehaviour
{
    private GridManager gridManager;
    private Dictionary<PassengerGroup, UnderpassController> groupToUnderpassMap = new Dictionary<PassengerGroup, UnderpassController>();

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

            // Oluşturulan yolcuları, hangi alt geçide ait olduklarını bilmek için haritaya ekle
            foreach (var groupInQueue in newUnderpass.GetQueue())
            {
                groupToUnderpassMap[groupInQueue] = newUnderpass;
            }
        }
    }
}