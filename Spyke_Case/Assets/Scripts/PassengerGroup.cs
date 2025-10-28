using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using GridSystem;
using GridSystem.Data;
using DG.Tweening;
public class PassengerGroup : MonoBehaviour
{
    public static event System.Action<PassengerGroup> OnGroupDeparted;
    public static event System.Action OnGroupClicked;
    public event System.Action<int> OnGroupSizeDecreased;
    public event System.Action<int> OnAvailableSlotsChanged;
    public event System.Action<int> OnCapacityChanged;
    [Header("Slot Event/Capacity")]
    public int maxGroupSize = 4; 
    private int _groupSize = 4;
    private int _lastAvailableSlots = -1;
    public int GroupSize
    {
        get => _groupSize;
        set
        {
            if (_groupSize == value) return; // Prevent firing events if the value hasn't changed.

            int oldGroupSize = _groupSize;
            _groupSize = value;

            // This is the new, clear event for capacity changes.
            Debug.Log($"[PassengerGroup] {name} GroupSize changed from {oldGroupSize} to {_groupSize}. Invoking OnCapacityChanged.");
            OnCapacityChanged?.Invoke(_groupSize);

            // --- Keep old events for compatibility with other systems that might be using them ---
            // This event fires only on decrease.
            if (_groupSize < oldGroupSize)
            {
                OnGroupSizeDecreased?.Invoke(_groupSize);
            }

            // This event is for the 'filled slots' logic, which seems to be inverted.
            int oldSlots = Mathf.Max(0, maxGroupSize - oldGroupSize);
            int newSlots = AvailableSlots;
            if (newSlots != oldSlots && gameObject.activeInHierarchy)
            {
                OnAvailableSlotsChanged?.Invoke(newSlots);
            }
            _lastAvailableSlots = newSlots;
        }
    }
    public int AvailableSlots => Mathf.Max(0, maxGroupSize - GroupSize);
    public Vector2Int moveDirection = Vector2Int.up;
    public HyperCasualColor groupColor = HyperCasualColor.Yellow;
    public Vector3 originalPosition;
    [SerializeField]
     float moveSpeed = 7f; 
    public Transform modelTransform;
    public Vector2Int gridPos; 

    [Header("Initialization")]
    [Tooltip("If true, this object will be placed at `gridPos` on Start(). Leave false for manual placement.")]
    public bool useGridPosition = false;
    [Header("Yön Göstergesi")]
    public Transform directionIndicator;

    [Header("Convoy (train) settings")]
    public PassengerGroup followTarget = null;
    [Tooltip("Number of steps of delay behind the leader (1 = directly into leader's previous cell)")]
    public int followStepDelay = 1;

    private static List<PassengerGroup> allGroups = new List<PassengerGroup>();

    [Header("Rail mode")]
    public bool railMode = true;
    private static Dictionary<PassengerGroup, List<Vector2Int>> leaderRoutes = new Dictionary<PassengerGroup, List<Vector2Int>>();
    private int lastRailIndex = -1;
    public bool isRailHead = false;
    private static List<Vector2Int> globalRailRoute = new List<Vector2Int>();
    private Queue<Vector2Int> followQueue = new Queue<Vector2Int>();
    private bool processingFollowQueue = false;
    private Queue<int> checkpointQueue = new Queue<int>();
    private bool processingCheckpointQueue = false; // Renamed to avoid conflict
    private bool isMoving = false;
    private Tween activeMovementTween = null;
    private float activeTweenBaseSpeed = 1f;
    private enum MovementType { None, Path, Move, Jump }
    private MovementType activeMovementType = MovementType.None;
    private Vector3[] activeMovementPathPoints = null;
    private Vector3 activeMovementTarget = Vector3.zero; 

    
    private void OnEnable()
    {
        InputManager.OnPassengerGroupTapped += HandleTap;
    }

    private void OnDisable()
    {
        InputManager.OnPassengerGroupTapped -= HandleTap; // Renamed to avoid conflict

        if (PassengerGrid.Instance != null)
        {
            PassengerGrid.Instance.UnregisterOccupant(gridPos, this);
        }
    }

