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

    public static event System.Action<bool> OnTrainAdjustmentStateChanged;

    // Tüm vagonların hareketini kontrol etmek için statik değişken
    public static bool IsMovementStopped { get; private set; }
    private bool isAdjusting = false; // Tren pozisyon ayarlaması yaparken true olur.
    // Pending removals collection: collect removed wagon transforms arriving within a short window
    private List<Transform> pendingRemovedTransforms = new List<Transform>();
    private Coroutine pendingRemovalCoroutine = null;
    private float pendingRemovalDelay = 0.05f; // small aggregation window

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

        // --- 1. Spawn all wagons and add to a temporary list of GameObjects ---
        List<GameObject> wagonObjects = new List<GameObject>();
        Vector3 basePos = checkpointPath.checkpoints[0].position;
        Vector3 forward = (checkpointPath.checkpoints.Count > 1) ?
            (checkpointPath.checkpoints[1].position - checkpointPath.checkpoints[0].position).normalized : Vector3.forward;

        // HEAD
        wagonObjects.Add(Instantiate(headPrefab, basePos, Quaternion.LookRotation(forward)));

        // MID
        for (int i = 0; i < midCount; i++)
        {
            Vector3 spawnPos = basePos - forward * wagonSpacing * (i + 1);
            wagonObjects.Add(Instantiate(midPrefab, spawnPos, Quaternion.LookRotation(forward)));
        }

        // TAIL
        Vector3 tailPos = basePos - forward * wagonSpacing * (midCount + 1);
        wagonObjects.Add(Instantiate(endPrefab, tailPos, Quaternion.LookRotation(forward)));

        // --- 2. Initialize, name, color, and register all wagons ---
        for (int i = 0; i < wagonObjects.Count; i++)
        {
            GameObject wagonObj = wagonObjects[i];
            MetroWagon wagon = wagonObj.GetComponent<MetroWagon>();

            if (wagon == null)
            {
                Debug.LogError($"Prefab for wagon at index {i} is missing MetroWagon script!");
                continue;
            }

            // Set head property for the first wagon
            if (i == 0)
            {
                wagon.isHead = true;
            }
            
            // Name the wagon
            wagonObj.name = $"Wagon_{i}";

            // Determine color
            HyperCasualColor colorToAssign = HyperCasualColor.White; // Default
            if (midWagonColors != null && midWagonColors.Count > 0)
            {
                int colorIndex = i % midWagonColors.Count;
                colorToAssign = midWagonColors[colorIndex];
            }

            // Initialize with path and color
            wagon.Init(checkpointPath, FindClosestCheckpointIndex(wagonObj.transform.position), colorToAssign);

            // Apply color to renderer
            var renderer = wagon.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = colorToAssign.ToColor();
            }
            
            // Register and add to the final list
            WagonManager.Instance.RegisterWagon(wagon);
            wagons.Add(wagon);
        }

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

    private void HandleWagonRemoval(MetroWagon removedWagon, Transform removedWagonTransform)
    {
        // --- Head Promotion ---
        if (removedWagon != null && removedWagon.isHead)
        {
            // The `wagons` list is the master list, ordered by spawn, and never changes.
            // We find the first wagon in our master list that is still active in the game.
            MetroWagon newHead = wagons.FirstOrDefault(w => w != null && w.gameObject.activeInHierarchy);
            if (newHead != null)
            {
                newHead.isHead = true;
                Debug.Log($"<color=#00FFFF>HEAD PROMOTION:</color> New head wagon is {newHead.name}.");
            }
        }
        // --- End Head Promotion ---

        // --- Original visual removal logic ---
        if (removedWagonTransform == null) return;

        Debug.LogWarning($"MetroManager: OnWagonRemoved enqueued for transform '{removedWagonTransform.name}' at pos {removedWagonTransform.position}");

        // Add to the queue
        pendingRemovedTransforms.Add(removedWagonTransform);

        // If a processing coroutine isn't already running, start one.
        if (pendingRemovalCoroutine == null)
        {
            pendingRemovalCoroutine = StartCoroutine(ProcessPendingRemovals());
        }
    }

    private System.Collections.IEnumerator ProcessPendingRemovals()
    {
        // If we are already adjusting, or there's nothing to process, exit.
        if (isAdjusting || pendingRemovedTransforms.Count == 0)
        {
            pendingRemovalCoroutine = null;
            yield break;
        }

        isAdjusting = true;
        OnTrainAdjustmentStateChanged?.Invoke(true);

        // Take only the FIRST wagon from the pending list.
        var removedTransform = pendingRemovedTransforms[0];
        pendingRemovedTransforms.RemoveAt(0);

        // Wait a frame to ensure all states are updated
        yield return null;

        // --- Logic for a SINGLE removal ---

        // Active wagons list
        wagons = WagonManager.Instance.GetActiveWagons();

        var items = new List<(Transform t, float progress)>();

        float GetPathProgress(Vector3 pos)
        {
            if (checkpointPath == null || checkpointPath.checkpoints == null || checkpointPath.checkpoints.Count < 2) return 0f;
            float bestDistSqr = float.MaxValue;
            float bestProgress = 0f;
            for (int si = 0; si < checkpointPath.checkpoints.Count - 1; si++)
            {
                Vector3 a = checkpointPath.checkpoints[si].position;
                Vector3 b = checkpointPath.checkpoints[si + 1].position;
                Vector3 ab = b - a;
                float len2 = ab.sqrMagnitude;
                float t = 0f;
                if (len2 > 0f) t = Mathf.Clamp01(Vector3.Dot(pos - a, ab) / len2);
                Vector3 proj = a + ab * t;
                float d2 = (pos - proj).sqrMagnitude;
                if (d2 < bestDistSqr)
                {
                    bestDistSqr = d2;
                    bestProgress = si + t;
                }
            }
            return bestProgress;
        }

        foreach (var w in wagons)
        {
            if (w == null) continue;
            float prog = 0f;
            int idx = w.GetCurrentCheckpointIndex();
            if (checkpointPath != null && checkpointPath.checkpoints != null && checkpointPath.checkpoints.Count > 1)
            {
                int seg = Mathf.Clamp(idx - 1, 0, checkpointPath.checkpoints.Count - 2);
                Vector3 a = checkpointPath.checkpoints[seg].position;
                Vector3 b = checkpointPath.checkpoints[seg + 1].position;
                Vector3 ab = b - a;
                float denom = ab.sqrMagnitude;
                float t = 0f;
                if (denom > 0f) t = Mathf.Clamp01(Vector3.Dot(w.transform.position - a, ab) / denom);
                prog = seg + t;
            }
            else
            {
                prog = GetPathProgress(w.transform.position);
            }
            items.Add((w.transform, prog));
        }
        items.Add((removedTransform, GetPathProgress(removedTransform.position)));

        items = items.OrderByDescending(i => i.progress).ToList();

        var rich = items.Select(i => (
            origTransform: i.t,
            pos: i.t != null ? i.t.position : Vector3.zero,
            rot: i.t != null ? i.t.rotation : Quaternion.identity,
            wagon: i.t != null ? i.t.GetComponent<MetroWagon>() : null
        )).ToList();

        var wagonTargetMap = new Dictionary<MetroWagon, (Vector3 pos, Quaternion rot)>();
        int removedIdx = rich.FindIndex(item => item.origTransform == removedTransform);

        if (removedIdx != -1)
        {
            for (int i = removedIdx - 1; i >= 0; i--)
            {
                var itemToMove = rich[i];
                var targetSlot = rich[i + 1];
                if (itemToMove.wagon != null)
                {
                    wagonTargetMap[itemToMove.wagon] = (targetSlot.pos, targetSlot.rot);
                }
            }
        }

        var movePairs = wagonTargetMap.Select(kv => (wagon: kv.Key, targetPos: kv.Value.pos, targetRot: kv.Value.rot)).ToList();

        if (movePairs.Count == 0)
        {
            isAdjusting = false; // No one to move, just continue processing.
            if (pendingRemovedTransforms.Count > 0)
            {
                pendingRemovalCoroutine = StartCoroutine(ProcessPendingRemovals());
            }
            else
            {
                pendingRemovalCoroutine = null;
                OnTrainAdjustmentStateChanged?.Invoke(false);
            }
            yield break;
        }

        float moveDuration = 0.5f;
        Sequence seq = DOTween.Sequence();

        foreach (var mp in movePairs)
        {
            if (mp.wagon == null) continue;
            seq.Join(mp.wagon.transform.DOMove(mp.targetPos, moveDuration).SetEase(Ease.InOutCubic));
            seq.Join(mp.wagon.transform.DORotateQuaternion(mp.targetRot, moveDuration).SetEase(Ease.InOutCubic));
        }

        seq.OnComplete(() => {
            Debug.Log($"Single wagon adjustment complete for: {removedTransform.name}");
            foreach (var mp in movePairs)
            {
                if (mp.wagon == null) continue;
                mp.wagon.SetTargetCheckpoint(FindNearestCheckpointIndexExact(mp.targetPos));
            }

            isAdjusting = false;
            // Check for more pending removals and restart the process.
            if (pendingRemovedTransforms.Count > 0)
            {
                Debug.Log($"More removals pending ({pendingRemovedTransforms.Count}), processing next.");
                pendingRemovalCoroutine = StartCoroutine(ProcessPendingRemovals());
            }
            else
            {
                Debug.Log("All wagon removals processed.");
                pendingRemovalCoroutine = null;
                OnTrainAdjustmentStateChanged?.Invoke(false);
            }
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

    /// <summary>
    /// Finds the index of the absolutely nearest checkpoint to a given position, without any offset.
    /// This is used for post-animation adjustments where the wagon needs to snap to the truly closest point.
    /// </summary>
    private int FindNearestCheckpointIndexExact(Vector3 position)
    {
        int closestIndex = 0;
        float minDistance = float.MaxValue;

        if (checkpointPath == null || checkpointPath.checkpoints == null) return 0;

        for (int i = 0; i < checkpointPath.checkpoints.Count; i++)
        {
            float dist = Vector3.Distance(position, checkpointPath.checkpoints[i].position);
            if (dist < minDistance)
            {
                minDistance = dist;
                closestIndex = i;
            }
        }
        return closestIndex;
    }

    public bool IsAdjusting()
    {
        return isAdjusting;
    }
}
