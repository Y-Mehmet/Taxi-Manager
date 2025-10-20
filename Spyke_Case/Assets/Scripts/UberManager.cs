using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// Yolculuğunu tamamlayan vagonları "Uber" nesneleriyle toplar.
/// Bir nesne havuzu (object pool) kullanarak Uber prefab'larını yönetir.
/// </summary>
public class UberManager : MonoBehaviour
{
    public static UberManager Instance { get; private set; }

    [Header("Uber Settings")]
    [Tooltip("Sahneye spawn edilecek Uber arabası prefabı.")]
    public GameObject uberPrefab;
    [Tooltip("Başlangıçta oluşturulacak Uber nesnesi sayısı.")]
    public int poolSize = 3;

    [Header("Animation Settings")]
    [SerializeField] private float targetZOffset = 20f;
    [SerializeField] private float animationDuration = 2.5f;
    [SerializeField] private Ease animationEase = Ease.InQuad;

    private Queue<MetroWagon> wagonQueue = new Queue<MetroWagon>();
    private List<GameObject> uberPool = new List<GameObject>();
    private bool isProcessingQueue = false;

    public static event Action<bool> OnUberStateChanged;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // Uber nesne havuzunu oluştur
        if (uberPrefab == null)
        {
            Debug.LogError("Uber Prefab is not assigned in UberManager!");
            return;
        }

        for (int i = 0; i < poolSize; i++)
        {
            GameObject uber = Instantiate(uberPrefab, Vector3.zero, Quaternion.identity, this.transform);
            uber.SetActive(false);
            uberPool.Add(uber);
        }
    }

    public void ProcessFinishedWagon(MetroWagon wagon)
    {
        if (wagon == null || wagonQueue.Contains(wagon)) return;

        Debug.Log($"<color=magenta>UBER:</color> Wagon '{wagon.name}' requested an Uber and is now in queue.");
        wagonQueue.Enqueue(wagon);

        if (!isProcessingQueue)
        {
            StartCoroutine(ProcessUberQueue());
        }
    }

    private IEnumerator ProcessUberQueue()
    {
        isProcessingQueue = true;
        OnUberStateChanged?.Invoke(true);

        while (wagonQueue.Count > 0)
        {
            GameObject availableUber = GetAvailableUberFromPool();
            if (availableUber == null)
            {
                Debug.LogWarning("No Uber available in the pool. Waiting...");
                yield return new WaitForSeconds(1f); // Havuzda yer açılmasını bekle
                continue; // Döngünün başına dön ve tekrar kontrol et
            }

            MetroWagon wagonToCollect = wagonQueue.Dequeue();
            if (wagonToCollect == null) continue;

            // Adım 1: Trenin kendini ayarlaması için vagonun kaldırıldığını bildir.
            if (WagonManager.Instance != null)
            {
                WagonManager.Instance.DeregisterWagon(wagonToCollect);
                WagonManager.Instance.TriggerWagonRemovalEvent(wagonToCollect, wagonToCollect.transform);
            }

            // Adım 2: Vagonu anında deaktif et, Uber nesnesini onun yerine koy.
            Vector3 startPos = wagonToCollect.transform.position;
            Quaternion startRot = wagonToCollect.transform.rotation;
            wagonToCollect.gameObject.SetActive(false);

            availableUber.transform.SetPositionAndRotation(startPos, startRot);
            availableUber.SetActive(true);

            // Adım 3: Uber nesnesini anime et ve bitmesini bekle.
            bool animationComplete = false;
            Vector3 targetPosition = new Vector3(startPos.x, startPos.y, startPos.z + targetZOffset);
            
            availableUber.transform.DOMove(targetPosition, animationDuration)
                .SetEase(animationEase)
                .OnComplete(() => {
                    availableUber.SetActive(false); // Uber'i havuza geri gönder
                    animationComplete = true;
                });

            yield return new WaitUntil(() => animationComplete);
        }

        isProcessingQueue = false;
        OnUberStateChanged?.Invoke(false);
    }

    private GameObject GetAvailableUberFromPool()
    {
        foreach (var uber in uberPool)
        {
            if (!uber.activeInHierarchy)
            {
                return uber;
            }
        }
        return null; // Hepsi meşgul
    }
}