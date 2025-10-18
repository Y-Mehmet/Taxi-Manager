using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using GridSystem; // PassengerGrid için eklendi
using DG.Tweening; // DOTween için eklendi

public class MetroManager : MonoBehaviour
{
    [Header("Mid vagon renkleri (sıralı)")]
    public List<HyperCasualColor> midWagonColors = new List<HyperCasualColor> { HyperCasualColor.Blue, HyperCasualColor.Red, HyperCasualColor.Green, HyperCasualColor.Yellow, HyperCasualColor.Orange, HyperCasualColor.Purple, HyperCasualColor.Pink, HyperCasualColor.Cyan, HyperCasualColor.Lime, HyperCasualColor.White };
    [Header("Prefablar")]
    public GameObject headPrefab;
    public GameObject midPrefab;
    public GameObject endPrefab;

    [Header("Vagon ayarları")]
    public int midCount = 10;
    [Tooltip("Vagonlar arası mesafe (birim)")]
    public float wagonSpacing = 1.5f;
    public MetroCheckpointPath checkpointPath;
    [Header("Bağlantılar")]
    public PassengerGrid passengerGrid; // Yolcu grid'i referansı

    private List<MetroWagon> wagons = new List<MetroWagon>();
    private Dictionary<MetroWagon, float> originalWagonSpeeds = new Dictionary<MetroWagon, float>();
    private bool speedsBoosted = false;
    private float initialSpeedMultiplier = 4f;

    public static MetroManager Instance { get; private set; }

    // Tüm vagonların hareketini kontrol etmek için statik değişken
    public static bool IsMovementStopped { get; private set; }
    private bool isAdjusting = false; // Tren pozisyon ayarlaması yaparken true olur.

    void Awake()
    {
        // Singleton pattern
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        WagonManager.Instance.OnWagonRemoved += HandleWagonRemoval;
        // Dinle: bir yolcu grubuna ilk tıklama gerçekleştiğinde hızları eski haline getir
        PassengerGroup.OnGroupClicked += HandleFirstGroupClicked;
    }

    public static void StopMovement()
    {
        IsMovementStopped = true;
    }

    void OnDestroy()
    {
        // Bellek sızıntılarını önlemek için event aboneliğini kaldır.
        if (WagonManager.Instance != null)
        {
            WagonManager.Instance.OnWagonRemoved -= HandleWagonRemoval;
        }
         PassengerGroup.OnGroupClicked -= HandleFirstGroupClicked;
    }

    void Start()
    {
        if (checkpointPath == null || checkpointPath.checkpoints == null || checkpointPath.checkpoints.Count == 0)
        {
            Debug.LogError("Checkpoint path atanmadı veya boş!");
            return;
        }
        if (headPrefab == null || midPrefab == null || endPrefab == null)
        {
            Debug.LogError("Prefab referansları atanmadı!");
            return;
        }
        if (passengerGrid == null)
        {
            Debug.LogError("PassengerGrid referansı MetroManager'a atanmadı!");
            return;
        }

        // Oyuna başlarken hareketi başlat
        IsMovementStopped = false;

        // HEAD vagonu en önde spawn et
        // Head vagonu en küçük z'de, tail en büyük z'de olacak şekilde spawn et
        Vector3 basePos = checkpointPath.checkpoints[0].position;
        Vector3 forward = (checkpointPath.checkpoints.Count > 1) ?
            (checkpointPath.checkpoints[1].position - checkpointPath.checkpoints[0].position).normalized : Vector3.forward;

        // Head vagonu
        GameObject headObj = Instantiate(headPrefab, basePos, Quaternion.LookRotation(forward));
        MetroWagon headWagon = headObj.GetComponent<MetroWagon>();
        if (headWagon == null)
        {
            Debug.LogError("Head prefabında MetroWagon scripti yok!");
            return;
        }
        // Head vagonu en yakın checkpoint'ten başlat
        headWagon.isHead = true; // Bu vagonun lider olduğunu belirt
        headWagon.Init(checkpointPath, FindClosestCheckpointIndex(headObj.transform.position));
        WagonManager.Instance.RegisterWagon(headWagon);
        wagons.Add(headWagon);

        for (int i = 0; i < midCount; i++) // Mid vagonlar
        {
            Vector3 spawnPos = basePos - forward * wagonSpacing * (i + 1);
            GameObject midObj = Instantiate(midPrefab, spawnPos, Quaternion.LookRotation(forward));
            MetroWagon midWagon = midObj.GetComponent<MetroWagon>();
            if (midWagon == null)
            {
                Debug.LogError($"Mid prefabında MetroWagon scripti yok! Index: {i}");
                continue;
            }
            // Her vagonu kendi en yakın checkpoint'inden başlat
            midWagon.Init(checkpointPath, FindClosestCheckpointIndex(midObj.transform.position));

            // Renk ata
            if (midWagonColors != null && midWagonColors.Count > 0)
            {
                int colorIndex = i % midWagonColors.Count;
                HyperCasualColor color = midWagonColors[colorIndex];
                var renderer = midWagon.GetComponentInChildren<Renderer>();
                if (renderer != null) // Init metoduna rengi de gönder
                {
                    midWagon.Init(checkpointPath, FindClosestCheckpointIndex(midObj.transform.position), color);
                    renderer.material.color = color.ToColor();
                }
            }
            WagonManager.Instance.RegisterWagon(midWagon);
            wagons.Add(midWagon);
        }

        // Tail vagon
        Vector3 tailPos = basePos - forward * wagonSpacing * (midCount + 1);
        GameObject tailObj = Instantiate(endPrefab, tailPos, Quaternion.LookRotation(forward));
        MetroWagon tailWagon = tailObj.GetComponent<MetroWagon>();
        if (tailWagon == null)
        {
            Debug.LogError("End prefabında MetroWagon scripti yok!");
            return;
        }
        // Tail vagonu kendi en yakın checkpoint'inden başlat
        tailWagon.Init(checkpointPath, FindClosestCheckpointIndex(tailObj.transform.position));
        WagonManager.Instance.RegisterWagon(tailWagon);
        wagons.Add(tailWagon);

        // Oyuna başlarken tüm vagonların hızını çarpanla arttır
        ApplyInitialWagonSpeedMultiplier();
    }


