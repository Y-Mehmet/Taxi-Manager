
using System.Collections.Generic;
using UnityEngine;

public class ConveyorManager : MonoBehaviour
{
    public static ConveyorManager Instance { get; private set; }

    [SerializeField] private List<PassengerGroup> passengerGroupsOnBelt = new List<PassengerGroup>();
    [SerializeField] private float speed = 2f;
    [SerializeField] private Transform startPoint; 
    [SerializeField] private Transform endPoint;
    
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

    void Update()
    {
        MoveBelt();
        CheckAndLoopPassengers();
    }

    private void MoveBelt()
    {
        foreach (var passengerGroup in passengerGroupsOnBelt)
        {
            if (passengerGroup.gameObject.activeSelf)
            {
                passengerGroup.transform.position += Vector3.left * speed * Time.deltaTime;
            }
        }
    }

    private void CheckAndLoopPassengers()
    {
        for (int i = 0; i < passengerGroupsOnBelt.Count; i++)
        {
            var passengerGroup = passengerGroupsOnBelt[i];
            if (passengerGroup.gameObject.activeSelf && passengerGroup.transform.position.x < endPoint.position.x)
            {
                passengerGroup.gameObject.SetActive(false);
                
                // Move to the end of the queue conceptually
                passengerGroupsOnBelt.RemoveAt(i);
                passengerGroupsOnBelt.Add(passengerGroup);
                i--; // Adjust index after removal
                
                // Reposition for the next loop
                passengerGroup.transform.position = startPoint.position; 
            }
        }

        // Check if there is space to activate the next one in line
        // This part will be detailed further. For now, we just loop them.
        // A simple approach to start:
        foreach (var passengerGroup in passengerGroupsOnBelt)
        {
            if (!passengerGroup.gameObject.activeSelf)
            {
                // A more complex logic is needed to place it correctly after the last active passenger
                // For now, let's just activate it at the start if it's inactive.
                passengerGroup.transform.position = startPoint.position;
                passengerGroup.gameObject.SetActive(true);
                break; // Activate one per frame to avoid overlaps
            }
        }
    }
    
    public void AddPassengerGroup(PassengerGroup group)
    {
        passengerGroupsOnBelt.Add(group);
        group.transform.position = startPoint.position; // Position it at the start
        group.gameObject.SetActive(true); // Make it visible
    }

    public void RemovePassenger(PassengerGroup passenger)
    {
        if (passengerGroupsOnBelt.Contains(passenger))
        {
            passengerGroupsOnBelt.Remove(passenger);
        }
    }
}
