using System.Collections.Generic;
using UnityEngine;

public class ConveyorBelt : MonoBehaviour
{
    public static ConveyorBelt Instance { get; private set; }

    [SerializeField] private List<PassengerGroup> passengerGroupsOnBelt = new List<PassengerGroup>();
    [SerializeField] private float speed = 2f;
    
    [Header("Belt Configuration")]
    public Transform startPoint;
    public Transform endPoint;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.LogWarning("[ConveyorBelt] Singleton instance is set. ConveyorBelt is ready.");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        // Each passenger moves independently.
        foreach (var passenger in passengerGroupsOnBelt)
        {
            if (passenger == null || !passenger.gameObject.activeSelf) continue;

            // If passenger reaches the end, loop it back to the start.
            if (passenger.transform.position.x < endPoint.position.x)
            {
                passenger.transform.position = startPoint.position;
            }
            else
            {
                // Otherwise, move it left.
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