    private void ApplyInitialWagonSpeedMultiplier()
    {
        if (speedsBoosted) return;
        foreach (var w in wagons)
        {
            if (w == null) continue;
            originalWagonSpeeds[w] = w.speed;
            w.speed *= initialSpeedMultiplier;
        }
        speedsBoosted = true;
        Debug.Log($"MetroManager: Applied initial wagon speed multiplier x{initialSpeedMultiplier} to {wagons.Count} wagons.");
    }

    private void HandleFirstGroupClicked()
    {
        if (!speedsBoosted) return;
        RestoreOriginalWagonSpeeds();
        // Sadece ilk tıklamada çalışsın
        PassengerGroup.OnGroupClicked -= HandleFirstGroupClicked;
    }

    private void RestoreOriginalWagonSpeeds()
    {
        foreach (var kv in originalWagonSpeeds)
        {
            var w = kv.Key;
            if (w != null)
            {
                w.speed = kv.Value;
            }
        }
        originalWagonSpeeds.Clear();
        speedsBoosted = false;
        Debug.Log("MetroManager: Restored original wagon speeds after first passenger group click.");
    }

    /// <summary>
    /// Bir vagon dolup sistemden kaldırıldığında tetiklenir.
    /// Öndeki vagonları kaydırarak boşluğu kapatır.
    /// </summary>
    private void HandleWagonRemoval(Transform removedWagonTransform)
    {
        if (isAdjusting) return;

        Debug.Log("Vagon kaldırma işlemi algılandı. Tren DOTween ile yeniden düzenleniyor...");
        isAdjusting = true;

        // 1. Aktif vagonları al ve trenin başından sonuna doğru (Z ekseninde artan şekilde) sırala.
        // Bu, Head vagonun listenin başında olmasını sağlar.
        wagons = WagonManager.Instance.GetActiveWagons();
        wagons = wagons.OrderBy(w => w.transform.position.z).ToList();

        // 2. Kaldırılan vagonun pozisyonundan daha önde (daha küçük Z) olan vagonları bul.
        var wagonsToMove = wagons.Where(w => w.transform.position.z < removedWagonTransform.position.z).ToList();

        if (wagonsToMove.Count == 0) {
            Debug.Log("Kaydırılacak ön vagon bulunamadı.");
            isAdjusting = false;
            return;
        }

        // 3. DOTween sekansı oluştur.
        Sequence sequence = DOTween.Sequence();
        float moveDuration = 0.7f; // Animasyon süresi

        // 4. Her vagonun, bir sonraki vagonun yerine geçmesini sağla.
        // Listenin sonundan başlayarak (kaldırılan vagona en yakın olan) başa doğru ilerle.
        for (int i = wagonsToMove.Count - 1; i >= 0; i--)
        {
            MetroWagon currentWagon = wagonsToMove[i];
            Transform targetTransform;

            // Eğer bu vagon, kaldırılan vagonun hemen önündekiyse, hedefi kaldırılan vagondur.
            if (i == wagonsToMove.Count - 1)
            {
                targetTransform = removedWagonTransform;
            }
            else // Değilse, hedefi listedeki bir sonraki (yani trenin arkasındaki) vagondur.
            {
                targetTransform = wagonsToMove[i + 1].transform;
            }

            // Animasyonları sekansa ekle. Join() ile hepsi aynı anda başlar.
            sequence.Join(currentWagon.transform.DOMove(targetTransform.position, moveDuration).SetEase(Ease.InOutCubic));
            sequence.Join(currentWagon.transform.DORotate(targetTransform.rotation.eulerAngles, moveDuration).SetEase(Ease.InOutCubic));
        }

        // 4. Sekans tamamlandığında yapılacaklar.
        sequence.OnComplete(() => {
            Debug.Log("Trenin düzenlenmesi tamamlandı. Normal hareket devam ediyor.");
            isAdjusting = false; // Ayarlama modunu bitir.
            // Animasyon sonrası vagonların checkpoint hedeflerini de güncelleyebiliriz.
            // Şimdilik bu adımı atlıyoruz, çünkü pozisyonları zaten doğru.
        });
    }

    // Verilen pozisyona en yakın checkpoint'in index'ini bulur.
    private int FindClosestCheckpointIndex(Vector3 position)
    {
        int closestIndex = 0;
        float minDistance = float.MaxValue;

        for (int i = 0; i < checkpointPath.checkpoints.Count; i++)
        {
            float dist = Vector3.Distance(position, checkpointPath.checkpoints[i].position);
            if (dist < minDistance)
            {
                minDistance = dist;
                closestIndex = i;
            }
        }

        // En yakın checkpoint'ten bir sonraki hedef olarak başla, eğer son checkpoint değilse.
        // Bu, vagonun geriye gitmesini engeller.
        return Mathf.Min(closestIndex + 1, checkpointPath.checkpoints.Count - 1);
    }

    public bool IsAdjusting()
    {
        return isAdjusting;
    }
}