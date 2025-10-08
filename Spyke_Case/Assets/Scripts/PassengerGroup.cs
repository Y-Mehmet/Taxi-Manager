
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
    //[Tooltip("If set, this PassengerGroup will follow the movements of the given leader group (rail-like behavior)")]
    public PassengerGroup followTarget = null;
    [Tooltip("Number of steps of delay behind the leader (1 = directly into leader's previous cell)")]
    public int followStepDelay = 1;

    // Static registry of all groups (used to find followers cheaply)
    private static List<PassengerGroup> allGroups = new List<PassengerGroup>();

    // --- Rail-mode support ---
    [Header("Rail mode")]
    [Tooltip("When true, followers will follow the leader's recorded route positions (rail-like). If false, legacy followQueue is used.")]
    public bool railMode = true;

    // leader -> recorded route positions (appended each time leader moves)
    private static Dictionary<PassengerGroup, List<Vector2Int>> leaderRoutes = new Dictionary<PassengerGroup, List<Vector2Int>>();

    // Per-follower index of last processed route entry (prevents repeated moves)
    private int lastRailIndex = -1;
    // Global rail route and head flag: when set, groups using rail will follow this global route
    [Tooltip("If true, this group will act as the global rail head and its moves will define the route all groups follow.")]
    public bool isRailHead = false;
    private static List<Vector2Int> globalRailRoute = new List<Vector2Int>();

    // Queue of positions to follow (filled by leader notifications)
    private Queue<Vector2Int> followQueue = new Queue<Vector2Int>();
    private bool processingFollowQueue = false;
    // Queue of checkpoint indices (stop indices) to follow
    private Queue<int> checkpointQueue = new Queue<int>();
    private bool processingCheckpointQueue = false;

    // --- METODLAR ---


    // Mobil tıklama algılama
    void Update()
    {
        // Sadece bir dokunuş varsa ve bu objeye dokunulduysa
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
        // Editor için mouse ile tıklama
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
        // Process follow queue if any
        UpdateFollowQueue();
        // Process checkpoint queue if any
        UpdateCheckpointQueue();
    }

    // Hareket denemesi ve loglama
    public void TryMoveForwardWithLog()
    {
        if (isMoving)
        {
            Debug.LogWarning($"Yolcu '{name}' zaten hareket halinde olduğu için yeni hareket başlatılamadı.");
            return;
        }

        // YENİ KONTROL: Harekete başlamadan önce boş durak var mı?
        if (StopManager.Instance != null && !StopManager.Instance.HasAvailableStops())
        {
            Debug.LogWarning($"Tüm duraklar dolu veya rezerve edilmiş. '{name}' için hareket başlatılamadı.");
            // İsteğe bağlı olarak burada bir ses veya animasyon tetiklenebilir.
            return;
        }

        Vector2Int nextPos = gridPos + moveDirection;
        string reason = "";
        if (grid == null)
        {
            reason = "Grid referansı yok!";
        }
        else if (nextPos.x < 0 || nextPos.y < 0 || nextPos.x >= grid.gridWidth || nextPos.y >= grid.gridHeight)
        {
            reason = $"Grid dışında: {nextPos}";
        }
        else
        {
            var cell = grid.GetCell(nextPos.x, nextPos.y);
            if (cell == null)
            {
                reason = $"Gridde cell yok: {nextPos}";
            }
            else if (cell.cellType == GridCellType.Blocked || cell.cellType == GridCellType.Empty)
            {
                reason = $"Hedef ({nextPos}) bir engel.";
                StartCoroutine(BounceVisual());
            }
            else
            {
                // 1) Temel bilgileri logla
                Debug.LogWarning($"[MoveStart] Passenger '{name}' at {gridPos} moving {moveDirection}");

                // 2) Düz ilerleme yolunu (straightLeg) ve pathfinding başlangıç noktasını belirle
                List<Vector2Int> straightVec = new List<Vector2Int>();
                Vector2Int pathfindingStartPoint = gridPos; // Varsayılan olarak mevcut pozisyon
                
                Vector2Int tempCursor = gridPos + moveDirection;
                while (grid.GetCell(tempCursor.x, tempCursor.y) != null)
                {
                    var currentCell = grid.GetCell(tempCursor.x, tempCursor.y);
                    if (currentCell.cellType == GridCellType.Blocked || currentCell.cellType == GridCellType.Empty)
                    {
                        break; // Engele çarptı, düz ilerleme bitti.
                    }

                    straightVec.Add(tempCursor); // Adımı listeye ekle

                    // Eğer Walkable veya Stop alanına ulaşıldıysa, burası pathfinding başlangıç noktasıdır.
                    if (currentCell.cellType == GridCellType.Walkable || currentCell.cellType == GridCellType.Stop)
                    {
                        pathfindingStartPoint = tempCursor;
                        break; 
                    }

                    tempCursor += moveDirection;
                }

                // Debug için straightLeg'i logla
                var straightLog = new List<string>();
                foreach(var pos in straightVec) straightLog.Add($"{pos}:{grid.GetCell(pos.x, pos.y)?.cellType}");
                Debug.LogWarning("[StraightLeg] " + string.Join(" -> ", straightLog.ToArray()));

                // 3) Durak rezerve et ve yolu hesapla
                var reservation = StopManager.Instance.ReserveFirstFreeStop(this);
                if (reservation != null)
                {
                    var (stopPos, stopIndex) = reservation.Value;
                    var pathToStop = grid.FindPathToTarget(pathfindingStartPoint, stopPos, this);

                    var pathStr = pathToStop != null ? string.Join(" -> ", pathToStop.ConvertAll(p => p.ToString()).ToArray()) : "(no path)";
                    Debug.LogWarning($"[PathPlan] straight: {string.Join(" -> ", straightVec.ConvertAll(p => p.ToString()).ToArray())} ; pathToStop: {pathStr}");
                    Debug.LogWarning($"[AssignedStop] index={stopIndex} pos={stopPos}");

                    StartCoroutine(MoveAlongThenFollow(straightVec, pathToStop, stopIndex, stopPos));
                }
                else
                {
                    // Rezervasyon için boş durak bulunamadı, fallback path dene
                    var fallbackPath = grid.FindNearestStopPath(pathfindingStartPoint);
                    var fallbackStr = fallbackPath != null ? string.Join(" -> ", fallbackPath.ConvertAll(p => p.ToString()).ToArray()) : "(no path)";
                    Debug.LogWarning($"[PathPlan] planned path (no reservation) from {pathfindingStartPoint}: {fallbackStr}");
                    StartCoroutine(MoveAlongThenFollow(straightVec, fallbackPath, -1, new Vector2Int(-1, -1)));
                }
                return;
            }
        }
        Debug.Log($"PassengerGroup hareket edemedi. Şu anki slot: {gridPos}, yön: {moveDirection}, hedef: {nextPos}, Sebep: {reason}");
    }


    // Yolcu grubunun griddeki slotunu boşluk/engel açısından kontrol et
    public bool CanMoveForward()
    {
        if (grid == null) return false;
        Vector2Int nextPos = gridPos + moveDirection;
        var cell = grid.GetCell(nextPos.x, nextPos.y);
        if (cell == null) return false;
        // Walkable, WaitingArea veya Stop ise ilerlenebilir
        if (cell.cellType == GridCellType.Walkable || cell.cellType == GridCellType.WaitingArea || cell.cellType == GridCellType.Stop)
        {
            // Başka yolcu var mı kontrolü (örnek: collider veya başka bir kontrol eklenebilir)
            // Şimdilik sadece griddeki cellType'a bakıyoruz
            return true;
        }
        return false;
    }

    // Dokunma veya tetikleyici ile çağrılır
    public void TryMoveForward()
    {
        Vector2Int nextPos = gridPos + moveDirection;
        var cell = grid.GetCell(nextPos.x, nextPos.y);
        // Immediate obstacle or out-of-bounds -> bounce
        if (cell == null || cell.cellType == GridCellType.Blocked || cell.cellType == GridCellType.Empty)
        {
            StartCoroutine(BounceVisual());
            return;
        }

        // Check occupancy by other PassengerGroup at that cell
        bool occupied = grid != null && grid.IsOccupied(nextPos);

        if (occupied)
        {
            // Can we jump over? Allowed only if current cell is Walkable or Stop
            var curCell = grid.GetCell(gridPos.x, gridPos.y);
            if (curCell != null && (curCell.cellType == GridCellType.Walkable || curCell.cellType == GridCellType.Stop))
            {
                Vector2Int landing = nextPos + moveDirection; // two steps ahead
                var landCell = grid.GetCell(landing.x, landing.y);
                // Do not allow jumping over blocked/empty/waiting cells
                var midCell = grid.GetCell(nextPos.x, nextPos.y);
                if (midCell != null)
                {
                    bool midAllowed = (midCell.cellType == GridCellType.Walkable || midCell.cellType == GridCellType.Stop);
                    bool landingAllowed = (landCell != null && (landCell.cellType == GridCellType.Walkable || landCell.cellType == GridCellType.Stop));
                    bool landOccupied = grid.IsOccupied(landing);
                    if (midAllowed && landingAllowed && !landOccupied)
                    {
                        if (!isMoving)
                            StartCoroutine(JumpThenFindStop(landing));
                        return;
                    }
                }
            }

            // otherwise blocked: bounce
            StartCoroutine(BounceVisual());
            return;
        }

        // Otherwise allowed to move: move one step then attempt to find a stop
        if (!isMoving)
            StartCoroutine(MoveThenFindStop(nextPos));
    }


    void Start()
    {
        SetGroupColor(groupColor);

        // Ensure the group's world position matches its gridPos at start
        // Only apply if explicitly requested (prevents default 0,0 placement)
        if (useGridPosition && grid != null)
        {
            transform.position = grid.GetWorldPosition(gridPos);
            // Register occupancy for initial placement
            grid.RegisterOccupant(gridPos, this);
        }
        else if (!useGridPosition && grid != null)
        {
            // Manual placement: infer logical gridPos from world position and register
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

        // Register in global list for convoy support
        if (!allGroups.Contains(this)) allGroups.Add(this);
    }

    void OnDestroy()
    {
        if (allGroups.Contains(this)) allGroups.Remove(this);
    }

    void OnDisable()
    {
        // Bu yolcu deaktif edildiğinde, grid üzerindeki doluluk kaydını temizle.
        if (grid != null)
        {
            grid.UnregisterOccupant(gridPos, this);
        }
    }



    public void SetGroupColor(HyperCasualColor color)
    {
        // Yolcular artık prefab'dan instantiate edilmediği için,
        // renk ataması doğrudan child objelere yapılır.
        foreach (Transform child in transform)
        {
            // Yön göstergesi bu mantığın dışında tutulur.
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

    // Hareket fonksiyonu (örnek: sağa hareket)
    // Flag to prevent concurrent movement
    private bool isMoving = false;

    // Public wrapper to start coroutine
    public void MoveTo(Vector2Int newGridPos)
    {
        if (!isMoving)
            StartCoroutine(MoveToCoroutine(newGridPos));
    }

    // Start processing follow queue if items exist
    void UpdateFollowQueue()
    {
        // Rail-mode fast path: followers follow leaderRoutes by index
        if (railMode && followTarget != null)
        {
            if (!leaderRoutes.ContainsKey(followTarget)) return;
            var route = leaderRoutes[followTarget];
            int targetIdx = route.Count - followStepDelay - 1;
            if (targetIdx > lastRailIndex && targetIdx >= 0)
            {
                // Move into the leader's recorded cell at targetIdx
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
        // Start coroutine to process
        StartCoroutine(ProcessFollowQueue());
    }

    // Start processing checkpoint queue too
    void UpdateCheckpointQueue()
    {
        if (processingCheckpointQueue) return;
        if (checkpointQueue.Count == 0) return;
        StartCoroutine(ProcessCheckpointQueue());
    }

    System.Collections.IEnumerator ProcessFollowQueue()
    {
        processingFollowQueue = true;
        // Respect followStepDelay: wait until queue has at least followStepDelay items
        while (followQueue.Count < Mathf.Max(1, followStepDelay))
        {
            yield return null;
        }

        while (followQueue.Count > 0)
        {
            // Only move if not currently moving
            if (isMoving)
            {
                yield return null;
                continue;
            }

            // Dequeue the next target and move there directly (no pathfinding)
            Vector2Int target = followQueue.Dequeue();
            // Safety: ensure the target cell is valid and walkable
            var cell = grid.GetCell(target.x, target.y);
            if (cell == null)
            {
                // skip invalid
                continue;
            }

            // Move directly into the leader's previous position
            yield return StartCoroutine(MoveToCoroutine(target));
        }

        processingFollowQueue = false;
    }

    System.Collections.IEnumerator ProcessCheckpointQueue()
    {
        processingCheckpointQueue = true;
        while (checkpointQueue.Count > 0)
        {
            // Only act if not currently moving
            if (isMoving)
            {
                yield return null;
                continue;
            }

            int stopIndex = checkpointQueue.Dequeue();
            if (grid == null || grid.gridData == null) continue;
            if (stopIndex < 0 || stopIndex >= grid.gridData.stopSlots.Count) continue;
            var stopPos = grid.gridData.stopSlots[stopIndex];

            // Compute targeted path to that stop from current gridPos, honoring reservation
            var path = grid.FindPathToTarget(gridPos, stopPos, this);
            if (path != null && path.Count > 0)
            {
                yield return StartCoroutine(FollowPath(path, stopIndex));
            }
            else
            {
                // Could not compute path — skip or retry later. For now, wait a bit and continue.
                yield return new WaitForSeconds(0.15f);
            }
        }
        processingCheckpointQueue = false;
    }

    // Coroutine that moves to a grid cell and updates gridPos on arrival
    System.Collections.IEnumerator MoveToCoroutine(Vector2Int newGridPos)
    {
        if (grid == null) yield break;
        var cell = grid.GetCell(newGridPos.x, newGridPos.y);
        if (cell == null) yield break;
        if (!(cell.cellType == GridCellType.Walkable || cell.cellType == GridCellType.WaitingArea || cell.cellType == GridCellType.Stop)) yield break;

        // Compute world target
        Vector3 worldTarget = cell.cellTransform != null ? cell.cellTransform.position : grid.GetWorldPosition(newGridPos);

        // Update occupancy map: unregister old, register new (important to prevent others stepping)
        grid.UnregisterOccupant(gridPos, this);
        grid.RegisterOccupant(newGridPos, this);

        isMoving = true;
        // Use existing axis-aligned MoveToWorld but wait for it to finish
        yield return StartCoroutine(MoveToWorld(worldTarget, newGridPos));
        
        // MOVED FROM MoveToWorld: Update logical grid position AFTER movement is complete.
        gridPos = newGridPos;

        // After finishing move, notify any followers (leader broadcasts its previous position)
   // NotifyFollowersOfMove(gridPos);
   // // Record leader route for rail-mode
   // if (!leaderRoutes.ContainsKey(this)) leaderRoutes[this] = new List<Vector2Int>();
   // leaderRoutes[this].Add(gridPos);
        // If the cell we moved into is a Stop and has an index, broadcast checkpoint to followers
        var arrivedCell = grid.GetCell(gridPos.x, gridPos.y);
        if (arrivedCell != null && arrivedCell.cellType == GridCellType.Stop && arrivedCell.stopIndex >= 0)
        {
            BroadcastCheckpointToFollowers(arrivedCell.stopIndex);
        }
        isMoving = false;
    }

    // Notify followers: leader informs followers of its previous position so they can enqueue moves
    void NotifyFollowersOfMove(Vector2Int leaderPos)
   {
   //     // Anyone that has followTarget == this should receive the leaderPos into their queue
   //     foreach (var g in allGroups)
   //     {
   //         if (g == null) continue;
   //         if (g.followTarget == this)
   //         {
   //             // Enqueue leader's previous pos for the follower; follower will process with delay
   //             g.followQueue.Enqueue(leaderPos);
   //         }
   //     }
   }


   void BroadcastCheckpointToFollowers(int stopIndex)
    {
        foreach (var g in allGroups)
        {
            if (g == null) continue;
            if (g.followTarget == this)
            {
                g.checkpointQueue.Enqueue(stopIndex);
            }
        }
    }

    // Move one step and then attempt to find nearest stop and follow path
    System.Collections.IEnumerator MoveThenFindStop(Vector2Int stepPos)
    {
        // Move one step
        yield return StartCoroutine(MoveToCoroutine(stepPos));

        // After arriving, attempt BFS to nearest free stop
        if (grid == null) yield break;
        // Reserve a stop now to avoid race conditions: assign first free stop to this passenger
        var reservation = StopManager.Instance.ReserveFirstFreeStop(this);
        if (reservation != null)
        {
            // Log assignment and then path towards that specific stop
            var (stopPos, stopIndex) = reservation.Value;
            Debug.Log($"Passenger '{name}' assigned to stop index {stopIndex} at {stopPos}");
            // Find path to the reserved stop using targeted pathfinder
            var pathToStop = grid.FindPathToTarget(gridPos, stopPos, this);
            if (pathToStop != null && pathToStop.Count > 0)
            {
                yield return StartCoroutine(FollowPath(pathToStop, stopIndex));
            }
            // When arrived at stop, release reservation and log arrival
            // Note: MoveToCoroutine updates gridPos when arriving, so check if gridPos == stopPos
            if (gridPos == stopPos)
            {
                // Durağa vardığımızı StopManager'a bildir.
                StopManager.Instance.ConfirmArrivalAtStop(stopIndex, this);
            }
            yield break;
        }

        // If no reservation available, fallback to generic nearest-stop path
        var path = grid.FindNearestStopPath(gridPos);
        if (path != null && path.Count > 0)
        {
            yield return StartCoroutine(FollowPath(path, -1));
        }
    }

    // Jump two cells ahead then look for nearest stop
    System.Collections.IEnumerator JumpThenFindStop(Vector2Int landingPos)
    {
        // perform jump animation
        yield return StartCoroutine(JumpToCoroutine(landingPos));

        // After landing, attempt BFS to nearest free stop
        if (grid == null) yield break;
        // Prefer reserved stop if we have one
        var reserved = StopManager.Instance.GetReservedStopFor(this);
        List<Vector2Int> path = null;
        if (reserved != null)
        {
            path = grid.FindPathToTarget(gridPos, reserved.Value.pos, this);
        }
        if (path == null)
            path = grid.FindNearestStopPath(gridPos);
        if (path != null && path.Count > 0)
        {
            yield return StartCoroutine(FollowPath(path, -1));
        }
    }

    // Follow a path given as a list of grid positions (each step will be executed sequentially)
    System.Collections.IEnumerator FollowPath(List<Vector2Int> path, int stopIndex, System.Nullable<Vector2Int> returnOrigin = null)
    {
        Vector2Int origin = returnOrigin.HasValue ? returnOrigin.Value : gridPos;
        for (int i = 0; i < path.Count; i++)
        {
            var step = path[i];
            var targetCell = grid.GetCell(step.x, step.y);
            if (targetCell == null || targetCell.cellType == GridCellType.Blocked || targetCell.cellType == GridCellType.Empty)
            {
                Debug.LogWarning($"[FollowPath] Path is blocked by terrain at {step}. Returning to origin.");
                if (stopIndex != -1) StopManager.Instance.CancelReservation(stopIndex, this);
                yield return StartCoroutine(GoHome(origin));
                yield break;
            }

            if (grid.IsOccupied(step))
            {
                var occupant = grid.GetOccupant(step);
                if (occupant == this)
                {
                    yield return StartCoroutine(MoveToCoroutine(step));
                    continue;
                }

                if (occupant != null && occupant.isMoving)
                {
                    Debug.LogWarning($"[FollowPath] Path at {step} is blocked by moving passenger '{occupant.name}'. Waiting.");
                    yield return new WaitUntil(() => grid.GetOccupant(step) != occupant || !occupant.isMoving);
                }

                if (grid.IsOccupied(step) && grid.GetOccupant(step) != this)
                {
                    occupant = grid.GetOccupant(step);
                    var occupiedCell = grid.GetCell(step.x, step.y);
                    // MODIFIED: An obstacle is jumpable if it's stationary on a Stop OR a Walkable tile.
                    bool isJumpableObstacle = occupant != null && !occupant.isMoving && (occupiedCell.cellType == GridCellType.Stop || occupiedCell.cellType == GridCellType.Walkable);

                    if (isJumpableObstacle)
                    {
                        Debug.LogWarning($"[FollowPath] Occupant '{occupant.name}' is stationary on a jumpable tile. Waiting 1s.");
                        yield return new WaitForSeconds(1f);

                        if (grid.GetOccupant(step) == occupant) // Still there
                        {
                            Debug.LogWarning($"[FollowPath] Occupant '{occupant.name}' is still there. Attempting to find a free spot to jump to.");
                            Vector2Int dir = step - gridPos;
                            if (dir == Vector2Int.zero) dir = moveDirection; // Fallback if stuck

                            Vector2Int landingPos = step;
                            bool foundLandingSpot = false;

                            for (int j = 1; j < 10; j++) // Limit search to 10 cells
                            {
                                landingPos += dir;
                                var landingCell = grid.GetCell(landingPos.x, landingPos.y);

                                if (landingCell == null || (landingCell.cellType != GridCellType.Walkable && landingCell.cellType != GridCellType.Stop))
                                {
                                    Debug.LogWarning($"[FollowPath] Chain-jump check hit a non-walkable cell at {landingPos}. Aborting jump.");
                                    break;
                                }

                                if (!grid.IsOccupied(landingPos))
                                {
                                    Debug.LogWarning($"[FollowPath] Found a free landing spot at {landingPos}. Jumping.");
                                    yield return StartCoroutine(JumpToCoroutine(landingPos));

                                    int landingIndexInPath = path.IndexOf(landingPos);
                                    if (landingIndexInPath > i)
                                    {
                                        i = landingIndexInPath - 1;
                                    }

                                    foundLandingSpot = true;
                                    break;
                                }
                                else
                                {
                                    Debug.LogWarning($"[FollowPath] Chain-jump check: cell {landingPos} is also occupied. Checking next one.");
                                }
                            }

                            if (foundLandingSpot)
                            {
                                continue;
                            }
                        }
                        else
                        {
                            yield return StartCoroutine(MoveToCoroutine(step));
                            continue;
                        }
                    }

                    Debug.LogWarning($"[FollowPath] Path at {step} is blocked by a non-jumpable obstacle ('{occupant?.name}' in a {occupiedCell.cellType} cell). Returning to origin.");
                    if (stopIndex != -1) StopManager.Instance.CancelReservation(stopIndex, this);
                    yield return StartCoroutine(GoHome(origin));
                    yield break;
                }
            }

            yield return StartCoroutine(MoveToCoroutine(step));
        }
    }

    System.Collections.IEnumerator MoveAlongThenFollow(List<Vector2Int> straightLeg, List<Vector2Int> pathToStop, int stopIndex, Vector2Int stopPos)
    {
        // Remember the overall origin (slot before any movement)
        Vector2Int overallOrigin = gridPos;

        // First, move along straight leg (if any)
        if (straightLeg != null)
        {
            foreach (var step in straightLeg)
            {
                var c = grid.GetCell(step.x, step.y);
                if (c == null) yield break; 

                if (c.cellType == GridCellType.Blocked || c.cellType == GridCellType.Empty)
                {
                    Debug.LogWarning($"[MoveAlongThenFollow] Düz ilerleme yolu ({step}) bir engel tarafından bloke edildi. Başlangıç konumuna ({overallOrigin}) dönülüyor.");
                    if (stopIndex != -1)
                    {
                        StopManager.Instance.CancelReservation(stopIndex, this);
                    }
                    yield return StartCoroutine(GoHome(overallOrigin));
                    yield break;
                }

                if (grid.IsOccupied(step))
                {
                    var occupant = grid.GetOccupant(step);
                    if (occupant != null) // Null check for safety
                    {
                        Debug.LogWarning($"[MoveAlongThenFollow] Düz ilerleme yolu ({step}) başka bir yolcu ('{occupant.name}') tarafından dolu. Kısa bir süre bekleniyor.");
                        yield return new WaitForSeconds(0.5f); // Wait a bit to see if it clears

                        // Check again
                        if (grid.IsOccupied(step))
                        {
                            Debug.LogWarning($"[MoveAlongThenFollow] Yol ({step}) hala dolu. Başlangıç konumuna dönülüyor.");
                            if (stopIndex != -1)
                            {
                                StopManager.Instance.CancelReservation(stopIndex, this);
                            }
                            yield return StartCoroutine(GoHome(overallOrigin));
                            yield break; // Exit the coroutine
                        }
                    }
                }

                yield return StartCoroutine(MoveToCoroutine(step));
            }
        }

        // Then follow pathToStop (which is computed from the walkable start)
        if (pathToStop != null && pathToStop.Count > 0)
        {
            yield return StartCoroutine(FollowPath(pathToStop, stopIndex, overallOrigin));
        }
        else if (gridPos == stopPos)
        {
            // Path is empty, but we are already at the destination stop. This is not an error.
            // The final arrival confirmation will handle it.
        }
        else if (stopIndex != -1) // Path not found AND we are not at the destination
        {
            StopManager.Instance.CancelReservation(stopIndex, this);
            Debug.LogError($"YOL BULUNAMADI: '{name}' yolcusu, atandığı {stopIndex} nolu durağa ({stopPos}) bir yol bulamadı. Başlangıç konumuna geri dönüyor.");
            yield return StartCoroutine(GoHome(overallOrigin));
            yield break; 
        }

        // On arrival, if we are at stopPos, confirm arrival
        if (gridPos == stopPos && stopIndex != -1)
        {
            StopManager.Instance.ConfirmArrivalAtStop(stopIndex, this);
        }
    }

    // Garantili Geri Dönüş Metodu
    System.Collections.IEnumerator GoHome(Vector2Int origin)
    {
        isMoving = true;

        // First, move back to the origin position smoothly.
        yield return StartCoroutine(MoveToCoroutine(origin));

        // --- NEW Burnout/Drift Animation ---
        Debug.LogWarning($"[GoHome] Returned to {origin}. Performing burnout turn.");

        // 1. Calculate the final target rotation based on moveDirection.
        Vector3 finalDirVector = new Vector3(moveDirection.x, 0, moveDirection.y);
        Quaternion finalRotation = transform.rotation; // Default to current if direction is zero
        if (finalDirVector != Vector3.zero)
        {
            finalRotation = Quaternion.LookRotation(finalDirVector);
        }

        // 2. Perform the 360 spin + correction animation.
        float spinDuration = 0.5f; // Duration for the spin.
        Transform transformToRotate = modelTransform != null ? modelTransform : transform;
        
        // Use DORotate with FastBeyond360 to ensure it makes at least a full spin towards the target rotation.
        yield return transformToRotate.DORotate(finalRotation.eulerAngles + new Vector3(0, 360, 0), spinDuration, RotateMode.FastBeyond360)
            .SetEase(Ease.OutSine)
            .WaitForCompletion();

        // Ensure final rotation is exact.
        transformToRotate.rotation = finalRotation;

        isMoving = false;
    }

    // Jump coroutine using DOTween
    System.Collections.IEnumerator JumpToCoroutine(Vector2Int landingGridPos)
    {
        if (grid == null) yield break;
        var landCell = grid.GetCell(landingGridPos.x, landingGridPos.y);
        if (landCell == null) yield break;
        // Update occupancy map: unregister old and register new landing cell
        grid.UnregisterOccupant(gridPos, this);
        grid.RegisterOccupant(landingGridPos, this);

        Vector3 landWorld = landCell.cellTransform != null ? landCell.cellTransform.position : grid.GetWorldPosition(landingGridPos);
        float jumpPower = 1f;
        float duration = 0.45f;
        // Make sure Y is slightly above current to have a visible arc
        Vector3 target = new Vector3(landWorld.x, transform.position.y, landWorld.z);
        Tween t = transform.DOJump(target, jumpPower, 1, duration).SetEase(Ease.OutQuad);
        yield return t.WaitForCompletion();

        // Arrived: update logical position
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

    // Axis-aligned movement: move on X then Z to avoid diagonal paths
    System.Collections.IEnumerator MoveToWorld(Vector3 target, Vector2Int finalGridPos)
    {
        Vector3 startPos = transform.position;
        Vector3 direction = target - startPos;
        direction.y = 0;

        // Determine target rotation to face the direction of movement
        Quaternion targetRotation = transform.rotation;
        if (direction.sqrMagnitude > 0.01f)
        {
            targetRotation = Quaternion.LookRotation(direction);
        }

        // Calculate durations
        float moveDuration = Vector3.Distance(startPos, target) / moveSpeed;
        float rotationDuration = moveDuration * 0.8f; // Make rotation a bit faster than movement

        // Get the transform to rotate, fallback to the main transform if not set.
        Transform transformToRotate = modelTransform != null ? modelTransform : transform;

        // Create a sequence for smooth, combined animation
        Sequence sequence = DOTween.Sequence();
        
        // Join rotation and movement to happen simultaneously
        sequence.Join(transformToRotate.DORotateQuaternion(targetRotation, rotationDuration).SetEase(Ease.OutQuad));
        sequence.Join(transform.DOMove(target, moveDuration).SetEase(Ease.InOutSine));

        yield return sequence.WaitForCompletion();

        // The logical grid position is updated in the calling coroutine (MoveToCoroutine)
        // to ensure it's handled in one place.
    }

}
