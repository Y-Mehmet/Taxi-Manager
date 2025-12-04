using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// YolculuÄŸunu tamamlayan vagonlarÄ±, sÄ±ralÄ± bir havuz sistemiyle yÃ¶netir.
/// Her vagon iÃ§in bir Uber gÃ¶nderir ve Uber'ler arasÄ±nda dÃ¶ngÃ¼sel bir animasyon mantÄ±ÄŸÄ± uygular.
/// </summary>
public class UberManager : MonoBehaviour
{
    public static UberManager Instance { get; private set; }

    [Header("Uber Pool Settings")]
    [Tooltip("Sahneye spawn edilecek Uber arabasÄ± prefabÄ±.")]
    public GameObject uberPrefab;
    [Tooltip("Havuzdaki toplam Uber sayÄ±sÄ±. Bu sistem 3 iÃ§in tasarlanmÄ±ÅŸtÄ±r.")]
    public int poolSize = 3;
    [Tooltip("Uber'lerin oyun baÅŸÄ±nda duracaÄŸÄ± park noktalarÄ± (SÄ±rayla atanmalÄ±: 1, 2, 3).")]
    public List<Transform> parkingSpots;
    [Tooltip("SÄ±radaki Uber'in gelip bekleyeceÄŸi nokta.")]
    public Transform waitingPoint;

    [Header("Gameplay")]
    [Tooltip("Bu sayÄ±ya ulaÅŸÄ±ldÄ±ÄŸÄ±nda oyun biter.")]
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

        // Uber havuzunu oluÅŸtur ve park noktalarÄ±na yerleÅŸtir.
        for (int i = 0; i < poolSize; i++)
        {
            GameObject uber = Instantiate(uberPrefab, parkingSpots[i].position, parkingSpots[i].rotation, this.transform);
            uberPool.AddLast(uber);
        }

        // BaÅŸlangÄ±Ã§ durumu: 1. ve 2. aktif, 3. pasif.
        var first = uberPool.First;
        var second = first.Next;
        var third = second.Next;
        
        first.Value.SetActive(true);
        second.Value.SetActive(true);
        third.Value.SetActive(false);

        // Ä°lk Uber'i bekleme noktasÄ±na taÅŸÄ±.
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

            // SayacÄ± GÃ–REV BAÅINDA artÄ±r
            UberCount++;
            OnUberCountChanged?.Invoke(UberCount);
            
            // Notify invoice about Uber pickup
            if (GameManager.Instance != null && GameManager.Instance.CurrentInvoice != null)
            {
                GameManager.Instance.CurrentInvoice.OnUberPickup();
            }
            
            // Deduct from temp coins (penalty will be shown in invoice)
            if (GameEconomy.Instance != null)
            {
                GameEconomy.Instance.DeductTempCoins(100);
                
                // Show penalty animation
                if (CoinAnimationManager.Instance != null && waitingPoint != null)
                {
                    CoinAnimationManager.Instance.ShowSpendingFeedback(100, waitingPoint.position);
                }
            }
            
            Debug.Log($"<color=magenta>UBER:</color> Mission started. Total count: {UberCount}");

            bool isLastMission = UberCount >= maxUberCount;

            // GÃ¶revdeki Uber'i ve sÄ±radakini (varsa) al
            GameObject uber1_mission = uberPool.First.Value;
            GameObject uber2_waiting = isLastMission ? null : uberPool.First.Next.Value;

            // Trenin kendini ayarlamasÄ± iÃ§in vagonun kaldÄ±rÄ±ldÄ±ÄŸÄ±nÄ± bildir
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

            // AnimasyonlarÄ± oluÅŸtur
            Sequence sequence = DOTween.Sequence();
            Vector3 uber1_startPos = uber1_mission.transform.position;
            Vector3 targetPos1 = new Vector3(uber1_startPos.x, uber1_startPos.y, uber1_startPos.z + targetZOffset);
            sequence.Append(uber1_mission.transform.DOMove(targetPos1, animationDuration).SetEase(animationEase));

            if (uber2_waiting != null)
            {
                // NORMAL GÃ–REV: SÄ±radaki Uber'i bekleme noktasÄ±na getir.
                sequence.Join(uber2_waiting.transform.DOMove(waitingPoint.position, animationDuration).SetEase(Ease.InOutSine));
            }
            else
            {
                // SON GÃ–REV: DiÄŸer tÃ¼m Uber'leri deaktif et.
                if(uberPool.First.Next != null) uberPool.First.Next.Value.SetActive(false);
                if(uberPool.Last != null) uberPool.Last.Value.SetActive(false);
            }

            // Animasyonun bitmesini bekle
            yield return sequence.WaitForCompletion();

            // --- Animasyon SonrasÄ± MantÄ±k ---
            uber1_mission.SetActive(false); // GÃ¶revdeki Uber her zaman pasif olur

            if (isLastMission)
            {
                // SON GÃ–REV TAMAMLANDI: Oyunu bitir.
                OnGameOver?.Invoke();
                Debug.LogError("GAME OVER: Last Uber has completed its mission!");
                isSequenceRunning = false;
                yield break; // Coroutine'i tamamen sonlandÄ±r.
            }
            else
            {
                // NORMAL GÃ–REV: Uber havuzunu bir sonraki tura hazÄ±rla.
                uberPool.RemoveFirst();
                uberPool.AddLast(uber1_mission);
                uber1_mission.transform.position = parkingSpots[parkingSpots.Count - 1].position;
                uberPool.First.Next.Value.SetActive(true);
            }
        }

        isSequenceRunning = false;
    }
}
