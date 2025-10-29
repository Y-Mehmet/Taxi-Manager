using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConveyorManager : MonoBehaviour
{
    public static ConveyorManager Instance { get; private set; }

    private PassengerGroup passengerGroupPrefab;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public IEnumerator Initialize(List<PassengerSpawnData> conveyorPassengers, PassengerGroup passengerGroupPrefab)
    {
        if (conveyorPassengers == null || conveyorPassengers.Count == 0)
        {
            yield break;
        }

        yield return new WaitUntil(() => ConveyorBelt.Instance != null);

        this.passengerGroupPrefab = passengerGroupPrefab;

        // --- Initial Spawn & Calculation based on User's Formula ---

        const float initialXOffset = 3.37f;
        const float spacing = 1.0f;

        for (int i = 0; i < conveyorPassengers.Count; i++)
        {
            PassengerSpawnData passengerData = conveyorPassengers[i];
            PassengerGroup newPassengerGroup = Instantiate(passengerGroupPrefab);
            newPassengerGroup.transform.SetParent(ConveyorBelt.Instance.transform);

            newPassengerGroup.useGridPosition = false;
            newPassengerGroup.onConveyorBelt = true;
            newPassengerGroup.moveDirection = Vector2Int.up; // This is not used for movement, but for logic.
            newPassengerGroup.SetGroupColor(passengerData.color);

            // Per user example: spawn starts at 3.37 and spaces by 1.0
            float spawnX = initialXOffset + (i * spacing);
            var startPointPos = ConveyorBelt.Instance.startPoint.position;
            Vector3 spawnPosition = new Vector3(spawnX, startPointPos.y, startPointPos.z);
            newPassengerGroup.transform.position = spawnPosition;

            ConveyorBelt.Instance.AddPassenger(newPassengerGroup);
        }

        // --- Calculate and set the static respawn X-coordinate ---
        // Formula: (initial passenger count) + 2.37
        float respawnX = conveyorPassengers.Count + 2.37f;
        ConveyorBelt.Instance.SetRespawnX(respawnX);
    }

    public void RemovePassenger(PassengerGroup passenger)
    {
        if (ConveyorBelt.Instance != null)
        {
            ConveyorBelt.Instance.RemovePassenger(passenger);
        }
    }
}
