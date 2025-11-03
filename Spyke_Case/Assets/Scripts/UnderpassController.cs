
using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using System.Linq;
using System.Text;

public class UnderpassController : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Bu objenin grid pozisyonuna göre aktif yolcunun duracağı hücrenin ofseti.")]
    public Vector2Int startCellOffset = new Vector2Int(-1, 0);

    [Header("References")]
    [Tooltip("Prefab'in kendi altındaki sayaç text'i.")]
    public TMP_Text queueCounterText;
    public GameObject objectToRotate;

    // Internal State
    private Queue<PassengerGroup> passengerQueue = new Queue<PassengerGroup>();
    private GridManager gridManager;
    private Vector2Int myGridPosition;
    private Tween activeQueueAnimation = null;

    private void Awake()
    {
        if (queueCounterText == null)
        {
            Debug.LogError("Kuyruk Sayacı Text'i (Queue Counter Text) atanmamış!", gameObject);
        }
    }

    public void Initialize(GridManager gridManager, Vector2Int gridPosition, PassengerGroup passengerPrefab, List<HyperCasualColor> sequence)
    {
        this.gridManager = gridManager;
        this.myGridPosition = gridPosition;
        if (objectToRotate != null)
        {
            float yRotation = 0f;
            if (startCellOffset.x == -1) // Left
            {
                yRotation = -90f;
            }
            else if (startCellOffset.x == 1) // Right
            {
                yRotation = 90f;
            }
            else if (startCellOffset.y == 1) // Up
            {
                yRotation = 0;
            }
            else if (startCellOffset.y == -1) // Down
            {
                yRotation = 180;
            }
            objectToRotate.transform.rotation = Quaternion.Euler(-25, yRotation, 90);
        }
        int groupIndex = 0;
        foreach (var color in sequence)
        {
            Vector3 spawnPos = transform.position;
            PassengerGroup newGroup = Instantiate(passengerPrefab, spawnPos, Quaternion.identity, transform);
            newGroup.name = groupIndex.ToString();
            newGroup.SetGroupColor(color);
            newGroup.moveDirection = this.startCellOffset;
            newGroup.OriginUnderpass = this;
            newGroup.followTarget = null;
            newGroup.railMode = false;

            // Subscribe to the INSTANCE event for this specific passenger
            newGroup.OnGroupDeparted += OnPassengerDeparted;

            if (groupIndex > 0)
            {
                newGroup.gameObject.SetActive(false);
            }

            passengerQueue.Enqueue(newGroup);
            groupIndex++;
        }

        ActivateFirstPassenger();
        UpdateCounterText();
        LogQueueState("Initial State");
    }

    private void OnPassengerDeparted(PassengerGroup departedGroup)
    {
        // Safety check: This should always be true now that we use instance events.
        if (passengerQueue.Count > 0 && passengerQueue.Peek() == departedGroup)
        {
            HandleDeparture();
            LogQueueState($"After {departedGroup.name} Departed");
        }
    }

    public void HandleDeparture()
    {
        if (passengerQueue.Count > 0)
        {
            // Unsubscribe from the departing passenger's event to prevent memory leaks
            PassengerGroup departedPassenger = passengerQueue.Dequeue();
            if(departedPassenger != null) departedPassenger.OnGroupDeparted -= OnPassengerDeparted;
        }

        UpdateCounterText();

        if (passengerQueue.Count > 0)
        {
            AnimateNextPassengerToStart();
        }
        else // The queue is now empty
        {
            // Ensure the waiting spot is cleared in the grid system
            Vector2Int waitingSpot = myGridPosition + startCellOffset;
            if (GridSystem.PassengerGrid.Instance != null)
            {
                var occupant = GridSystem.PassengerGrid.Instance.GetOccupant(waitingSpot);
                if (occupant != null)
                {
                    Debug.Log($"[UnderpassController] Last passenger departed. Clearing occupant {occupant.name} from waiting spot {waitingSpot}.");
                    GridSystem.PassengerGrid.Instance.UnregisterOccupant(waitingSpot, occupant);
                }
            }
        }
    }

    public void ReturnPassengerToEndOfQueue(PassengerGroup returnedPassenger)
    {
        if (returnedPassenger == null) return;

        // Defensive check: Prevent adding a duplicate if it was never properly dequeued.
        if (passengerQueue.Contains(returnedPassenger))
        {
            Debug.LogWarning($"[UnderpassController] Tried to re-add {returnedPassenger.name} which was already in the queue. Aborting recall to prevent duplicates.");
            return;
        }

        bool queueWasEmpty = passengerQueue.Count == 0;

        // Deactivate and position the passenger in the hiding spot first
        returnedPassenger.gameObject.SetActive(false);
        returnedPassenger.transform.position = this.transform.position;

        // Set rotation
        Transform transformToRotate = returnedPassenger.modelTransform != null ? returnedPassenger.modelTransform : returnedPassenger.transform;
        Vector3 directionVector = new Vector3(returnedPassenger.moveDirection.x, 0, returnedPassenger.moveDirection.y);
        if (directionVector != Vector3.zero)
        {
            transformToRotate.rotation = Quaternion.LookRotation(directionVector);
        }

        // Add to the queue
        passengerQueue.Enqueue(returnedPassenger);

        // Re-subscribe to the event for this passenger that is now back in the queue
        returnedPassenger.OnGroupDeparted += OnPassengerDeparted;

        UpdateCounterText();
        LogQueueState($"After {returnedPassenger.name} Recalled");

        // If the queue was empty, this new passenger is now the active one.
        if (queueWasEmpty)
        {
            Debug.Log($"[UnderpassController] Queue was empty. Activating {returnedPassenger.name} as the new lead.");
            AnimateNextPassengerToStart();
        }
    }

    private void ActivateFirstPassenger()
    {
        if (passengerQueue.Count > 0)
        {
            PassengerGroup firstGroup = passengerQueue.Peek();
            firstGroup.gameObject.SetActive(true);
            Vector2Int startCellGridPos = myGridPosition + startCellOffset;

            if (GridSystem.PassengerGrid.Instance != null)
            {
                GridSystem.PassengerGrid.Instance.RegisterOccupant(startCellGridPos, firstGroup);
            }
            firstGroup.gridPos = startCellGridPos;
            firstGroup.homeGridPos = startCellGridPos;
            Debug.Log($"[UnderpassController] Set homeGridPos for {firstGroup.name} to {startCellGridPos}");

            firstGroup.transform.position = gridManager.GetWorldPosition(startCellGridPos);
            firstGroup.GetComponent<Collider>().enabled = true;

            Transform transformToRotate = firstGroup.modelTransform != null ? firstGroup.modelTransform : firstGroup.transform;
            Vector3 directionVector = new Vector3(firstGroup.moveDirection.x, 0, firstGroup.moveDirection.y);
            if (directionVector != Vector3.zero)
            {
                transformToRotate.rotation = Quaternion.LookRotation(directionVector);
            }
        }
    }

    private void AnimateNextPassengerToStart()
    {
        PassengerGroup nextGroup = passengerQueue.Peek();
        nextGroup.gameObject.SetActive(true);

        Vector2Int startCellGridPos = myGridPosition + startCellOffset;
        Vector3 targetPos = gridManager.GetWorldPosition(startCellGridPos);

        // Register the passenger in the grid at its destination BEFORE animating.
        if (GridSystem.PassengerGrid.Instance != null)
        {
            GridSystem.PassengerGrid.Instance.RegisterOccupant(startCellGridPos, nextGroup);
        }
        nextGroup.gridPos = startCellGridPos;
        nextGroup.homeGridPos = startCellGridPos;
        Debug.Log($"[UnderpassController] Set homeGridPos for {nextGroup.name} to {startCellGridPos}");

        activeQueueAnimation = nextGroup.transform.DOMove(targetPos, 0.5f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                nextGroup.GetComponent<Collider>().enabled = true;
                activeQueueAnimation = null;
            });
    }

    private void UpdateCounterText()
    {
        if (queueCounterText != null)
        {
            int waitingCount = passengerQueue.Count > 0 ? passengerQueue.Count - 1 : 0;
            queueCounterText.text = waitingCount.ToString();
        }
    }

    private void LogQueueState(string context)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append($"<color=cyan>[{context}] Underpass '{name}' Queue:</color> ");
        if (passengerQueue.Count == 0)
        {
            sb.Append("(Empty)");
        }
        else
        {
            PassengerGroup activePassenger = passengerQueue.Peek();
            foreach (var passenger in passengerQueue)
            {
                sb.Append($"{passenger.name} ({(passenger == activePassenger ? "Active" : "Passive")}), ");
            }
        }
        Debug.Log(sb.ToString());
    }

    public Queue<PassengerGroup> GetQueue()
    {
        return passengerQueue;
    }

    public Vector2Int GetWaitingSpotGridPosition()
    {
        return myGridPosition + startCellOffset;
    }
}
