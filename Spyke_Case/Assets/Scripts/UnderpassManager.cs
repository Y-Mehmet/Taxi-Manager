using UnityEngine;
using System.Collections.Generic;

public class UnderpassManager : MonoBehaviour
{
    [Header("Prefabs")]
    public UnderpassController underpassPrefab;
    public PassengerGroup passengerGroupPrefab;

    [Header("Configuration")]
    public PassengerGroupSequenceSO defaultSequence;
    public List<Vector2Int> underpassSpawnPoints; // Alt geçitlerin spawn edileceği grid koordinatları

    private GridManager gridManager;
    private Dictionary<PassengerGroup, UnderpassController> groupToUnderpassMap = new Dictionary<PassengerGroup, UnderpassController>();

    private void OnEnable()
    {
        PassengerGroup.OnGroupDeparted += OnPassengerGroupDeparted;
    }

    private void OnDisable()
    {
        PassengerGroup.OnGroupDeparted -= OnPassengerGroupDeparted;
    }

    private void Start()
    {
        gridManager = FindObjectOfType<GridManager>();
        if (gridManager == null)
        {
            Debug.LogError("Sahnede GridManager bulunamadı!");
            return;
        }

        if (underpassPrefab == null || passengerGroupPrefab == null || defaultSequence == null)
        {
            Debug.LogError("UnderpassManager'da gerekli prefablar veya sequence atanmamış!");
            return;
        }

        InitializeUnderpasses();
    }

    private void InitializeUnderpasses()
    {
        foreach (var spawnPoint in underpassSpawnPoints)
        {
            Vector3 spawnPos = gridManager.GetWorldPosition(spawnPoint);
            UnderpassController newUnderpass = Instantiate(underpassPrefab, spawnPos, Quaternion.identity, transform);
            newUnderpass.name = $"Underpass_{spawnPoint.x}_{spawnPoint.y}";

            // Yeni alt geçidi başlat ve içindeki yolcuları oluştur
            newUnderpass.Initialize(gridManager, spawnPoint, passengerGroupPrefab, defaultSequence);

            // Oluşturulan yolcuları, hangi alt geçide ait olduklarını bilmek için haritaya ekle
            foreach (var groupInQueue in newUnderpass.GetQueue())
            {
                groupToUnderpassMap[groupInQueue] = newUnderpass;
            }
        }
    }

    private void OnPassengerGroupDeparted(PassengerGroup departedGroup)
    {
        // Ayrılan grubun hangi alt geçide ait olduğunu bul ve o alt geçide haber ver
        if (groupToUnderpassMap.TryGetValue(departedGroup, out UnderpassController controller))
        {
            controller.HandleDeparture();
            groupToUnderpassMap.Remove(departedGroup); // Haritadan temizle
        }
    }
}