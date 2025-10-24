
using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;

public class UnderpassManager : MonoBehaviour
{
    public List<Underpass> underpasses;
    public PassengerGroup passengerGroupPrefab;
    public PassengerGroupSequenceSO defaultSequence;

    private GridManager gridManager;
    private Dictionary<PassengerGroup, Underpass> groupToUnderpassMap = new Dictionary<PassengerGroup, Underpass>();

    private void OnEnable()
    {
        PassengerGroup.OnGroupDeparted += OnPassengerGroupDeparted;
    }

    private void OnDisable()
    {
        PassengerGroup.OnGroupDeparted -= OnPassengerGroupDeparted;
    }

    private void Start()
    {
        gridManager = FindObjectOfType<GridManager>();
        if (gridManager == null)
        {
            Debug.LogError("Sahnede GridManager bulunamadı!");
            return;
        }
        InitializeUnderpasses();
    }

    private void InitializeUnderpasses()
    {
        foreach (var underpass in underpasses)
        {
            if (underpass.QueueCounterText == null)
            {
                Debug.LogWarning($"'{underpass.Name}' isimli alt geçidin sayaç text'i atanmamış!");
            }

            foreach (var color in defaultSequence.PassengerColors)
            {
                Vector3 spawnPos = gridManager.GetWorldPosition(underpass.EndCell);
                PassengerGroup newGroup = Instantiate(passengerGroupPrefab, spawnPos, Quaternion.identity, transform);
                
                newGroup.SetGroupColor(color);

                newGroup.GetComponent<Collider>().enabled = false; // Tıklamayı engelle
                underpass.PassengerQueue.Enqueue(newGroup);
                groupToUnderpassMap[newGroup] = underpass; // Hızlı erişim için haritaya ekle
            }

            // Kuyruktaki ilk elemanı başlangıç noktasına taşı ve aktifleştir
            if (underpass.PassengerQueue.Count > 0)
            {
                PassengerGroup firstGroup = underpass.PassengerQueue.Peek();
                firstGroup.transform.position = gridManager.GetWorldPosition(underpass.StartCell);
                firstGroup.GetComponent<Collider>().enabled = true;
            }

            UpdateQueueCounter(underpass);
        }
    }

    private void OnPassengerGroupDeparted(PassengerGroup departedGroup)
    {
        // Ayrılan grubun hangi alt geçide ait olduğunu bul
        if (groupToUnderpassMap.TryGetValue(departedGroup, out Underpass underpass))
        {
            // Grubu kuyruktan çıkar
            if (underpass.PassengerQueue.Count == 0 || underpass.PassengerQueue.Peek() != departedGroup)
            {
                // Bu durum, haritaya eklenmemiş veya sırası bozulmuş bir grup demek. Hata ayıklama için önemli.
                return; 
            }
            
            underpass.PassengerQueue.Dequeue();

            // Sırada bekleyen yeni bir grup varsa, onu animasyonla başlangıç noktasına taşı
            if (underpass.PassengerQueue.Count > 0)
            {
                PassengerGroup nextGroup = underpass.PassengerQueue.Peek();
                Vector3 targetPos = gridManager.GetWorldPosition(underpass.StartCell);

                // Animasyon tamamlandığında grubu tıklanabilir yap
                nextGroup.transform.DOMove(targetPos, 0.5f)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() => 
                    {
                        nextGroup.GetComponent<Collider>().enabled = true;
                    });
            }
            
            UpdateQueueCounter(underpass);
        }
    }

    private void UpdateQueueCounter(Underpass underpass)
    {
        if (underpass.QueueCounterText != null)
        {
            // Aktif olan hariç, kuyrukta bekleyenlerin sayısı
            int waitingCount = underpass.PassengerQueue.Count > 0 ? underpass.PassengerQueue.Count - 1 : 0;
            underpass.QueueCounterText.text = waitingCount.ToString();
        }
    }
}
