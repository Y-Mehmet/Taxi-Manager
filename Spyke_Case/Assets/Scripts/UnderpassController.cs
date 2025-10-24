
using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;

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
    public void Initialize(GridManager gridManager, Vector2Int gridPosition, PassengerGroup passengerPrefab, PassengerGroupSequenceSO sequence)
    {
        this.gridManager = gridManager;
        this.myGridPosition = gridPosition;

        // Yolcuları oluştur ve kuyruğa ekle
        foreach (var color in sequence.PassengerColors)
        {
            Vector3 spawnPos = transform.position; // Bu obje zaten EndCell'e spawn edilecek
            PassengerGroup newGroup = Instantiate(passengerPrefab, spawnPos, Quaternion.identity, transform);
            newGroup.SetGroupColor(color);
            newGroup.GetComponent<Collider>().enabled = false;
            passengerQueue.Enqueue(newGroup);
        }

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

    private void ActivateFirstPassenger()
    {
        if (passengerQueue.Count > 0)
        {
            PassengerGroup firstGroup = passengerQueue.Peek();
            Vector2Int startCellGridPos = myGridPosition + startCellOffset;
            firstGroup.transform.position = gridManager.GetWorldPosition(startCellGridPos);
            firstGroup.GetComponent<Collider>().enabled = true;
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
