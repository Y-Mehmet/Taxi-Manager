using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// Yolculuğunu tamamlayan vagonları, sıralı bir havuz sistemiyle yönetir.
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
    public int UberCount { get; private set; } = 1;

    [Header("Animation Settings")]
    [SerializeField] private float targetZOffset = 10f;
    [SerializeField] private float animationDuration = 2.5f;
    [SerializeField] private Ease animationEase = Ease.InQuad;

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
        SoundManager.instance.PlaySfx(SoundType.Slurp);
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
            // Kuyruktan bir sonraki vagonu al
            MetroWagon wagonToCollect = wagonQueue.Dequeue();
            if (wagonToCollect == null) continue;

            // Sayacı GÖREV BAŞINDA artır
            UberCount++;
            OnUberCountChanged?.Invoke(UberCount);
            Debug.Log($"<color=magenta>UBER:</color> Mission started. Total count: {UberCount}");

            bool isLastMission = UberCount >= maxUberCount;

            // Görevdeki Uber'i ve sıradakini (varsa) al
            GameObject uber1_mission = uberPool.First.Value;
            GameObject uber2_waiting = isLastMission ? null : uberPool.First.Next.Value;

            // Trenin kendini ayarlaması için vagonun kaldırıldığını bildir
            if (WagonManager.Instance != null)
            {
                WagonManager.Instance.DeregisterWagon(wagonToCollect);
                WagonManager.Instance.TriggerWagonRemovalEvent(wagonToCollect, wagonToCollect.transform);
            }

            // Vagonu deaktif et
            // wagonToCollect.gameObject.SetActive(false); // Replaced with animation

            // Animate the wagon moving to the Uber, then deactivate it.
            Transform wagonTransform = wagonToCollect.transform;
            Transform uberTransform = uber1_mission.transform;

            // Unparent the wagon so it can move freely in world space
            wagonTransform.SetParent(null); 

            Sequence collectSequence = DOTween.Sequence();
            collectSequence.Append(wagonTransform.DOMove(uberTransform.position, 0.5f).SetEase(Ease.InQuad));
            collectSequence.Join(wagonTransform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InQuad));
            collectSequence.OnComplete(() => {
                wagonToCollect.gameObject.SetActive(false);
            });

            // Wait for the animation to complete before continuing the Uber sequence
            yield return collectSequence.WaitForCompletion();

            // Animasyonları oluştur
            Sequence sequence = DOTween.Sequence();
            Vector3 uber1_startPos = uber1_mission.transform.position;
            Vector3 targetPos1 = new Vector3(uber1_startPos.x, uber1_startPos.y, uber1_startPos.z + targetZOffset);
            sequence.Append(uber1_mission.transform.DOMove(targetPos1, animationDuration).SetEase(animationEase));

            if (uber2_waiting != null)
            {
                // NORMAL GÖREV: Sıradaki Uber'i bekleme noktasına getir.
                sequence.Join(uber2_waiting.transform.DOMove(waitingPoint.position, animationDuration).SetEase(Ease.InOutSine));
            }
            else
            {
                // SON GÖREV: Diğer tüm Uber'leri deaktif et.
                if(uberPool.First.Next != null) uberPool.First.Next.Value.SetActive(false);
                if(uberPool.Last != null) uberPool.Last.Value.SetActive(false);
            }

            // Animasyonun bitmesini bekle
            yield return sequence.WaitForCompletion();

            // --- Animasyon Sonrası Mantık ---
            uber1_mission.SetActive(false); // Görevdeki Uber her zaman pasif olur

            if (isLastMission)
            {
                // SON GÖREV TAMAMLANDI: Oyunu bitir.
                OnGameOver?.Invoke();
                Debug.LogError("GAME OVER: Last Uber has completed its mission!");
                isSequenceRunning = false;
                yield break; // Coroutine'i tamamen sonlandır.
            }
            else
            {
                // NORMAL GÖREV: Uber havuzunu bir sonraki tura hazırla.
                uberPool.RemoveFirst();
                uberPool.AddLast(uber1_mission);
                uber1_mission.transform.position = parkingSpots[parkingSpots.Count - 1].position;
                uberPool.First.Next.Value.SetActive(true);
            }
        }

        isSequenceRunning = false;
    }
}