    public bool onConveyorBelt = false;
    void Start()
    {
        originalPosition = transform.position;
        OnAvailableSlotsChanged += (slots) => Debug.Log($"[PassengerGroup] {name} kalan slot: {slots}");
        _lastAvailableSlots = AvailableSlots;
        OnAvailableSlotsChanged?.Invoke(_lastAvailableSlots);

        SetGroupColor(groupColor);

        if (useGridPosition && PassengerGrid.Instance != null)
        {
            transform.position = PassengerGrid.Instance.GetWorldPosition(gridPos);
            PassengerGrid.Instance.RegisterOccupant(gridPos, this);
        }
        else if (!useGridPosition && PassengerGrid.Instance != null)
        {
            var gd = PassengerGrid.Instance.gridData;
            if (gd != null)
            {
                Vector3 relative = transform.position - PassengerGrid.Instance.transform.position - gd.worldOffset;
                int gx = Mathf.RoundToInt(relative.x / gd.cellSize);
                int gy = Mathf.RoundToInt(relative.z / gd.cellSize);
                gx = Mathf.Clamp(gx, 0, gd.width - 1);
                gy = Mathf.Clamp(gy, 0, gd.height - 1);
                Vector2Int inferred = new Vector2Int(gx, gy);
                gridPos = inferred;
                if (PassengerGrid.Instance.IsOccupied(gridPos))
                {
                    Debug.LogWarning($"Passenger '{name}' manual placement at {gridPos} conflicts with existing occupant.");
                }
                PassengerGrid.Instance.RegisterOccupant(gridPos, this);
            }
        }

        if (!allGroups.Contains(this)) allGroups.Add(this); // Renamed to avoid conflict
    }
    private void HandleTap(PassengerGroup tappedGroup)
    {
        if (tappedGroup != this) return;

        if (onConveyorBelt)
        {
            TryMoveToWaitingArea();
            return;
        }

        if (AbilityManager.Instance != null && AbilityManager.Instance.IsAbilityModeActive)
        {
            // The tap will be handled by the AbilityManager's subscriber. Do nothing here.
            Debug.Log($"[PassengerGroup] Tap on {name} is being handled by an active ability.");
            return;
        }

        // If the passenger is already at a stop, do not allow it to move again.
        if (StopManager.Instance != null && StopManager.Instance.GetOccupiedStops().ContainsValue(this))
        {
            Debug.Log($"[PassengerGroup] Tap on {name} ignored because it is already at a stop.");
            return;
        }

        Debug.Log($"[PassengerGroup] Tap detected on {name} via event, initiating normal move.");
        OnGroupClicked?.Invoke();
        TryMoveForwardWithLog();
    }

    void Update()
    {
        UpdateFollowQueue();
        UpdateCheckpointQueue();
    }

    public void TryMoveForwardWithLog()
    {
        if (isMoving)
        {
            Debug.LogWarning($"Yolcu '{name}' zaten hareket halinde olduğu için yeni hareket başlatılamadı.");
            return;
        }

        if (StopManager.Instance != null && !StopManager.Instance.HasAvailableStops())
        {
            Debug.LogWarning($"Tüm duraklar dolu veya rezerve edilmiş. '{name}' için hareket başlatılamadı.");
            return;
        }

        Vector2Int nextPos = gridPos + moveDirection;
        var cell = PassengerGrid.Instance.GetCell(nextPos.x, nextPos.y);
        if (cell == null || cell.cellType == GridCellType.Blocked || cell.cellType == GridCellType.Empty)
        {
            Debug.Log($"PassengerGroup hareket edemedi. Engel veya grid dışı: {nextPos}");
            StartCoroutine(BounceVisual());
            return;
        }

        Debug.LogWarning($"[MoveStart] Passenger '{name}' at {gridPos} moving {moveDirection}");

        List<Vector2Int> straightVec = new List<Vector2Int>();
        Vector2Int pathfindingStartPoint = gridPos;
        Vector2Int tempCursor = gridPos + moveDirection;
        while (PassengerGrid.Instance.GetCell(tempCursor.x, tempCursor.y) != null)
        {
            var currentCell = PassengerGrid.Instance.GetCell(tempCursor.x, tempCursor.y);
            if (currentCell.cellType == GridCellType.Blocked || currentCell.cellType == GridCellType.Empty) break;
            straightVec.Add(tempCursor);
            if (currentCell.cellType == GridCellType.Walkable || currentCell.cellType == GridCellType.Stop) { pathfindingStartPoint = tempCursor; break; }
            tempCursor += moveDirection;
        }

        AttemptPathfinding(pathfindingStartPoint, straightVec);
    }

    public void TryUniversalMove()
    {
        if (isMoving)
        {
            Debug.LogWarning($"[UniversalMove] Yolcu '{name}' zaten hareket halinde olduğu için yeni hareket başlatılamadı.");
            return;
        }

        if (StopManager.Instance == null || !StopManager.Instance.HasAvailableStops())
        {
            Debug.LogWarning($"[UniversalMove] Tüm duraklar dolu veya rezerve edilmiş. '{name}' için hareket başlatılamadı.");
            return;
        }

        Vector2Int pathfindingStartPoint = gridPos;
        Debug.LogWarning($"[UniversalMove] Passenger '{name}' at {gridPos} starting a 4-way search.");

        AttemptPathfinding(pathfindingStartPoint, new List<Vector2Int>());
    }

