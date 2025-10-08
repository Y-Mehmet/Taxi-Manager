using UnityEngine;
using System.Collections.Generic;
using GridSystem;
using GridSystem.Data;
using DG.Tweening;

public class PassengerGroup : MonoBehaviour
{
    [Header("Yolcu grubunun yönü (sağ, sol, yukarı, aşağı)")]
    public Vector2Int moveDirection = Vector2Int.up;
    [Header("Yolcu Grubu Ayarları")]
    [Tooltip("Dönüş animasyonlarının uygulanacağı görsel model.")]
    public Transform modelTransform;
    public int groupSize = 4; // 2, 4, 6, 8
    public HyperCasualColor groupColor = HyperCasualColor.Yellow;
    public float moveSpeed = 2f;
    public Vector2Int gridPos; // Şu anki grid pozisyonu
    public PassengerGrid grid;
    [Header("Initialization")]
    [Tooltip("If true, this object will be placed at `gridPos` on Start(). Leave false for manual placement.")]
    public bool useGridPosition = false;
    [Header("Yön Göstergesi")]
    public Transform directionIndicator;

    // --- Convoy / train follow support ---
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
    private bool processingCheckpointQueue = false;
    private bool isMoving = false;

    void Update()
    {
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
           Ray ray = Camera.main.ScreenPointToRay(Input.GetTouch(0).position);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.transform == this.transform)
                {
                    TryMoveForwardWithLog();
                }
            }
        }
#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.transform == this.transform)
                {
                    TryMoveForwardWithLog();
                }
            }
        }
