using System.Collections.Generic;
using UnityEngine;

public class ConveyorManager : MonoBehaviour
{
    public static ConveyorManager Instance { get; private set; }

    private PassengerGroup passengerGroupPrefab;

    private void Awake()
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

    public void Initialize(List<HyperCasualColor> passengerColors, PassengerGroup passengerGroupPrefab)
    {
        if (passengerColors == null || passengerColors.Count == 0)
        {
            Debug.Log("[ConveyorManager] No conveyor passengers specified in LevelSpawnSO.");
            return;
        }

        if (ConveyorBelt.Instance == null)
        {
            Debug.LogError("[ConveyorManager] ConveyorBelt.Instance is not found in the scene! Cannot spawn passengers.");
            return;
        }

        this.passengerGroupPrefab = passengerGroupPrefab;

        for (int i = 0; i < passengerColors.Count; i++)
        {
            HyperCasualColor color = passengerColors[i];
            
            // Instantiate the passenger
            PassengerGroup newPassengerGroup = Instantiate(passengerGroupPrefab);
            
            // Set parent to the ConveyorBelt
            newPassengerGroup.transform.SetParent(ConveyorBelt.Instance.transform);

            // Set properties as per the new requirements
            newPassengerGroup.useGridPosition = false; // It's not on the main grid
            newPassengerGroup.onConveyorBelt = true;
            newPassengerGroup.moveDirection = Vector2Int.up; // Default direction
            newPassengerGroup.SetGroupColor(color);

            // Calculate position with 1-unit X offset
            Vector3 spawnPosition = ConveyorBelt.Instance.startPoint.position + new Vector3(i * 1.0f, 0, 0);
            newPassengerGroup.transform.position = spawnPosition;

            // Add the passenger to the belt's management list
            ConveyorBelt.Instance.AddPassenger(newPassengerGroup);
        }
    }

    public void RemovePassenger(PassengerGroup passenger)
    {
        if (ConveyorBelt.Instance != null)
        {
            ConveyorBelt.Instance.RemovePassenger(passenger);
        }
    }
}