    private void AttemptPathfinding(Vector2Int pathfindingStartPoint, List<Vector2Int> initialPathSegment)
    {
        var reservation = StopManager.Instance.ReserveFirstFreeStop(this);
        if (reservation == null)
        {
            Debug.LogWarning($"[PathPlan] Could not reserve a stop for {name}. Pathfinding aborted.");
            StartCoroutine(BounceVisual());
            return;
        }

        var (stopWorldPos, stopIndex) = reservation.Value;

        List<Vector2Int> path = FindPathToHighestWalkableCell(pathfindingStartPoint);

        if (path == null)
        {
            Debug.LogWarning($"[PathPlan] No path to highest cell found. Trying fallback to nearest stop from {pathfindingStartPoint}.");
            path = PassengerGrid.Instance.FindNearestStopPath(pathfindingStartPoint);
        }

        if (path != null)
        {
            List<Vector2Int> fullPath = new List<Vector2Int>(initialPathSegment);
            fullPath.AddRange(path);
            Debug.LogWarning($"[PathPlan] Path found for {name}. Length: {fullPath.Count}. Starting movement.");
            OnGroupDeparted?.Invoke(this);
            StartCoroutine(ExecuteContinuousPath(fullPath, stopIndex, stopWorldPos));
        }
        else
        {
            Debug.LogError($"[PathPlan] FAILED. No path found for {name} from {pathfindingStartPoint} to any destination.");
            StopManager.Instance.CancelReservation(stopIndex, this);
            StartCoroutine(BounceVisual());
        }
    }

    private List<Vector2Int> FindPathToHighestWalkableCell(Vector2Int from)
    {
        int maxY = int.MinValue;
        List<Vector2Int> candidates = new List<Vector2Int>();
        for (int y = 0; y < PassengerGrid.Instance.gridData.height; y++)
        {
            for (int x = 0; x < PassengerGrid.Instance.gridData.width; x++)
            {
                var c = PassengerGrid.Instance.GetCell(x, y);
                if (c != null && c.cellType == GridCellType.Walkable)
                {
                    if (y > maxY) { maxY = y; candidates.Clear(); candidates.Add(new Vector2Int(x, y)); }
                    else if (y == maxY) { candidates.Add(new Vector2Int(x, y)); }
                }
            }
        }

        List<Vector2Int> bestPath = null;
        int bestLen = int.MaxValue;
        foreach (var cand in candidates)
        {
            var p = PassengerGrid.Instance.FindPathToTarget(from, cand, this, new List<GridCellType> { GridCellType.Walkable, GridCellType.Stop, GridCellType.WaitingArea });
            if (p != null && p.Count > 0 && p.Count < bestLen)
            {
                bestLen = p.Count;
                bestPath = p;
            }
        }
        return bestPath;
    }

   

    void OnDestroy()
    {
        if (allGroups.Contains(this)) allGroups.Remove(this);
    }