#endif
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
        var cell = grid.GetCell(nextPos.x, nextPos.y);
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
        while (grid.GetCell(tempCursor.x, tempCursor.y) != null)
        {
            var currentCell = grid.GetCell(tempCursor.x, tempCursor.y);
            if (currentCell.cellType == GridCellType.Blocked || currentCell.cellType == GridCellType.Empty)
            {
                break;
            }
            straightVec.Add(tempCursor);
            if (currentCell.cellType == GridCellType.Walkable || currentCell.cellType == GridCellType.Stop)
            {
                pathfindingStartPoint = tempCursor;
                break;
            }
            tempCursor += moveDirection;
        }

        var reservation = StopManager.Instance.ReserveFirstFreeStop(this);
        if (reservation != null)
        {
            var (stopPos, stopIndex) = reservation.Value;
            var pathToStop = grid.FindPathToTarget(pathfindingStartPoint, stopPos, this);

            List<Vector2Int> fullPath = new List<Vector2Int>(straightVec);
            if (pathToStop != null)
            {
                fullPath.AddRange(pathToStop);
            }

            var pathStr = fullPath.Count > 0 ? string.Join(" -> ", fullPath.ConvertAll(p => p.ToString()).ToArray()) : "(no path)";
            Debug.LogWarning($"[PathPlan] Full path: {pathStr}");
            Debug.LogWarning($"[AssignedStop] index={stopIndex} pos={stopPos}");

            StartCoroutine(ExecuteContinuousPath(fullPath, stopIndex, stopPos));
        }
        else
        {
            var fallbackPath = grid.FindNearestStopPath(pathfindingStartPoint);
            if (fallbackPath != null)
            {
                List<Vector2Int> fullPath = new List<Vector2Int>(straightVec);
                fullPath.AddRange(fallbackPath);
                var fallbackStr = string.Join(" -> ", fullPath.ConvertAll(p => p.ToString()).ToArray());
                Debug.LogWarning($"[PathPlan] planned path (no reservation) from {pathfindingStartPoint}: {fallbackStr}");
                StartCoroutine(ExecuteContinuousPath(fullPath, -1, new Vector2Int(-1, -1)));
            }
        }
    }

    void Start()
    {
        SetGroupColor(groupColor);

        if (useGridPosition && grid != null)
        {
            transform.position = grid.GetWorldPosition(gridPos);
            grid.RegisterOccupant(gridPos, this);
        }
        else if (!useGridPosition && grid != null)
        {
            var gd = grid.gridData;
            if (gd != null)
            {
                Vector3 relative = transform.position - grid.transform.position - gd.worldOffset;
                int gx = Mathf.RoundToInt(relative.x / gd.cellSize);
                int gy = Mathf.RoundToInt(relative.z / gd.cellSize);
                gx = Mathf.Clamp(gx, 0, gd.width - 1);
                gy = Mathf.Clamp(gy, 0, gd.height - 1);
                Vector2Int inferred = new Vector2Int(gx, gy);
                gridPos = inferred;
                if (grid.IsOccupied(gridPos))
                {
                    Debug.LogWarning($"Passenger '{name}' manual placement at {gridPos} conflicts with existing occupant.");
                }
                grid.RegisterOccupant(gridPos, this);
            }
        }

        if (!allGroups.Contains(this)) allGroups.Add(this);
    }

    void OnDestroy()
    {
        if (allGroups.Contains(this)) allGroups.Remove(this);
    }

    void OnDisable()
    {
        if (grid != null)
        {
            grid.UnregisterOccupant(gridPos, this);
        }
    }

    public void SetGroupColor(HyperCasualColor color)
    {
        foreach (Transform child in transform)
        {
            if (directionIndicator != null && child == directionIndicator)
            {
                continue;
            }

            var renderer = child.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color.ToColor();
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
                if (!isMoving)
                {
                    StartCoroutine(MoveToCoroutine(targetPos));
                    lastRailIndex = targetIdx;
                }
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
        while (followQueue.Count < Mathf.Max(1, followStepDelay))
        {
            yield return null;
        }

        while (followQueue.Count > 0)
        {
            if (isMoving)
            {
                yield return null;
                continue;
            }
            Vector2Int target = followQueue.Dequeue();
            var cell = grid.GetCell(target.x, target.y);
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
            if (grid == null || grid.gridData == null) continue;
            if (stopIndex < 0 || stopIndex >= grid.gridData.stopSlots.Count) continue;
            var stopPos = grid.gridData.stopSlots[stopIndex];
            var path = grid.FindPathToTarget(gridPos, stopPos, this);
            if (path != null && path.Count > 0)
            {
                StartCoroutine(ExecuteContinuousPath(path, stopIndex, stopPos));
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
        if (grid == null) yield break;
        var cell = grid.GetCell(newGridPos.x, newGridPos.y);
        if (cell == null) yield break;
        if (!(cell.cellType == GridCellType.Walkable || cell.cellType == GridCellType.WaitingArea || cell.cellType == GridCellType.Stop)) yield break;

        Vector3 worldTarget = cell.cellTransform != null ? cell.cellTransform.position : grid.GetWorldPosition(newGridPos);
        grid.UnregisterOccupant(gridPos, this);
        grid.RegisterOccupant(newGridPos, this);
        isMoving = true;
        yield return StartCoroutine(MoveToWorld(worldTarget, newGridPos));
        gridPos = newGridPos;
        var arrivedCell = grid.GetCell(gridPos.x, gridPos.y);
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
            if (g != null && g.followTarget == this)
            {
                g.checkpointQueue.Enqueue(stopIndex);
            }
        }
    }

    System.Collections.IEnumerator ExecuteContinuousPath(List<Vector2Int> fullPath, int stopIndex, Vector2Int stopPos)
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
                var cell = grid.GetCell(step.x, step.y);
                if (cell == null || cell.cellType == GridCellType.Blocked || cell.cellType == GridCellType.Empty)
                {
                    Debug.LogWarning($"[ContinuousPath] Path blocked by terrain at {step}. Returning home.");
                    if (stopIndex != -1) StopManager.Instance.CancelReservation(stopIndex, this);
                    yield return StartCoroutine(GoHome(overallOrigin));
                    isMoving = false;
                    yield break;
                }

                var occupant = grid.GetOccupant(step);
                if (occupant != null && occupant != this)
                {
                    segmentEndIdx = i;
                    obstacle = occupant;
                    break;
                }
            }

            List<Vector2Int> gridSegment;
            if (segmentEndIdx != -1)
            {
                gridSegment = fullPath.GetRange(pathIdx, segmentEndIdx - pathIdx);
            }
            else
            {
                gridSegment = fullPath.GetRange(pathIdx, fullPath.Count - pathIdx);
            }

            if (gridSegment.Count > 0)
            {
                List<Vector3> worldSegment = gridSegment.ConvertAll(p => grid.GetWorldPosition(p));
                Vector2Int endOfSegmentPos = gridSegment[gridSegment.Count - 1];

                grid.UnregisterOccupant(gridPos, this);
                grid.RegisterOccupant(endOfSegmentPos, this);

                float duration = (worldSegment.Count * grid.gridData.cellSize) / moveSpeed;

                Debug.Log($"[ContinuousPath] Moving along segment of {gridSegment.Count} cells.");
                
                Transform transformToRotate = modelTransform != null ? modelTransform : transform;
                var pathTween = transform.DOPath(worldSegment.ToArray(), duration, PathType.Linear)
                    .SetEase(Ease.Linear);

                pathTween.OnUpdate(() =>
                {
                    float lookAheadPercentage = pathTween.ElapsedPercentage() + 0.05f; 
                    if (lookAheadPercentage > 1f)
                    {
                        lookAheadPercentage = 1f;
                    }
                    
                    Vector3 lookAtPos = pathTween.PathGetPoint(lookAheadPercentage);

                    if (Vector3.Distance(transform.position, lookAtPos) > 0.1f)
                    {
                        transformToRotate.LookAt(lookAtPos);
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
                    yield return new WaitUntil(() => grid.GetOccupant(step) != obstacle || !obstacle.isMoving);
                    continue;
                }

                var occupiedCell = grid.GetCell(step.x, step.y);
                bool isJumpable = (occupiedCell.cellType == GridCellType.Stop || occupiedCell.cellType == GridCellType.Walkable);

                if (isJumpable)
                {
                    Debug.LogWarning($"[ContinuousPath] Occupant '{obstacle.name}' is stationary on a jumpable tile. Waiting 1s.");
                    yield return new WaitForSeconds(1f);

                    if (grid.GetOccupant(step) == obstacle)
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

        if (gridPos == stopPos && stopIndex != -1)
        {
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
            finalRotation = Quaternion.LookRotation(finalDirVector);
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
        if (grid == null) yield break;
        var landCell = grid.GetCell(landingGridPos.x, landingGridPos.y);
        if (landCell == null) yield break;
        grid.UnregisterOccupant(gridPos, this);
        grid.RegisterOccupant(landingGridPos, this);

        Vector3 landWorld = landCell.cellTransform != null ? landCell.cellTransform.position : grid.GetWorldPosition(landingGridPos);
        float jumpPower = 1f;
        float duration = 0.45f;
        Vector3 target = new Vector3(landWorld.x, transform.position.y, landWorld.z);
        Tween t = transform.DOJump(target, jumpPower, 1, duration).SetEase(Ease.OutQuad);
        yield return t.WaitForCompletion();
        gridPos = landingGridPos;
    }

    System.Collections.IEnumerator BounceVisual()
    {
        Vector3 original = transform.position;
        Vector3 back = original - new Vector3(moveDirection.x * 0.2f, 0, moveDirection.y * 0.2f);
        float t = 0f;
        while (t < 0.1f)
        {
            transform.position = Vector3.Lerp(original, back, t / 0.1f);
            t += Time.deltaTime;
            yield return null;
        }
        t = 0f;
        while (t < 0.1f)
        {
            transform.position = Vector3.Lerp(back, original, t / 0.1f);
            t += Time.deltaTime;
            yield return null;
        }
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
            targetRotation = Quaternion.LookRotation(direction);
        }

        float moveDuration = Vector3.Distance(startPos, target) / moveSpeed;
        float rotationDuration = moveDuration * 0.8f;

        Transform transformToRotate = modelTransform != null ? modelTransform : transform;

        Sequence sequence = DOTween.Sequence();
        
        sequence.Join(transformToRotate.DORotateQuaternion(targetRotation, rotationDuration).SetEase(Ease.OutQuad));
        sequence.Join(transform.DOMove(target, moveDuration).SetEase(Ease.InOutSine));

        yield return sequence.WaitForCompletion();
    }
}
