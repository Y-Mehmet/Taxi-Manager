
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
    }

    public void ReturnPassengerToEndOfQueue(PassengerGroup returnedPassenger)
    {
        if (returnedPassenger == null) return;

        returnedPassenger.gameObject.SetActive(false);
        returnedPassenger.transform.position = this.transform.position;

        Transform transformToRotate = returnedPassenger.modelTransform != null ? returnedPassenger.modelTransform : returnedPassenger.transform;
        Vector3 directionVector = new Vector3(returnedPassenger.moveDirection.x, 0, returnedPassenger.moveDirection.y);
        if (directionVector != Vector3.zero)
        {
            transformToRotate.rotation = Quaternion.LookRotation(directionVector);
        }

        passengerQueue.Enqueue(returnedPassenger);
        UpdateCounterText();
        LogQueueState($"After {returnedPassenger.name} Recalled");
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
}
