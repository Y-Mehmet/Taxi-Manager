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
        foreach (var passengerGroup in passengerGroupsOnBelt)
        {
            if (!passengerGroup.gameObject.activeSelf)
            {
                passengerGroup.transform.position = startPoint.position;
                passengerGroup.gameObject.SetActive(true);
                break; // Activate one per frame to avoid overlaps
            }
        }
    }

    public void AddPassenger(PassengerGroup group)
    {
        passengerGroupsOnBelt.Add(group);
        group.transform.position = startPoint.position;
        group.gameObject.SetActive(true);
    }

    public void RemovePassenger(PassengerGroup passenger)
    {
        if (passengerGroupsOnBelt.Contains(passenger))
        {
            passengerGroupsOnBelt.Remove(passenger);
        }
    }
}
