using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System.Linq;
using GridSystem;
using DG.Tweening;

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
    public PassengerGrid passengerGrid;

    private int currentLevelIndex = 0;
    private readonly List<MetroWagon> masterWagonList = new List<MetroWagon>();
    private List<MetroWagon> activeWagons = new List<MetroWagon>();
    
    private Dictionary<MetroWagon, float> originalWagonSpeeds = new Dictionary<MetroWagon, float>();
    private float currentSpeedMultiplier = 4.0f;

    public static MetroManager Instance { get; private set; }

    public static event System.Action<bool> OnTrainAdjustmentStateChanged;
    public static event System.Action<string> OnSpeedMultiplierChanged;

    public static bool IsMovementStopped { get; private set; }
    private bool isAdjusting = false; 
    private List<Transform> pendingRemovedTransforms = new List<Transform>();
    private Coroutine pendingRemovalCoroutine = null;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        WagonManager.Instance.OnWagonRemoved += HandleWagonRemoval;
    }

    void OnDestroy()
    {
        if (WagonManager.Instance != null)
        {
            WagonManager.Instance.OnWagonRemoved -= HandleWagonRemoval;
        }

        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.OnDataLoaded -= LoadData;
        }
    }

    public void LoadData(SaveGameData data)
    {
        if (data == null) return;
        currentLevelIndex = data.levelIndex;
    }

    public void SaveData(SaveGameData data)
    {
        if (data == null) return;
        data.levelIndex = this.currentLevelIndex;
    }

    public static void StopMovement()
    {
        if (IsMovementStopped) return;
        IsMovementStopped = true;
        Debug.Log("<color=red>TRAIN MOVEMENT STOPPED.</color>");
    }

    public static void StartMovement()
    {
        if (!IsMovementStopped) return;
        IsMovementStopped = false;
        Debug.Log("<color=green>TRAIN MOVEMENT RESTARTED.</color>");
    }

    public void SetSpeedMultiplier(float newMultiplier)
    {
        if (Mathf.Approximately(currentSpeedMultiplier, newMultiplier)) return;

        currentSpeedMultiplier = newMultiplier;
        ApplySpeedMultiplier();
        OnSpeedMultiplierChanged?.Invoke($"{currentSpeedMultiplier}x");
        Debug.Log($"[MetroManager] Speed explicitly set to {currentSpeedMultiplier}x.");
    }

    public void ToggleSpeed()
    {
        float newSpeed;
        if (currentSpeedMultiplier == 1.0f)
        {
            newSpeed = 2.0f;
        }
        else
        {
            newSpeed = 1.0f;
        }
        SetSpeedMultiplier(newSpeed);
    }

    private void ApplySpeedMultiplier()
    {
        foreach (var wagon in masterWagonList)
        {
            if (wagon != null && originalWagonSpeeds.ContainsKey(wagon))
            {
                wagon.speed = originalWagonSpeeds[wagon] * currentSpeedMultiplier;
            }
        }
        Debug.Log($"MetroManager: Wagon speeds set to {currentSpeedMultiplier}x.");
    }

    void Start()
    {
        if (checkpointPath == null || checkpointPath.checkpoints == null || checkpointPath.checkpoints.Count == 0)
        {
            Debug.LogError("Checkpoint path atanmadı veya boş!");
            return;
        }
        if (passengerGrid == null)
        {
            Debug.LogError("PassengerGrid referansı MetroManager'a atanmadı!");
            return;
        }

        IsMovementStopped = false;
        masterWagonList.Clear();
        activeWagons.Clear();
        StartCoroutine(InitializeWagonsAfterDelay());
    }

    private IEnumerator InitializeWagonsAfterDelay()
    {
        yield return null;

        if (checkpointPath == null)
        {
            Debug.LogError("FATAL HATA: MetroManager'daki 'Checkpoint Path' alanı boş (None). Lütfen sahnedeki MetroManager objesine bir yol (MetroCheckpointPath) atayın.");
            yield break;
        }
        if (checkpointPath.checkpoints.Any(c => c == null))
        {
            Debug.LogError($"FATAL HATA: MetroManager'a atanan '{checkpointPath.name}' adlı yolun 'Checkpoints' listesinde boş (None) veya yok edilmiş elemanlar var. Lütfen '{checkpointPath.name}' objesini seçip listeyi kontrol edin ve boş elemanları silin veya düzeltin.");
            yield break;
        }

        var wagonsFromManager = WagonManager.Instance.GetActiveWagons();
        masterWagonList.AddRange(wagonsFromManager);
        activeWagons.AddRange(wagonsFromManager);

        if (activeWagons.Count == 0)
        {
            Debug.LogWarning("MetroManager, WagonManager'dan hiç vagon alamadı. LevelSpawner'ın çalıştığından emin olun.");
            yield break;
        }

        Debug.Log($"MetroManager, {activeWagons.Count} adet vagonu WagonManager'dan aldı.");
        originalWagonSpeeds.Clear();

        for (int i = 0; i < activeWagons.Count; i++)
        {
            MetroWagon wagon = activeWagons[i];
            if (wagon == null) continue;

            if (i == 0) wagon.isHead = true;

            wagon.Init(checkpointPath, FindClosestCheckpointIndex(wagon.transform.position), wagon.wagonColor);
            originalWagonSpeeds[wagon] = wagon.speed;
        }

        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.OnDataLoaded += LoadData;
            LoadData(GameDataManager.Instance.GetSaveData());
        }

        ApplySpeedMultiplier();
        OnSpeedMultiplierChanged?.Invoke($"{currentSpeedMultiplier}x");
    }

    private void HandleWagonRemoval(MetroWagon removedWagon, Transform removedWagonTransform)
    {
        if (removedWagon != null && removedWagon.isHead)
        {
            MetroWagon newHead = masterWagonList.FirstOrDefault(w => w != null && w.gameObject.activeInHierarchy && w != removedWagon);
            if (newHead != null)
            {
                newHead.isHead = true;
                Debug.LogWarning($"HEAD DEĞİŞTİ! Yeni head vagon: {newHead.name}");
                StartMovement();
            }
            else
            {
                Debug.LogError("Last wagon removed! Train cannot move.");
                StopMovement();
            }
        }

        if (removedWagonTransform == null) return;

        Debug.LogWarning($"MetroManager: OnWagonRemoved enqueued for transform '{removedWagonTransform.name}' at pos {removedWagonTransform.position}");
        pendingRemovedTransforms.Add(removedWagonTransform);

        if (pendingRemovalCoroutine == null)
        {
            pendingRemovalCoroutine = StartCoroutine(ProcessPendingRemovals());
        }
    }

    private System.Collections.IEnumerator ProcessPendingRemovals()
    {
        if (isAdjusting || pendingRemovedTransforms.Count == 0)
        {
            pendingRemovalCoroutine = null;
            yield break;
        }

        isAdjusting = true;
        OnTrainAdjustmentStateChanged?.Invoke(true);

        var removedTransform = pendingRemovedTransforms[0];
        pendingRemovedTransforms.RemoveAt(0);

        yield return null;

        activeWagons = WagonManager.Instance.GetActiveWagons();

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

        foreach (var w in activeWagons)
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
            isAdjusting = false;
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

        return Mathf.Min(closestIndex + 1, checkpointPath.checkpoints.Count - 1);
    }

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

    public void ShuffleWagonColors()
    {
        if (isAdjusting) 
        {
            Debug.LogWarning("[MetroManager] Cannot shuffle colors while another adjustment is in progress.");
            return;
        }
        
        StartCoroutine(ShuffleWagonColorsCoroutine());
    }

    private System.Collections.IEnumerator ShuffleWagonColorsCoroutine()
    {
        isAdjusting = true;
        OnTrainAdjustmentStateChanged?.Invoke(true);

        List<MetroWagon> wagonsToShuffle = masterWagonList.Where(w => w != null && w.gameObject.activeInHierarchy).ToList();

        if (wagonsToShuffle.Count < 2) 
        {
            Debug.LogWarning("[MetroManager] Not enough active wagons to shuffle.");
            isAdjusting = false;
            OnTrainAdjustmentStateChanged?.Invoke(false);
            yield break;
        }

        List<HyperCasualColor> originalColors = wagonsToShuffle.Select(w => w.wagonColor).ToList();

        foreach (var wagon in wagonsToShuffle)
        {
            wagon.SetColor(HyperCasualColor.Grey);
        }

        yield return new WaitForSeconds(0.75f);

        List<HyperCasualColor> newColors = WagonManager.ShuffleColorGroups(originalColors);

        for (int i = 0; i < wagonsToShuffle.Count; i++)
        {
            if (i < newColors.Count)
            {
                wagonsToShuffle[i].SetColor(newColors[i]);
                yield return new WaitForSeconds(0.05f); 
            }
        }

        isAdjusting = false;
        OnTrainAdjustmentStateChanged?.Invoke(false);
        Debug.Log("[MetroManager] Wagon colors have been shuffled.");
    }
}