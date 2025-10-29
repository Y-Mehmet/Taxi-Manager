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
}
