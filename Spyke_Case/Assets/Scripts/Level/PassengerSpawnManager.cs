using UnityEngine;
using System.Collections.Generic;

// LevelSpawnSO'daki basit yolcu gruplarını spawn eder.
public class PassengerSpawnManager : MonoBehaviour
{
    public void Initialize(List<PassengerSpawnData> spawnData, PassengerGroup prefab, GridManager gridManager)
    {
        Debug.Log("[PassengerSpawnManager] Initializing...");

        if (prefab == null)
        {
            Debug.LogError("[PassengerSpawnManager] PassengerGroup prefab is not assigned!");
            return;
        }

        if (gridManager == null)
        {
            Debug.LogError("[PassengerSpawnManager] GridManager is not assigned!");
            return;
        }

        if (spawnData == null || spawnData.Count == 0)
        {
            Debug.LogWarning("[PassengerSpawnManager] No passenger spawn data provided for this level.");
            return;
        }

        Debug.Log($"[PassengerSpawnManager] Received {spawnData.Count} passenger groups to spawn.");

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
            Debug.Log($"[PassengerSpawnManager] Spawned passenger group at {data.position}");
        }
    }
}