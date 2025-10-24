
using UnityEngine;
using System.Collections.Generic;

// LevelSpawnSO'daki basit yolcu gruplarını spawn eder.
public class PassengerSpawnManager : MonoBehaviour
{
    public void Initialize(List<PassengerSpawnData> spawnData, PassengerGroup prefab, GridManager gridManager)
    {
        if (prefab == null)
        {
            Debug.LogError("PassengerSpawnManager'a PassengerGroup prefabı atanmamış!");
            return;
        }

        foreach (var data in spawnData)
        {
            Vector3 spawnPos = gridManager.GetWorldPosition(data.position);
            PassengerGroup newGroup = Instantiate(prefab, spawnPos, Quaternion.identity, transform);
            
            // PassengerGroup scriptindeki değişkenlere göre ayarla
            newGroup.gridPos = data.position;
            newGroup.moveDirection = data.direction;
            newGroup.groupColor = data.color; // Renk ataması
            newGroup.useGridPosition = true; // Grid pozisyonunu kullanmasını sağla

            newGroup.name = $"PassengerGroup_{data.position.x}_{data.position.y}";
        }
    }
}
