using System.Collections.Generic;
using UnityEngine;

public class ConveyorBelt : MonoBehaviour
{
    public static ConveyorBelt Instance { get; private set; }

    [Header("Belt Configuration")]
    [SerializeField] private float speed = 1f;
    public float passengerSpacing = 1.2f; // Made public to be accessed by ConveyorManager
    public Transform startPoint;
    public Transform endPoint;

    [Header("Runtime State")]
    [SerializeField] private List<PassengerGroup> passengerGroupsOnBelt = new List<PassengerGroup>();
    [SerializeField] private Vector3 queueTailPosition; // The position for the next recycled passenger

    private Vector3 beltDirection;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            beltDirection = (endPoint.position - startPoint.position).normalized;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetInitialTailPosition(Vector3 position)
    {
        queueTailPosition = position;
    }

    void Update()
    {
        if (passengerGroupsOnBelt.Count == 0) return;

        // Iterate backwards to safely remove items while looping
        for (int i = passengerGroupsOnBelt.Count - 1; i >= 0; i--)
        {
            var passenger = passengerGroupsOnBelt[i];
            if (passenger == null)
            {
                passengerGroupsOnBelt.RemoveAt(i);
                continue;
            }

            // Check if the passenger has passed the end point
            if (Vector3.Dot(endPoint.position - passenger.transform.position, beltDirection) < 0)
            {
                // --- Simplified Recycling Logic ---
                passenger.transform.position = queueTailPosition;
                // Update the tail position for the next passenger, moving it further out
                queueTailPosition += startPoint.right * passengerSpacing;
            }
            else
            {
                // Move passenger along the belt
                passenger.transform.position += beltDirection * speed * Time.deltaTime;
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
