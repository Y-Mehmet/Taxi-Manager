
using System.Collections.Generic;
using GridSystem;
using UnityEngine;

public class ConveyorManager : MonoBehaviour
{
    public static ConveyorManager Instance { get; private set; }

    [SerializeField] private ConveyorBelt conveyorBeltPrefab;

    private List<ConveyorBelt> activeConveyors = new List<ConveyorBelt>();
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

    public void Initialize(List<ConveyorSpawnData> conveyorSpawns, PassengerGroup passengerGroupPrefab)
    {
        if (conveyorSpawns == null || conveyorSpawns.Count == 0)
        {
            return;
        }

        this.passengerGroupPrefab = passengerGroupPrefab;

        foreach (var spawnData in conveyorSpawns)
        {
            Transform startPoint = new GameObject("ConveyorStartPoint").transform;
            startPoint.position = spawnData.startPoint;
            startPoint.parent = transform;

            Transform endPoint = new GameObject("ConveyorEndPoint").transform;
            endPoint.position = spawnData.endPoint;
            endPoint.parent = transform;

            ConveyorBelt newBelt = Instantiate(conveyorBeltPrefab, transform);
            
            List<PassengerGroup> initialGroups = new List<PassengerGroup>();
            foreach(var passengerData in spawnData.initialPassengerGroups)
            {
                PassengerGroup newPassengerGroup = Instantiate(passengerGroupPrefab);
                newPassengerGroup.useGridPosition = false;
                newPassengerGroup.onConveyorBelt = true;
               
                newPassengerGroup.SetGroupColor(passengerData.color);
                newPassengerGroup.transform.position = startPoint.position;
                initialGroups.Add(newPassengerGroup);
            }

            newBelt.Initialize(spawnData.speed, startPoint, endPoint, initialGroups);
            activeConveyors.Add(newBelt);
        }
    }

    public void RemovePassenger(PassengerGroup passenger)
    {
        foreach(var belt in activeConveyors)
        {
            belt.RemovePassenger(passenger);
        }
    }
}
