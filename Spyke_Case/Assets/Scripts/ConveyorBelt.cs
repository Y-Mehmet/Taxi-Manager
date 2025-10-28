using System.Collections.Generic;
using UnityEngine;

public class ConveyorBelt : MonoBehaviour
{
    public static ConveyorBelt Instance { get; private set; }

    [SerializeField] private List<PassengerGroup> passengerGroupsOnBelt = new List<PassengerGroup>();
    [SerializeField] private float speed = 2f;
    [SerializeField] private float followLerpSpeed = 10f;
    
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
        MoveBeltTrain();
    }

    private void MoveBeltTrain()
    {
        if (passengerGroupsOnBelt.Count == 0) return;

        // 1. Move the leader
        PassengerGroup leader = passengerGroupsOnBelt[0];
        if (leader != null && leader.gameObject.activeSelf)
        {
            leader.transform.position += Vector3.left * speed * Time.deltaTime;
        }

        // 2. Followers chase the one in front
        for (int i = 1; i < passengerGroupsOnBelt.Count; i++)
        {
            PassengerGroup follower = passengerGroupsOnBelt[i];
            PassengerGroup targetToFollow = passengerGroupsOnBelt[i - 1];

            if (follower != null && follower.gameObject.activeSelf && targetToFollow != null)
            {
                Vector3 targetPosition = targetToFollow.transform.position + new Vector3(1.0f, 0, 0);
                follower.transform.position = Vector3.Lerp(follower.transform.position, targetPosition, Time.deltaTime * followLerpSpeed);
            }
        }
        
        // 3. Check if the leader has gone past the end point to loop
        if (leader != null && leader.transform.position.x < endPoint.position.x)
        {
            // Find the current last passenger
            PassengerGroup lastPassenger = passengerGroupsOnBelt[passengerGroupsOnBelt.Count - 1];
            
            // Reposition the leader to be behind the last passenger
            leader.transform.position = lastPassenger.transform.position + new Vector3(1.0f, 0, 0);

            // Move the leader from the front of the list to the back
            passengerGroupsOnBelt.RemoveAt(0);
            passengerGroupsOnBelt.Add(leader);
        }
    }

    public void AddPassenger(PassengerGroup group)
    {
        // The ConveyorManager now sets the initial position.
        // This method just adds the group to the list.
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
