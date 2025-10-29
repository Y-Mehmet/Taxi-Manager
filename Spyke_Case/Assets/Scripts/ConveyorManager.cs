using System.Collections;
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

    public IEnumerator Initialize(List<PassengerSpawnData> conveyorPassengers, PassengerGroup passengerGroupPrefab)
    {
        if (conveyorPassengers == null || conveyorPassengers.Count == 0)
        {
            yield break;
        }

        yield return new WaitUntil(() => ConveyorBelt.Instance != null);

        this.passengerGroupPrefab = passengerGroupPrefab;

        for (int i = 0; i < conveyorPassengers.Count; i++)
        {
            PassengerSpawnData passengerData = conveyorPassengers[i];
            
            PassengerGroup newPassengerGroup = Instantiate(passengerGroupPrefab);
            
            newPassengerGroup.transform.SetParent(ConveyorBelt.Instance.transform);

            newPassengerGroup.useGridPosition = false;
            newPassengerGroup.onConveyorBelt = true;
            newPassengerGroup.moveDirection = Vector2Int.up;
            newPassengerGroup.SetGroupColor(passengerData.color);

            // --- CORRECTED INITIAL POSITIONING ---
            // Use the belt's startPoint.right vector for correct spacing regardless of rotation
            float spacing = ConveyorBelt.Instance.passengerSpacing;
            Vector3 offset = ConveyorBelt.Instance.startPoint.right * i * spacing;
            Vector3 spawnPosition = ConveyorBelt.Instance.startPoint.position + offset;
            newPassengerGroup.transform.position = spawnPosition;

            ConveyorBelt.Instance.AddPassenger(newPassengerGroup);
        }

        // --- SET THE INITIAL TAIL POSITION FOR RECYCLING ---
        // The tail is where the *next* passenger would spawn.
        float finalSpacing = ConveyorBelt.Instance.passengerSpacing;
        Vector3 tailOffset = ConveyorBelt.Instance.startPoint.right * conveyorPassengers.Count * finalSpacing;
        Vector3 initialTailPosition = ConveyorBelt.Instance.startPoint.position + tailOffset;
        ConveyorBelt.Instance.SetInitialTailPosition(initialTailPosition);
    }

    public void RemovePassenger(PassengerGroup passenger)
    {
        if (ConveyorBelt.Instance != null)
        {
            ConveyorBelt.Instance.RemovePassenger(passenger);
        }
    }
}
