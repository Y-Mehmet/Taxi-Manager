
using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using System.Linq;

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

    private void Awake()
    {
        if (queueCounterText == null)
        {
            Debug.LogError("Kuyruk Sayacı Text'i (Queue Counter Text) atanmamış!", gameObject);
        }
    }

    /// <summary>
    /// UnderpassManager tarafından çağrılarak bu alt geçidi başlatır.
    /// </summary>
    public void Initialize(GridManager gridManager, Vector2Int gridPosition, PassengerGroup passengerPrefab, List<HyperCasualColor> sequence)
    {
        this.gridManager = gridManager;
        this.myGridPosition = gridPosition;

        // Yolcuları oluştur ve kuyruğa ekle
        foreach (var color in sequence)
        {
            Vector3 spawnPos = transform.position; // Bu obje zaten EndCell'e spawn edilecek
            PassengerGroup newGroup = Instantiate(passengerPrefab, spawnPos, Quaternion.identity, transform);
            newGroup.SetGroupColor(color);
            newGroup.moveDirection = this.startCellOffset;
            newGroup.GetComponent<Collider>().enabled = false;
            
            // Set origin for ability logic
            newGroup.OriginUnderpass = this;

            passengerQueue.Enqueue(newGroup);
        }

        // --- GEMINI-DEBUG: Log spawned passenger colors ---
        Debug.LogWarning($"--- Logging Spawned Passenger Colors for Underpass {name} ---");
        int groupIndex = 0;
        foreach (var group in passengerQueue)
        {
            if (group != null)
            {
                Debug.LogWarning($"Spawned Passenger [{groupIndex}] has color: {group.groupColor}");
            }
            else
            {
                Debug.LogWarning($"Spawned Passenger [{groupIndex}] is NULL.");
            }
            groupIndex++;
        }
        Debug.LogWarning("--- End of Spawned Passenger Log ---");
        // --- END GEMINI-DEBUG ---

        // İlk yolcuyu bekleme noktasına taşı ve aktifleştir
        ActivateFirstPassenger();
        UpdateCounterText();
    }

    /// <summary>
    /// Bir yolcu bu kuyruktan ayrıldığında UnderpassManager tarafından çağrılır.
    /// </summary>
    public void HandleDeparture()
    {
        if (passengerQueue.Count > 0)
        {
            PassengerGroup departedGroup = passengerQueue.Dequeue();
            // İsteğe bağlı: departedGroup objesini yok et (Destroy)
        }

        UpdateCounterText();

        if (passengerQueue.Count > 0)
        {
            AnimateNextPassengerToStart();
        }
    }

    public void ReturnPassengerToFront(PassengerGroup returnedPassenger)
    {
        Debug.Log($"[UnderpassController] {name} is recalling {returnedPassenger.name} to the front of the queue.");

        List<PassengerGroup> newQueueOrder = new List<PassengerGroup>();
        newQueueOrder.Add(returnedPassenger);

        if (passengerQueue.Count > 0)
        {
            PassengerGroup currentActive = passengerQueue.Peek();
            if (currentActive != null && currentActive != returnedPassenger)
            {
                Debug.Log($"[UnderpassController] Deactivating current passenger {currentActive.name} and putting it behind {returnedPassenger.name}.");
                currentActive.GetComponent<Collider>().enabled = false;
                currentActive.transform.position = this.transform.position; // Move to hiding spot
            }
            newQueueOrder.AddRange(passengerQueue.ToList());
        }

        passengerQueue = new Queue<PassengerGroup>(newQueueOrder);

        ActivateFirstPassenger();
        UpdateCounterText();
    }

    private void ActivateFirstPassenger()
    {
        if (passengerQueue.Count > 0)
        {
            PassengerGroup firstGroup = passengerQueue.Peek();
            Vector2Int startCellGridPos = myGridPosition + startCellOffset;

            // Update grid system and passenger state
            if (GridSystem.PassengerGrid.Instance != null)
            {
                GridSystem.PassengerGrid.Instance.RegisterOccupant(startCellGridPos, firstGroup);
            }
            firstGroup.gridPos = startCellGridPos;

            // Update transform
            firstGroup.transform.position = gridManager.GetWorldPosition(startCellGridPos);
            firstGroup.GetComponent<Collider>().enabled = true;

            // Set rotation to face its move direction
            Transform transformToRotate = firstGroup.modelTransform != null ? firstGroup.modelTransform : firstGroup.transform;
            Vector3 directionVector = new Vector3(firstGroup.moveDirection.x, 0, firstGroup.moveDirection.y);
            if (directionVector != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionVector);
                transformToRotate.rotation = targetRotation;
            }
        }
    }

    private void AnimateNextPassengerToStart()
    {
        PassengerGroup nextGroup = passengerQueue.Peek();
        Vector2Int startCellGridPos = myGridPosition + startCellOffset;
        Vector3 targetPos = gridManager.GetWorldPosition(startCellGridPos);

        // Animasyon tamamlandığında grubu tıklanabilir yap
        nextGroup.transform.DOMove(targetPos, 0.5f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => 
            {
                nextGroup.GetComponent<Collider>().enabled = true;
            });
    }

    private void UpdateCounterText()
    {
        if (queueCounterText != null)
        {
            // Aktif olan hariç, kuyrukta bekleyenlerin sayısı
            int waitingCount = passengerQueue.Count > 0 ? passengerQueue.Count - 1 : 0;
            queueCounterText.text = waitingCount.ToString();
        }
    }

    public Queue<PassengerGroup> GetQueue()
    {
        return passengerQueue;
    }
}
