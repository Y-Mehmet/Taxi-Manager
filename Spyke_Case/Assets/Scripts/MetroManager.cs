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
    // Name wagons sequentially: head = Wagon_0
    headObj.name = $"Wagon_0";
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
            // name sequentially: Wagon_1 .. Wagon_midCount
            int midIndex = 1 + i; // head=0
            midObj.name = $"Wagon_{midIndex}";
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
    // name tail sequentially
    int tailIndex = 1 + midCount; // head=0
    tailObj.name = $"Wagon_{tailIndex}";
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
    /// A wagon was removed (disabled). Aggregate multiple removals and then reassign positions
    /// for the wagons that were ahead of the removed wagons. This method avoids relying on world Z
    /// and instead orders wagons by their checkpoint progress (and fallback along the path).
    /// Each preceding wagon will be animated to the position of the wagon behind it (or to the
    /// removed wagon's transform position placeholder) using DOTween.
    /// </summary>
    private void HandleWagonRemoval(Transform removedWagonTransform)
    {
        if (removedWagonTransform == null) return;

        Debug.LogWarning($"MetroManager: OnWagonRemoved enqueued for transform '{removedWagonTransform.name}' at pos {removedWagonTransform.position}");

        // collect removed transforms within a short time window
        pendingRemovedTransforms.Add(removedWagonTransform);

        if (pendingRemovalCoroutine == null)
        {
            pendingRemovalCoroutine = StartCoroutine(ProcessPendingRemovals());
        }
    }

    private System.Collections.IEnumerator ProcessPendingRemovals()
    {
        // Small delay so simultaneous removals are aggregated
        yield return new WaitForSeconds(pendingRemovalDelay);

        if (isAdjusting)
        {
            pendingRemovedTransforms.Clear();
            pendingRemovalCoroutine = null;
            yield break;
        }

        isAdjusting = true;

        // Copy and clear pending list
        var removedList = new List<Transform>(pendingRemovedTransforms);
        pendingRemovedTransforms.Clear();
        pendingRemovalCoroutine = null;

        // Active wagons list
        wagons = WagonManager.Instance.GetActiveWagons();

        // Build a combined ordered list of items along the path: active wagons and placeholders for removed ones
        // We'll compute a continuous 'progress' along the path (segmentIndex + t) and sort by that.
        var items = new List<(Transform t, float progress)>();

        // Helper: compute continuous progress along checkpoint path
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

        // Add all active wagons with their path progress based on their checkpoint index + fraction
        foreach (var w in wagons)
        {
            if (w == null) continue;
            float prog = 0f;
            int idx = w.GetCurrentCheckpointIndex();
            // compute fraction between checkpoint idx-1 and idx using positions if possible
            if (checkpointPath != null && checkpointPath.checkpoints != null && checkpointPath.checkpoints.Count > 1)
            {
                int seg = Mathf.Clamp(idx - 1, 0, checkpointPath.checkpoints.Count - 2);
                Vector3 a = checkpointPath.checkpoints[seg].position;
                Vector3 b = checkpointPath.checkpoints[seg + 1].position;
                Vector3 ab = b - a;
                float denom = ab.sqrMagnitude;
                float t = 0f;
                if (denom > 0f)
                {
                    t = Mathf.Clamp01(Vector3.Dot(w.transform.position - a, ab) / denom);
                }
                prog = seg + t;
            }
            else
            {
                prog = GetPathProgress(w.transform.position);
            }
            items.Add((w.transform, prog));
        }

        // Add placeholders for removed wagons using their path progress
        foreach (var r in removedList)
        {
            if (r == null) continue;
            float prog = GetPathProgress(r.position);
            items.Add((r, prog));
        }


        // Sort items by continuous path progress descending (head first)
        items = items.OrderByDescending(i => i.progress).ToList();

        // Build richer item model (named tuple) so placeholders don't rely on transform state
        var rich = items.Select(i => (
            origTransform: i.t,
            pos: i.t != null ? i.t.position : Vector3.zero,
            rot: i.t != null ? i.t.rotation : Quaternion.identity,
            progress: i.progress,
            wagon: i.t != null ? i.t.GetComponent<MetroWagon>() : null
        )).ToList();

        var removedSet = new HashSet<Transform>(removedList);

    // Map wagons to their final target slot (pos, rot)
    var wagonTargetMap = new Dictionary<MetroWagon, (Vector3 pos, Quaternion rot)>();
    // For reporting, a batch-affected list (we'll list only wagons that actually change slot)
    var affectedWagons = new List<(MetroWagon wagon, float srcProg, Vector3 srcPos, Vector3 targetPos, Quaternion targetRot)>();
    // Map removed transform to final head slot (we'll apply at the end)
    var removedFinalPositions = new Dictionary<Transform, (Vector3 pos, Quaternion rot)>();

    // --- Batch rotate: compute final order once for all removals to avoid cascading effects ---
    // Original 'rich' is head-first (index 0 = head). We'll compute finalRich by:
    // 1) taking the original rich list
    // 2) removing all removed entries
    // 3) prepending the removed transforms (in the same order as removedList) into the head area using the original head-slot positions
    // This produces a single final ordering equivalent to performing all rotates at once.

    // Snapshot original head-slot positions (for assigning removed wagons)
    int removeCount = removedList.Count;
    var headSlotPositions = new List<(Vector3 pos, Quaternion rot)>();
    for (int i = 0; i < Mathf.Min(removeCount, rich.Count); i++) headSlotPositions.Add((rich[i].pos, rich[i].rot));

    // Build finalRich: first add placeholders for removed transforms (they will occupy the original head slots)
    var finalRich = new List<(Transform origTransform, Vector3 pos, Quaternion rot, float progress, MetroWagon wagon)>();
    for (int i = 0; i < removedList.Count; i++)
    {
        var rtr = removedList[i];
        Vector3 p = Vector3.zero; Quaternion q = Quaternion.identity;
        if (i < headSlotPositions.Count) { p = headSlotPositions[i].pos; q = headSlotPositions[i].rot; }
        finalRich.Add((rtr, p, q, 0f, rtr != null ? rtr.GetComponent<MetroWagon>() : null));
        removedFinalPositions[rtr] = (p, q);
    }

    // Then add the non-removed original entries in the same relative order
    for (int i = 0; i < rich.Count; i++)
    {
        var entry = rich[i];
        if (entry.origTransform != null && removedSet.Contains(entry.origTransform)) continue; // skip removed
        finalRich.Add(entry);
    }

    // Now compute mapping: for each wagon that remains (non-removed), compare its original index vs new index
    for (int origIdx = 0; origIdx < rich.Count; origIdx++)
    {
        var entry = rich[origIdx];
        if (entry.wagon == null) continue;
        // find new index of this wagon in finalRich
        int newIdx = finalRich.FindIndex(f => f.origTransform == entry.origTransform);
        if (newIdx == -1) continue; // shouldn't happen
        if (newIdx != origIdx)
        {
            var target = finalRich[newIdx];
            wagonTargetMap[entry.wagon] = (target.pos, target.rot);
            affectedWagons.Add((entry.wagon, entry.progress, entry.pos, target.pos, target.rot));
        }
    }

        // Prepare final movePairs from wagonTargetMap
        var movePairs = wagonTargetMap.Select(kv => (wagon: kv.Key, targetPos: kv.Value.pos, targetRot: kv.Value.rot)).ToList();

        // --- Concise batch report: removed wagons final head positions + the set of wagons that actually change slot ---
        Debug.LogWarning("--- Removal report (concise) start ---");
        foreach (var kv in removedFinalPositions)
        {
            var removedTr = kv.Key;
            var removedPos = kv.Value.pos; // final head slot pos
            MetroWagon removedWagonComp = removedTr != null ? removedTr.GetComponent<MetroWagon>() : null;
            int removedCheckpoint = -1;
            if (removedWagonComp != null) removedCheckpoint = removedWagonComp.GetCurrentCheckpointIndex();
            else removedCheckpoint = FindClosestCheckpointIndex(removedTr != null ? removedTr.position : removedPos);

            Debug.LogWarning($"Removed wagon: '{(removedTr != null ? removedTr.name : "null")}' checkpoint={removedCheckpoint} finalHeadPos={removedPos}");
        }

        Debug.LogWarning($"Affected wagons count (will move): {affectedWagons.Count}");
        foreach (var a in affectedWagons)
        {
            Debug.LogWarning($"  -> '{a.wagon.name}' from prog={a.srcProg:F3} pos={a.srcPos} -> targetPos={a.targetPos}");
        }
        Debug.LogWarning("--- Removal report (concise) end ---");

        // Condensed final slot ordering: only show first N slots and a total count to avoid huge logs
        int maxShownSlots = 10;
        Debug.LogWarning($"--- Final slot ordering (Head=0) — showing first {maxShownSlots} of {rich.Count} slots ---");
        for (int s = 0; s < Mathf.Min(maxShownSlots, rich.Count); s++)
        {
            var slot = rich[s];
            string name = slot.origTransform != null ? slot.origTransform.name : (slot.wagon != null ? slot.wagon.name : "(empty)");
            Debug.LogWarning($"Slot {s} -> '{name}' pos={slot.pos} prog={slot.progress:F3}");
        }
        if (rich.Count > maxShownSlots) Debug.LogWarning($"... (+{rich.Count - maxShownSlots} more slots not printed)");
        Debug.LogWarning("--- End slot ordering (condensed) ---");

        if (movePairs.Count == 0)
        {
            isAdjusting = false;
            yield break;
        }

        float moveDuration = 0.7f;
        Sequence seq = DOTween.Sequence();

        foreach (var mp in movePairs)
        {
            if (mp.wagon == null) continue;
            var tr = mp.wagon.transform;
            seq.Join(tr.DOMove(mp.targetPos, moveDuration).SetEase(Ease.InOutCubic));
            seq.Join(tr.DORotateQuaternion(mp.targetRot, moveDuration).SetEase(Ease.InOutCubic));
        }

        seq.OnComplete(() => {
            Debug.Log($"Tren yeniden düzenlendi ({movePairs.Count} vagon hareket etti).");
            // After animation, update each wagon's checkpoint to nearest checkpoint of its target position
            foreach (var mp in movePairs)
            {
                if (mp.wagon == null) continue;
                int newIdx = FindClosestCheckpointIndex(mp.targetPos);
                mp.wagon.SetTargetCheckpoint(newIdx);
                Debug.LogWarning($"MetroManager: Post-move set '{mp.wagon.name}' checkpoint -> {newIdx}");
            }
            // Apply final positions to removed (disabled) wagon transforms
            foreach (var kv in removedFinalPositions)
            {
                var tr = kv.Key;
                var p = kv.Value.pos;
                var r = kv.Value.rot;
                if (tr != null)
                {
                    tr.position = p;
                    tr.rotation = r;
                    Debug.LogWarning($"MetroManager: Applied final head-slot pos to removed '{tr.name}' -> {p}");
                }
            }
            isAdjusting = false;
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