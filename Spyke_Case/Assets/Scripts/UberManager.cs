using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// Yolunu tamamlayan vagonları, sıralı bir havuz sistemiyle yönetir.
/// Her vagon için bir Uber gönderir ve Uber'ler arasında döngüsel bir animasyon mantığı uygular.
/// </summary>
public class UberManager : MonoBehaviour
{
    public static UberManager Instance { get; private set; }

    [Header("Uber Pool Settings")]
    [Tooltip("Sahneye spawn edilecek Uber arabası prefabı.")]
    public GameObject uberPrefab;
    [Tooltip("Havuzdaki toplam Uber sayısı. Bu sistem 3 için tasarlanmıştır.")]
    public int poolSize = 3;
    [Tooltip("Uber'lerin oyun başında duracağı park noktaları (Sırayla atanmalı: 1, 2, 3).")]
    public List<Transform> parkingSpots;
    [Tooltip("Sıradaki Uber'in gelip bekleyeceği nokta.")]
    public Transform waitingPoint;

    [Header("Gameplay")]
    [Tooltip("Bu sayıya ulaşıldığında oyun biter.")]
    public int maxUberCount = 10;
    public int UberCount { get; private set; } = 0;

    [Header("Animation Settings")]
    [SerializeField] private float targetZOffset = 10f;
    [SerializeField] private float animationDuration = 2.5f;

    private Queue<MetroWagon> wagonQueue = new Queue<MetroWagon>();
    private LinkedList<GameObject> uberPool = new LinkedList<GameObject>();
    private bool isSequenceRunning = false;

    public static event Action<int> OnUberCountChanged;
    public static event Action OnGameOver;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (uberPrefab == null || waitingPoint == null || parkingSpots.Count < poolSize)
        {
            Debug.LogError("UberManager is not configured correctly! Assign Uber Prefab, Waiting Point, and all Parking Spots.");
            this.enabled = false;
            return;
        }

        // Uber havuzunu oluştur ve park noktalarına yerleştir.
        for (int i = 0; i < poolSize; i++)
        {
            GameObject uber = Instantiate(uberPrefab, parkingSpots[i].position, parkingSpots[i].rotation, this.transform);
            uberPool.AddLast(uber);
        }

        // Başlangıç durumu: 1. ve 2. aktif, 3. pasif.
        var first = uberPool.First;
        var second = first.Next;
        var third = second.Next;
        
        first.Value.SetActive(true);
        second.Value.SetActive(true);
        third.Value.SetActive(false);

        // İlk Uber'i bekleme noktasına taşı.
        first.Value.transform.DOMove(waitingPoint.position, 1.5f).SetEase(Ease.OutQuad);
    }

    public void ProcessFinishedWagon(MetroWagon wagon)
    {
        if (wagon == null || wagonQueue.Contains(wagon)) return;

        Debug.Log($"<color=magenta>UBER:</color> Wagon '{wagon.name}' requested an Uber and is now in queue.");
        wagonQueue.Enqueue(wagon);

        if (!isSequenceRunning)
        {
            StartCoroutine(ProcessUberSequence());
        }
    }

    private IEnumerator ProcessUberSequence()
    {
        isSequenceRunning = true;

        while (wagonQueue.Count > 0)
        {
            MetroWagon wagonToCollect = wagonQueue.Dequeue();
            if (wagonToCollect == null) continue;

            // Pool'dan Uber'leri al
            GameObject uber1_mission = uberPool.First.Value;
            GameObject uber2_waiting = uberPool.First.Next.Value;

            // Adım 1: Trenin kendini ayarlaması için vagonun kaldırıldığını bildir.
            if (WagonManager.Instance != null)
            {
                WagonManager.Instance.DeregisterWagon(wagonToCollect);
                WagonManager.Instance.TriggerWagonRemovalEvent(wagonToCollect, wagonToCollect.transform);
            }

            // Adım 2: Vagonu deaktif et, Uber'i onun yerine koy.
            Vector3 startPos = wagonToCollect.transform.position;
            wagonToCollect.gameObject.SetActive(false);
            uber1_mission.transform.position = startPos;

            // Adım 3: Senkronize animasyonları başlat.
            Sequence sequence = DOTween.Sequence();
            Vector3 targetPos1 = new Vector3(startPos.x, startPos.y, startPos.z + targetZOffset);

            sequence.Append(uber1_mission.transform.DOMove(targetPos1, animationDuration).SetEase(Ease.InQuad));
            sequence.Join(uber2_waiting.transform.DOMove(waitingPoint.position, animationDuration).SetEase(Ease.InOutSine));

            // Animasyonun bitmesini bekle
            yield return sequence.WaitForCompletion();

            // Adım 4: Sırayı güncelle ve durumu ayarla
            uber1_mission.SetActive(false); // Görevdeki Uber'i pasif yap
            uberPool.RemoveFirst(); // Görevdekini sıranın başından kaldır
            uberPool.AddLast(uber1_mission); // Sıranın en sonuna ekle

            // Yeni 3. sıradaki (az önce sona eklenen) Uber'in pozisyonunu park noktasına ayarla
            uber1_mission.transform.position = parkingSpots[2].position;

            // Yeni 2. sıradaki Uber'i aktif et
            uberPool.First.Next.Value.SetActive(true);

            // Gameplay sayacını artır ve kontrol et
            UberCount++;
            OnUberCountChanged?.Invoke(UberCount);
            Debug.Log($"<color=magenta>UBER:</color> Mission complete. Total count: {UberCount}");

            if (UberCount >= maxUberCount)
            {
                OnGameOver?.Invoke();
                Debug.LogError("GAME OVER: Max Uber count reached!");
                // Burada oyun bitirme mantığı eklenebilir.
            }
        }

        isSequenceRunning = false;
    }
}
