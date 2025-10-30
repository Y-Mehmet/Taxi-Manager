using System.Collections.Generic;
using UnityEngine;

public class ConveyorBelt : MonoBehaviour
{
    public static ConveyorBelt Instance { get; private set; }

    [Header("Belt Configuration")]
    [SerializeField] private float speed = 1f;
    public Transform startPoint;
    public Transform endPoint;

    [Header("Runtime State")]
    [SerializeField] private List<PassengerGroup> passengerGroupsOnBelt = new List<PassengerGroup>();
    
    // A static, fixed X-coordinate to teleport passengers to, calculated by ConveyorManager.
    private float respawnX;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void SetRespawnX(float x)
    {
        respawnX = x;
        Debug.Log($"[ConveyorBelt] Respawn X coordinate set to: {respawnX}");
    }

    void Update()
    {
        if (passengerGroupsOnBelt.Count == 0) return;

        for (int i = passengerGroupsOnBelt.Count - 1; i >= 0; i--)
        {
            var passenger = passengerGroupsOnBelt[i];
            if (passenger == null)
            {
                passengerGroupsOnBelt.RemoveAt(i);
                continue;
            }

            // Check if the passenger has passed the end point's X coordinate.
            if (passenger.transform.position.x < endPoint.position.x)
            {
                // --- User's Explicit Formula ---
                // Teleport the passenger to the calculated fixed X, preserving its Y and Z.
                var currentPos = passenger.transform.position;
                passenger.transform.position = new Vector3(respawnX, currentPos.y, currentPos.z);
            }
            else
            {
                // Move passenger to the left (negative X).
                passenger.transform.position += Vector3.left * speed * Time.deltaTime;
            }
        }
    }

    public void AddPassenger(PassengerGroup group)
    {
        if (!passengerGroupsOnBelt.Contains(group))
        {
             passengerGroupsOnBelt.Add(group);
        }
    }

    public void RemovePassenger(PassengerGroup passenger)
    {
        if (passengerGroupsOnBelt.Contains(passenger))
        {
            passengerGroupsOnBelt.Remove(passenger);
        }
    }

    public void AddPassengerToEmptySlot(PassengerGroup passenger)
    {
        if (passenger == null) return;

        passenger.transform.SetParent(this.transform);
        passenger.onConveyorBelt = true;

        if (passengerGroupsOnBelt.Count == 0)
        {
            float spawnX = startPoint.position.x;
            Vector3 spawnPosition = new Vector3(spawnX, startPoint.position.y, startPoint.position.z);
            passenger.transform.position = spawnPosition;
            AddPassenger(passenger);
            return;
        }

        passengerGroupsOnBelt.Sort((a, b) => b.transform.position.x.CompareTo(a.transform.position.x));

        const float desiredSpacing = 1.5f;
        bool slotFound = false;

        for (int i = 0; i < passengerGroupsOnBelt.Count - 1; i++)
        {
            Vector3 currentPos = passengerGroupsOnBelt[i].transform.position;
            Vector3 nextPos = passengerGroupsOnBelt[i + 1].transform.position;

            if (currentPos.x - nextPos.x > desiredSpacing * 2)
            {
                float spawnX = nextPos.x + (currentPos.x - nextPos.x) / 2;
                Vector3 spawnPosition = new Vector3(spawnX, startPoint.position.y, startPoint.position.z);
                passenger.transform.position = spawnPosition;
                slotFound = true;
                break;
            }
        }

        if (!slotFound)
        {
            float lastX = passengerGroupsOnBelt[0].transform.position.x;
            float spawnX = lastX + desiredSpacing;
            Vector3 spawnPosition = new Vector3(spawnX, startPoint.position.y, startPoint.position.z);
            passenger.transform.position = spawnPosition;
        }

        AddPassenger(passenger);
    }
}
