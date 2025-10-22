using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

public class StopManager : MonoBehaviour
{
    public static StopManager Instance { get; private set; }

    [Tooltip("Sahnedeki tüm durak objelerini (aktif veya değil) buraya sürükleyin. İsimlerine göre sıralanacaklardır.")]
    public List<Stop> AllPossibleStops = new List<Stop>();

    public static event Action<PassengerGroup, int> OnPassengerArrivedAtStop;

    public List<Stop> AllStops { get; private set; } = new List<Stop>();
    private Dictionary<int, PassengerGroup> reservedStops = new Dictionary<int, PassengerGroup>();
    private Dictionary<int, PassengerGroup> occupiedStops = new Dictionary<int, PassengerGroup>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        FindAndRegisterInitialStops();
    }

    private void FindAndRegisterInitialStops()
    {
        AllStops.Clear();
        occupiedStops.Clear();
        reservedStops.Clear();

        if (AllPossibleStops == null || AllPossibleStops.Count == 0)
        {
            Debug.LogError("[StopManager] 'AllPossibleStops' list is not assigned in the inspector!");
            return;
        }

        var sortedMasterList = AllPossibleStops.Where(s => s != null).OrderBy(s => s.name).ToList();
        
        Debug.Log($"[StopManager] Found {sortedMasterList.Count} stops in master list. Will activate the first 3.");

        for (int i = 0; i < sortedMasterList.Count; i++)
        {
            Stop stop = sortedMasterList[i];
            
            if (i < 3)
            {
                stop.gameObject.SetActive(true);
                if (!AllStops.Contains(stop))
                {
                    AllStops.Add(stop);
                }
            }
            else
            {
                stop.gameObject.SetActive(false);
            }
        }

        Debug.Log($"[StopManager] Initialization complete. Active stops: {AllStops.Count}");
        for(int i = 0; i < AllStops.Count; i++)
        {
            Debug.Log($"  -> Active Stop Index [{i}]: {AllStops[i].name}");
        }
    }

    public void ActivateNextStop()
    {
        Stop nextStop = AllPossibleStops
            .OrderBy(s => s.name)
            .FirstOrDefault(s => s != null && !AllStops.Contains(s));

        if (nextStop != null)
        {
            Debug.Log($"[StopManager] Activating next stop via ability: {nextStop.name}");
            nextStop.gameObject.SetActive(true);
            RegisterStop(nextStop);
        }
        else
        {
            Debug.LogWarning("[StopManager] Tried to activate next stop, but no more inactive stops are available.");
        }
    }

    public void RegisterStop(Stop newStop)
    {
        if (newStop != null && !AllStops.Contains(newStop))
        {
            AllStops.Add(newStop);
            AllStops = AllStops.OrderBy(s => s.name).ToList();
            Debug.Log($"[StopManager] A new stop '{newStop.name}' was registered. Total active stops: {AllStops.Count}");
        }
    }

    public Dictionary<int, PassengerGroup> GetOccupiedStops()
    {
        return occupiedStops;
    }

    public void FreeStop(int stopIndex)
    {
        if (stopIndex < 0 || stopIndex >= AllStops.Count) return;

        if (occupiedStops.ContainsKey(stopIndex) || reservedStops.ContainsKey(stopIndex))
        {
            Debug.Log($"<color=red>[StopManager] Stop {stopIndex} is now FREE.</color>");
            occupiedStops.Remove(stopIndex);
            reservedStops.Remove(stopIndex);
        }
    }

    public PassengerGroup GetPassengerAtStop(int stopIndex)
    {
        occupiedStops.TryGetValue(stopIndex, out PassengerGroup group);
        return group;
    }

    public bool IsStopOccupied(int stopIndex)
    {
        return occupiedStops.ContainsKey(stopIndex);
    }

    public bool IsStopReserved(int stopIndex)
    {
        return reservedStops.ContainsKey(stopIndex);
    }

    public bool HasAvailableStops()
    {
        int availableCount = AllStops.Count - (occupiedStops.Count + reservedStops.Count);
        if (availableCount <= 0)
        {
            Debug.LogWarning($"[StopManager] HasAvailableStops check: All stops are full or reserved. " +
                             $"Total: {AllStops.Count}, Occupied: {occupiedStops.Count}, Reserved: {reservedStops.Count}");
        }
        return availableCount > 0;
    }

    public (Vector3 stopWorldPos, int stopIndex)? ReserveFirstFreeStop(PassengerGroup passengerGroup)
    {
        Debug.Log($"[StopManager] Attempting to reserve a stop for {passengerGroup.name}. " +
                  $"TotalStops: {AllStops.Count}, Occupied: {occupiedStops.Count}, Reserved: {reservedStops.Count}");

        for (int i = 0; i < AllStops.Count; i++)
        {
            if (!occupiedStops.ContainsKey(i) && !reservedStops.ContainsKey(i))
            {
                reservedStops.Add(i, passengerGroup);
                Debug.Log($"<color=yellow>[StopManager] Stop {i} RESERVED by {passengerGroup.name}.</color>");
                return (AllStops[i].transform.position, i);
            }
            else
            {
                string status = occupiedStops.ContainsKey(i) ? "OCCUPIED" : "RESERVED";
                PassengerGroup pg = occupiedStops.ContainsKey(i) ? occupiedStops[i] : reservedStops[i];
                Debug.Log($"[StopManager] Checking Stop {i}: SKIPPED ({status} by {pg.name})");
            }
        }

        Debug.LogWarning($"[StopManager] {passengerGroup.name} tried to reserve a stop, but none are available. " +
                         $"Final check - Occupied: {occupiedStops.Count}, Reserved: {reservedStops.Count}");
        return null;
    }

    public Vector3 GetStopWorldPosition(int stopIndex)
    {
        if (stopIndex >= 0 && stopIndex < AllStops.Count)
        {
            return AllStops[stopIndex].transform.position;
        }
        return Vector3.zero;
    }

    public void CancelReservation(int stopIndex, PassengerGroup passengerGroup)
    {
        if (reservedStops.ContainsKey(stopIndex) && reservedStops[stopIndex] == passengerGroup)
        {
            Debug.Log($"<color=orange>[StopManager] Reservation for Stop {stopIndex} by {passengerGroup.name} CANCELLED.</color>");
            reservedStops.Remove(stopIndex);
        }
    }

    public void ConfirmArrivalAtStop(int stopIndex, PassengerGroup passengerGroup)
    {
        if (stopIndex < 0 || stopIndex >= AllStops.Count) return;

        reservedStops.Remove(stopIndex);
        if (!occupiedStops.ContainsKey(stopIndex))
        {
            occupiedStops.Add(stopIndex, passengerGroup);
            Debug.Log($"<color=green>[StopManager] Stop {stopIndex} OCCUPIED by {passengerGroup.name}.</color>");
        }

        OnPassengerArrivedAtStop?.Invoke(passengerGroup, stopIndex);
    }

    public void EvictPassenger(PassengerGroup passengerToEvict)
    {
        if (passengerToEvict == null) return;

        int stopToFree = -1;

        // Check occupied stops
        foreach (var pair in occupiedStops)
        {
            if (pair.Value == passengerToEvict)
            {
                stopToFree = pair.Key;
                break;
            }
        }

        if (stopToFree != -1)
        {
            occupiedStops.Remove(stopToFree);
            Debug.Log($"<color=blue>[StopManager] Passenger {passengerToEvict.name} EVICTED from occupied stop {stopToFree}. Stop is now free.</color>");
            return; // Found and handled
        }

        // Check reserved stops
        foreach (var pair in reservedStops)
        {
            if (pair.Value == passengerToEvict)
            {
                stopToFree = pair.Key;
                break;
            }
        }

        if (stopToFree != -1)
        {
            reservedStops.Remove(stopToFree);
            Debug.Log($"<color=blue>[StopManager] Passenger {passengerToEvict.name}'s reservation for stop {stopToFree} CANCELLED due to eviction.</color>");
        }
    }
}