    public void SetGroupColor(HyperCasualColor color)
    {
        // Update the property so it holds the correct value
        this.groupColor = color;

        foreach (Transform child in transform)
        {
            if (directionIndicator != null && child == directionIndicator) continue;

            var renderer = child.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                // Use the newly set property to change the material color
                renderer.material.color = this.groupColor.ToColor();
            }
        }
    }

    void UpdateFollowQueue()
    {
        if (railMode && followTarget != null)
        {
            if (!leaderRoutes.ContainsKey(followTarget)) return;
            var route = leaderRoutes[followTarget];
            int targetIdx = route.Count - followStepDelay - 1;
            if (targetIdx > lastRailIndex && targetIdx >= 0)
            {
                Vector2Int targetPos = route[targetIdx];
                if (!isMoving) { StartCoroutine(MoveToCoroutine(targetPos)); lastRailIndex = targetIdx; }
            }
            return;
        }

        if (processingFollowQueue) return;
        if (followQueue.Count == 0) return;
        StartCoroutine(ProcessFollowQueue());
    }

    void UpdateCheckpointQueue()
    {
        if (processingCheckpointQueue) return;
        if (checkpointQueue.Count == 0) return;
        StartCoroutine(ProcessCheckpointQueue());
    }

    System.Collections.IEnumerator ProcessFollowQueue()
    {
        processingFollowQueue = true;
        while (followQueue.Count < Mathf.Max(1, followStepDelay)) yield return null;

        while (followQueue.Count > 0)
        {
            if (isMoving) { yield return null; continue; }
            Vector2Int target = followQueue.Dequeue();
            var cell = PassengerGrid.Instance.GetCell(target.x, target.y);
            if (cell == null) { continue; }
            yield return StartCoroutine(MoveToCoroutine(target));
        }
        processingFollowQueue = false;
    }

    System.Collections.IEnumerator ProcessCheckpointQueue()
    {
        processingCheckpointQueue = true;
        while (checkpointQueue.Count > 0)
        {
            if (isMoving) { yield return null; continue; }
            int stopIndex = checkpointQueue.Dequeue();
            if (PassengerGrid.Instance == null || PassengerGrid.Instance.gridData == null) continue;
            if (stopIndex < 0) continue;
            var stopWorldPos = StopManager.Instance.GetStopWorldPosition(stopIndex);
            var path = PassengerGrid.Instance.FindPathToTarget(gridPos, PassengerGrid.Instance.gridData.stopSlots[Mathf.Clamp(stopIndex,0,PassengerGrid.Instance.gridData.stopSlots.Count-1)], this);
            if (path != null && path.Count > 0)
            {
                StartCoroutine(ExecuteContinuousPath(path, stopIndex, stopWorldPos));
            }
            else
            {
                yield return new WaitForSeconds(0.15f);
            }
        }
        processingCheckpointQueue = false;
    }

    System.Collections.IEnumerator MoveToCoroutine(Vector2Int newGridPos)
    {
        if (PassengerGrid.Instance == null) yield break;
        var cell = PassengerGrid.Instance.GetCell(newGridPos.x, newGridPos.y);
        if (cell == null) yield break;
        if (!(cell.cellType == GridCellType.Walkable || cell.cellType == GridCellType.WaitingArea || cell.cellType == GridCellType.Stop)) yield break;

        Vector3 worldTarget = cell.cellTransform != null ? cell.cellTransform.position : PassengerGrid.Instance.GetWorldPosition(newGridPos);
        PassengerGrid.Instance.UnregisterOccupant(gridPos, this);
        PassengerGrid.Instance.RegisterOccupant(newGridPos, this);
        isMoving = true;
        yield return StartCoroutine(MoveToWorld(worldTarget, newGridPos));
        gridPos = newGridPos;
        var arrivedCell = PassengerGrid.Instance.GetCell(gridPos.x, gridPos.y);
        if (arrivedCell != null && arrivedCell.cellType == GridCellType.Stop && arrivedCell.stopIndex >= 0)
        {
            BroadcastCheckpointToFollowers(arrivedCell.stopIndex);
        }
        isMoving = false;
    }

    void BroadcastCheckpointToFollowers(int stopIndex)
    {
        foreach (var g in allGroups)
        {
            if (g != null && g.followTarget == this) g.checkpointQueue.Enqueue(stopIndex);
        }
    }

    System.Collections.IEnumerator ExecuteContinuousPath(List<Vector2Int> fullPath, int stopIndex, Vector3 stopWorldPos, int ascendIndex = -1)
    {
        isMoving = true;
        Vector2Int overallOrigin = gridPos;
        int pathIdx = 0;

        while (pathIdx < fullPath.Count)
        {
            int segmentEndIdx = -1;
            PassengerGroup obstacle = null;

            for (int i = pathIdx; i < fullPath.Count; i++)
            {
                var step = fullPath[i];
                var cell = PassengerGrid.Instance.GetCell(step.x, step.y);
                if (cell == null || cell.cellType == GridCellType.Blocked || cell.cellType == GridCellType.Empty)
                {
                    Debug.LogWarning($"[ContinuousPath] Path blocked by terrain at {step}. Returning home.");
                    if (stopIndex != -1) StopManager.Instance.CancelReservation(stopIndex, this);
                    yield return StartCoroutine(GoHome(overallOrigin));
                    isMoving = false;
                    yield break;
                }

                var occupant = PassengerGrid.Instance.GetOccupant(step);
                if (occupant != null && occupant != this)
                {
                    if (ascendIndex >= 0 && i > ascendIndex)
                    {
                        var c = cell;
                        bool isJumpable = (c.cellType == GridCellType.Stop || c.cellType == GridCellType.Walkable);
                        if (isJumpable && !occupant.isMoving) continue;
                    }

                    segmentEndIdx = i;
                    obstacle = occupant;
                    break;
                }
            }

            List<Vector2Int> gridSegment;
            if (segmentEndIdx != -1) gridSegment = fullPath.GetRange(pathIdx, segmentEndIdx - pathIdx);
            else gridSegment = fullPath.GetRange(pathIdx, fullPath.Count - pathIdx);

            if (gridSegment.Count > 0)
            {
                List<Vector3> worldSegment = gridSegment.ConvertAll(p => PassengerGrid.Instance.GetWorldPosition(p));
                Vector2Int endOfSegmentPos = gridSegment[gridSegment.Count - 1];

                PassengerGrid.Instance.UnregisterOccupant(gridPos, this);
                PassengerGrid.Instance.RegisterOccupant(endOfSegmentPos, this);

                float duration = (worldSegment.Count * PassengerGrid.Instance.gridData.cellSize) / moveSpeed;

                var segmentLog = new List<string>();
                foreach(var pos in gridSegment) segmentLog.Add($"{pos}:{PassengerGrid.Instance.GetCell(pos.x, pos.y)?.cellType}");
                Debug.Log("[ContinuousPath] Segment: " + string.Join(" -> ", segmentLog.ToArray()));

                Debug.Log($"[ContinuousPath] Moving along segment of {gridSegment.Count} cells.");
                
                Transform transformToRotate = modelTransform != null ? modelTransform : transform;
                var pathTween = transform.DOPath(worldSegment.ToArray(), duration, PathType.Linear).SetEase(Ease.Linear);

                activeMovementTween = pathTween;
                activeTweenBaseSpeed = moveSpeed;
                activeMovementType = MovementType.Path;
                activeMovementPathPoints = worldSegment.ToArray();
                pathTween.OnComplete(() => { if (activeMovementTween == pathTween) { activeMovementTween = null; activeMovementType = MovementType.None; activeMovementPathPoints = null; } });

                pathTween.OnUpdate(() =>
                {
                    float lookAheadPercentage = pathTween.ElapsedPercentage() + 0.05f; 
                    if (lookAheadPercentage > 1f) lookAheadPercentage = 1f;
                    
                    Vector3 lookAtPos = pathTween.PathGetPoint(lookAheadPercentage);

                    if (Vector3.Distance(transform.position, lookAtPos) > 0.1f)
                    {
                        Vector3 lookTarget = new Vector3(lookAtPos.x, transformToRotate.position.y, lookAtPos.z);
                        transformToRotate.LookAt(lookTarget, Vector3.up);
                        Vector3 e = transformToRotate.eulerAngles;
                        transformToRotate.rotation = Quaternion.Euler(0f, e.y, 0f);
                    }
                });

                yield return pathTween.WaitForCompletion();
                
                gridPos = endOfSegmentPos;
                pathIdx += gridSegment.Count;
            }

            if (obstacle != null)
            {
                var step = fullPath[segmentEndIdx];

                if (obstacle.isMoving)
                {
                    Debug.LogWarning($"[ContinuousPath] Path at {step} is blocked by moving passenger '{obstacle.name}'. Waiting.");
                    yield return new WaitUntil(() => PassengerGrid.Instance.GetOccupant(step) != obstacle || !obstacle.isMoving);
                    continue;
                }

                var occupiedCell = PassengerGrid.Instance.GetCell(step.x, step.y);
                bool isJumpable = (occupiedCell.cellType == GridCellType.Stop || occupiedCell.cellType == GridCellType.Walkable);

                if (isJumpable)
                {
                    Debug.LogWarning($"[ContinuousPath] Occupant '{obstacle.name}' is stationary on a jumpable tile. Waiting 1s.");
                    yield return new WaitForSeconds(1f);

                    if (PassengerGrid.Instance.GetOccupant(step) == obstacle)
                    {
                        Debug.LogWarning($"[ContinuousPath] Occupant '{obstacle.name}' is still there. Aborting path.");
                        if (stopIndex != -1) StopManager.Instance.CancelReservation(stopIndex, this);
                        yield return StartCoroutine(GoHome(overallOrigin));
                        isMoving = false;
                        yield break;
                    }
                    continue;
                }

                Debug.LogWarning($"[ContinuousPath] Path at {step} is blocked by non-jumpable obstacle '{obstacle.name}'. Returning to origin.");
                if (stopIndex != -1) StopManager.Instance.CancelReservation(stopIndex, this);
                yield return StartCoroutine(GoHome(overallOrigin));
                isMoving = false;
                yield break;
            }
        }

        if (stopIndex != -1)
        {
            yield return StartCoroutine(MoveToWorld(stopWorldPos, gridPos));
            PassengerGrid.Instance.UnregisterOccupant(gridPos, this);
            StopManager.Instance.ConfirmArrivalAtStop(stopIndex, this);
        }
        isMoving = false;
    }

    System.Collections.IEnumerator GoHome(Vector2Int origin)
    {
        isMoving = true;
        yield return StartCoroutine(MoveToCoroutine(origin));
        Vector3 finalDirVector = new Vector3(moveDirection.x, 0, moveDirection.y);
        Quaternion finalRotation = transform.rotation;
        if (finalDirVector != Vector3.zero)
        {
            var rot = Quaternion.LookRotation(finalDirVector);
            finalRotation = Quaternion.Euler(0f, rot.eulerAngles.y, 0f);
        }

        float spinDuration = 0.5f;
        Transform transformToRotate = modelTransform != null ? modelTransform : transform;
        
        yield return transformToRotate.DORotate(finalRotation.eulerAngles + new Vector3(0, 360, 0), spinDuration, RotateMode.FastBeyond360)
            .SetEase(Ease.OutSine)
            .WaitForCompletion();

        transformToRotate.rotation = finalRotation;
        isMoving = false;
    }

    System.Collections.IEnumerator JumpToCoroutine(Vector2Int landingGridPos)
    {
        if (PassengerGrid.Instance == null) yield break;
        var landCell = PassengerGrid.Instance.GetCell(landingGridPos.x, landingGridPos.y);
        if (landCell == null) yield break;
        PassengerGrid.Instance.UnregisterOccupant(gridPos, this);
        PassengerGrid.Instance.RegisterOccupant(landingGridPos, this);

        Vector3 landWorld = landCell.cellTransform != null ? landCell.cellTransform.position : PassengerGrid.Instance.GetWorldPosition(landingGridPos);
        float jumpPower = 1f;
        float duration = 0.45f;
        Vector3 target = new Vector3(landWorld.x, transform.position.y, landWorld.z);
    Tween t = transform.DOJump(target, jumpPower, 1, duration).SetEase(Ease.OutQuad);
    activeMovementTween = t;
    activeTweenBaseSpeed = moveSpeed;
    activeMovementType = MovementType.Jump;
    activeMovementTarget = target;
    t.OnComplete(() => { if (activeMovementTween == t) { activeMovementTween = null; activeMovementType = MovementType.None; } });
    yield return t.WaitForCompletion();
        gridPos = landingGridPos;
    }

    System.Collections.IEnumerator BounceVisual()
    {
        Vector3 original = transform.position;
        Vector3 back = original - new Vector3(moveDirection.x * 0.2f, 0, moveDirection.y * 0.2f);
        float t = 0f;
        while (t < 0.1f) { transform.position = Vector3.Lerp(original, back, t / 0.1f); t += Time.deltaTime; yield return null; }
        t = 0f;
        while (t < 0.1f) { transform.position = Vector3.Lerp(back, original, t / 0.1f); t += Time.deltaTime; yield return null; }
        transform.position = original;
    }

    System.Collections.IEnumerator MoveToWorld(Vector3 target, Vector2Int finalGridPos)
    {
        Vector3 startPos = transform.position;
        Vector3 direction = target - startPos;
        direction.y = 0;

        Quaternion targetRotation = transform.rotation;
        if (direction.sqrMagnitude > 0.01f)
        {
            var rot = Quaternion.LookRotation(direction);
            targetRotation = Quaternion.Euler(0f, rot.eulerAngles.y, 0f);
        }

        float moveDuration = Vector3.Distance(startPos, target) / moveSpeed;
        float rotationDuration = moveDuration * 0.8f;

        Transform transformToRotate = modelTransform != null ? modelTransform : transform;

        Sequence sequence = DOTween.Sequence();
        
        sequence.Join(transformToRotate.DORotateQuaternion(targetRotation, rotationDuration).SetEase(Ease.OutQuad));
        sequence.Join(transform.DOMove(target, moveDuration).SetEase(Ease.InOutSine));

    activeMovementTween = sequence;
    activeTweenBaseSpeed = moveSpeed;
    activeMovementType = MovementType.Move;
    activeMovementTarget = target;
    sequence.OnComplete(() => { if (activeMovementTween == sequence) { activeMovementTween = null; activeMovementType = MovementType.None; } });

        yield return sequence.WaitForCompletion();
    }

    public void SetMoveSpeed(float newSpeed)
    {
        if (newSpeed <= 0f) return;
        float old = moveSpeed;
        moveSpeed = newSpeed;

        if (activeMovementTween != null && activeMovementTween.IsActive() && activeMovementTween.active)
        {
            try { activeMovementTween.Kill(); } catch { }

            if (activeMovementType == MovementType.Move)
            {
                Vector3 currPos = transform.position;
                float remaining = Vector3.Distance(currPos, activeMovementTarget);
                if (remaining > 0.001f)
                {
                    float newDur = remaining / moveSpeed;
                    Sequence seq = DOTween.Sequence();
                    Transform transformToRotate = modelTransform != null ? modelTransform : transform;
                    Quaternion rot = transformToRotate.rotation; 
                    seq.Join(transform.DOMove(activeMovementTarget, newDur).SetEase(Ease.InOutSine));
                    activeMovementTween = seq;
                    activeTweenBaseSpeed = moveSpeed;
                    activeMovementType = MovementType.Move;
                    seq.OnComplete(() => { if (activeMovementTween == seq) { activeMovementTween = null; activeMovementType = MovementType.None; } });
                }
                else { activeMovementTween = null; activeMovementType = MovementType.None; }
            }
            else if (activeMovementType == MovementType.Path && activeMovementPathPoints != null && activeMovementPathPoints.Length > 0)
            {
                Vector3 currPos = transform.position;
                int startIdx = 0;
                float bestDist = float.MaxValue;
                for (int i = 0; i < activeMovementPathPoints.Length; i++)
                {
                    float d = Vector3.Distance(currPos, activeMovementPathPoints[i]);
                    if (d < bestDist) { bestDist = d; startIdx = i; }
                }
                List<Vector3> remaining = new List<Vector3>();
                for (int i = startIdx; i < activeMovementPathPoints.Length; i++) remaining.Add(activeMovementPathPoints[i]);
                if (remaining.Count > 0)
                {
                    float totalDist = 0f;
                    Vector3 prev = currPos;
                    foreach (var p in remaining) { totalDist += Vector3.Distance(prev, p); prev = p; }
                    float newDur = totalDist / moveSpeed;
                    var pathTween = transform.DOPath(remaining.ToArray(), newDur, PathType.Linear).SetEase(Ease.Linear);
                    activeMovementTween = pathTween;
                    activeTweenBaseSpeed = moveSpeed;
                    activeMovementPathPoints = remaining.ToArray();
                    activeMovementType = MovementType.Path;
                    pathTween.OnComplete(() => { if (activeMovementTween == pathTween) { activeMovementTween = null; activeMovementType = MovementType.None; activeMovementPathPoints = null; } });
                }
                else { activeMovementTween = null; activeMovementType = MovementType.None; }
            }
            else if (activeMovementType == MovementType.Jump)
            {
                Vector3 currPos = transform.position;
                float remaining = Vector3.Distance(currPos, activeMovementTarget);
                if (remaining > 0.001f)
                {
                    float estDur = remaining / moveSpeed;
                    var t = transform.DOMove(activeMovementTarget, estDur).SetEase(Ease.OutQuad);
                    activeMovementTween = t;
                    activeTweenBaseSpeed = moveSpeed;
                    activeMovementType = MovementType.Move;
                    t.OnComplete(() => { if (activeMovementTween == t) { activeMovementTween = null; activeMovementType = MovementType.None; } });
                }
                else { activeMovementTween = null; activeMovementType = MovementType.None; }
            }
            else { activeMovementTween = null; activeMovementType = MovementType.None; }
        }
    }

    public void DoubleMoveSpeed()
    {
        SetMoveSpeed(moveSpeed * 2f);
    }

    public void ReturnToOrigin()
    {
        Debug.Log($"[PassengerGroup] {name} is being recalled to origin.");

        // Stop any current movement to avoid conflicts
        if (isMoving && activeMovementTween != null && activeMovementTween.IsActive())
        {
            activeMovementTween.Kill();
        }
        StopAllCoroutines(); // Resets all movement logic
        isMoving = false;

        // Free up the stop this passenger might be on
        if (StopManager.Instance != null)
        {
            StopManager.Instance.EvictPassenger(this);
        }

        // Pathfind back to the start
        StartCoroutine(ReturnToOriginCoroutine());
    }

    private System.Collections.IEnumerator ReturnToOriginCoroutine()
    {
        // Convert originalPosition (Vector3) to originalGridPos (Vector2Int)
        if (PassengerGrid.Instance == null || PassengerGrid.Instance.gridData == null)
        {
            Debug.LogError($"[ReturnToOrigin] Grid or GridData is null for {name}. Cannot pathfind.");
            yield break;
        }
        var gd = PassengerGrid.Instance.gridData;
        Vector3 relative = originalPosition - PassengerGrid.Instance.transform.position - gd.worldOffset;
        int gx = Mathf.RoundToInt(relative.x / gd.cellSize);
        int gy = Mathf.RoundToInt(relative.z / gd.cellSize);
        Vector2Int originGridPos = new Vector2Int(gx, gy);

        // Find a path from the current grid position to the origin
        List<Vector2Int> path = PassengerGrid.Instance.FindPathToTarget(gridPos, originGridPos, this, new List<GridCellType> { GridCellType.Walkable, GridCellType.WaitingArea, GridCellType.Stop });

        if (path != null && path.Count > 0)
        {
            // Use the existing path execution logic, but tell it we are not ending at a stop.
            yield return StartCoroutine(ExecuteContinuousPath(path, -1, Vector3.zero));

            // Add the final rotation animation from the GoHome coroutine
            Vector3 finalDirVector = new Vector3(moveDirection.x, 0, moveDirection.y);
            Quaternion finalRotation = transform.rotation;
            if (finalDirVector != Vector3.zero)
            {
                var rot = Quaternion.LookRotation(finalDirVector);
                finalRotation = Quaternion.Euler(0f, rot.eulerAngles.y, 0f);
            }

            float spinDuration = 0.5f;
            Transform transformToRotate = modelTransform != null ? modelTransform : transform;
            
            yield return transformToRotate.DORotate(finalRotation.eulerAngles + new Vector3(0, 360, 0), spinDuration, RotateMode.FastBeyond360)
                .SetEase(Ease.OutSine)
                .WaitForCompletion();

            transformToRotate.rotation = finalRotation;

            Debug.Log($"[PassengerGroup] {name} has successfully returned to its origin point and reoriented.");
        }
        else
        {
            // If no path, teleport and also apply the final rotation.
            Debug.LogWarning($"[ReturnToOrigin] No path found for {name} to return home from {gridPos} to {originGridPos}. Teleporting as fallback.");
            transform.position = originalPosition;
            PassengerGrid.Instance.UnregisterOccupant(gridPos, this);
            gridPos = originGridPos;
            PassengerGrid.Instance.RegisterOccupant(gridPos, this);

            Vector3 finalDirVector = new Vector3(moveDirection.x, 0, moveDirection.y);
            Quaternion finalRotation = transform.rotation;
            if (finalDirVector != Vector3.zero)
            {
                var rot = Quaternion.LookRotation(finalDirVector);
                finalRotation = Quaternion.Euler(0f, rot.eulerAngles.y, 0f);
            }
            Transform transformToRotate = modelTransform != null ? modelTransform : transform;
            transformToRotate.rotation = finalRotation;
        }
    }

    private void LogPathNotFound()
    {
        Debug.Log("Yol bulunamadı");
        // We can add a visual feedback for the player here later.
    }

    private void TryMoveToWaitingArea()
    {
        if (isMoving) return;
        if (PassengerGrid.Instance == null || PassengerGrid.Instance.gridData == null) 
        {
            LogPathNotFound();
            return;
        }

        GridCell bestCandidate = null;
        float minDistance = float.MaxValue;

        // 1. Find the nearest valid waiting area cell
        foreach (var cell in PassengerGrid.Instance.gridData.cells)
        {
            if (cell.position.y == 1 && cell.cellType == GridCellType.WaitingArea)
            {
                if (!PassengerGrid.Instance.IsOccupied(cell.position))
                {
                    float distance = Vector3.Distance(transform.position, PassengerGrid.Instance.GetWorldPosition(cell.position));
                    if (distance < minDistance)
                    { 
                        minDistance = distance;
                        bestCandidate = cell;
                    }
                }
            }
        }

        if (bestCandidate == null)
        {
            LogPathNotFound();
            return;
        }

        // 2. Check if a path exists from that cell to a stop
        var path = PassengerGrid.Instance.FindNearestStopPath(bestCandidate.position);
        if (path == null || path.Count == 0)
        {
            LogPathNotFound();
            return;
        }

        // All checks passed, let's move!
        Debug.Log($"[PassengerGroup] {name} moving from conveyor to waiting area at {bestCandidate.position}");

        // Remove from conveyor
        if (ConveyorManager.Instance != null)
        {
            ConveyorManager.Instance.RemovePassenger(this);
        }
        onConveyorBelt = false;

        // The path from FindNearestStopPath starts at the candidate position. We need to move there first.
        // We can create a new path that starts from the passenger's current world position.
        // For simplicity, we can just start the pathfinding from the grid.
        
        var reservation = StopManager.Instance.ReserveFirstFreeStop(this);
        if (reservation == null)
        {
            LogPathNotFound();
            // Should we re-add the passenger to the conveyor? For now, no.
            return;
        }

        var (stopWorldPos, stopIndex) = reservation.Value;

        // We need to pathfind from our new starting cell.
        List<Vector2Int> fullPath = PassengerGrid.Instance.FindPathToTarget(bestCandidate.position, path[path.Count-1], this);

        if (fullPath != null && fullPath.Count > 0)
        {
            // We need to place the passenger on the grid to start the path.
            gridPos = bestCandidate.position;
            transform.position = PassengerGrid.Instance.GetWorldPosition(gridPos);
            PassengerGrid.Instance.RegisterOccupant(gridPos, this);

            OnGroupDeparted?.Invoke(this);
            StartCoroutine(ExecuteContinuousPath(fullPath, stopIndex, stopWorldPos));
        }
        else
        {
            StopManager.Instance.CancelReservation(stopIndex, this);
            LogPathNotFound();
        }
    }
}