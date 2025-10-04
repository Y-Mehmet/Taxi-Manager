
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
    public int groupSize = 4; // 2, 4, 6, 8
    public HyperCasualColor groupColor = HyperCasualColor.Yellow;
    public float moveSpeed = 2f;
    public Vector2Int gridPos; // Şu anki grid pozisyonu
    public PassengerGrid grid;
    [Header("Initialization")]
    [Tooltip("If true, this object will be placed at `gridPos` on Start(). Leave false for manual placement.")]
    public bool useGridPosition = false;
    [Header("Yolcu Prefabı")]
    public GameObject passengerPrefab;
    private List<GameObject> passengers = new List<GameObject>();

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
    }

    // Hareket denemesi ve loglama
    public void TryMoveForwardWithLog()
    {
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
            else if (cell.cellType != GridCellType.Walkable && cell.cellType != GridCellType.Stop && cell.cellType != GridCellType.WaitingArea)
            {
                // Allow stepping into WaitingArea as well — it's not a hard obstacle
                reason = $"Hedef cell engelli veya geçilemez: {cell.cellType}";
            }
            else
            {
                // Diagnostic logging before starting movement
                if (!isMoving)
                {
                    // 1) basic info
                    Debug.LogWarning($"[MoveStart] Passenger '{name}' at {gridPos} moving {moveDirection}");

                    // 2) straight leg indices/types until Walkable reached (or edge)
                    var straight = new List<string>();
                    Vector2Int cursor = gridPos + moveDirection;
                    while (cursor.x >= 0 && cursor.y >= 0 && cursor.x < grid.gridWidth && cursor.y < grid.gridHeight)
                    {
                        var c = grid.GetCell(cursor.x, cursor.y);
                        if (c == null) break;
                        straight.Add($"{cursor}:{c.cellType}");
                        if (c.cellType == GridCellType.Walkable) break;
                        cursor += moveDirection;
                    }
                    Debug.LogWarning("[StraightLeg] " + string.Join(" -> ", straight.ToArray()));

                    // Convert straight strings into Vector2Int positions for movement (available to both reservation and fallback)
                    List<Vector2Int> straightVec = new List<Vector2Int>();
                    Vector2Int scInit = gridPos + moveDirection;
                    Vector2Int sc = scInit;
                    while (sc.x >= 0 && sc.y >= 0 && sc.x < grid.gridWidth && sc.y < grid.gridHeight)
                    {
                        var csc = grid.GetCell(sc.x, sc.y);
                        if (csc == null) break;
                        straightVec.Add(sc);
                        if (csc.cellType == GridCellType.Walkable) break;
                        sc += moveDirection;
                    }

                    // 3) Reserve a stop (if any) and compute path
                        var reservation = grid.ReserveFirstFreeStop(this);
                        if (reservation != null)
                        {
                            var (stopPos, stopIndex) = reservation.Value;

                            // Determine walkable start (cursor currently points to first Walkable or edge)
                            Vector2Int walkableStart = cursor; // cursor was advanced until Walkable or edge
                            if (straight.Count == 0)
                            {
                                // no straight steps, use current gridPos
                                walkableStart = gridPos;
                            }

                            // compute a path to the reserved stop starting from the walkableStart and allow reservation (this)
                            var pathToStop = grid.FindPathToTarget(walkableStart, stopPos, this);

                            // Build combined plan string: straight leg then pathToStop
                            string straightStr = straight.Count > 0 ? string.Join(" -> ", straight.ToArray()) : "(none)";
                            var pathStr = pathToStop != null ? string.Join(" -> ", pathToStop.ConvertAll(p => p.ToString()).ToArray()) : "(no path)";
                            Debug.LogWarning($"[PathPlan] straight: {straightStr} ; pathToStop: {pathStr}");
                            Debug.LogWarning($"[AssignedStop] index={stopIndex} pos={stopPos}");

                            // Start movement: move along straightVec then follow pathToStop
                            StartCoroutine(MoveAlongThenFollow(straightVec, pathToStop, stopIndex, stopPos));
                            return;
                        }

                        // No reservation available: compute fallback path starting from walkableStart
                        Vector2Int fallbackStart = cursor;
                        if (straight.Count == 0) fallbackStart = gridPos;
                        var fallbackPath = grid.FindNearestStopPath(fallbackStart);
                        var fallbackStr = fallbackPath != null ? string.Join(" -> ", fallbackPath.ConvertAll(p => p.ToString()).ToArray()) : "(no path)";
                        Debug.LogWarning($"[PathPlan] planned path (no reservation) from {fallbackStart}: {fallbackStr}");
                        // Move along straightVec then follow the fallback path
                        StartCoroutine(MoveAlongThenFollow(straightVec, fallbackPath, -1, new Vector2Int(-1, -1)));
                        return;
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
        SpawnPassengers();
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
    }

    void SpawnPassengers()
    {
        // Yolcuları grup büyüklüğüne göre spawn et
        for (int i = 0; i < groupSize; i++)
        {
            GameObject p = Instantiate(passengerPrefab, transform);
            // Yolcuları yan yana veya blok şeklinde diz
            float offset = (groupSize - 1) * 0.25f;
            p.transform.localPosition = new Vector3(i * 0.5f - offset, 0, 0);
            passengers.Add(p);
        }
    }

    public void SetGroupColor(HyperCasualColor color)
    {
        foreach (var p in passengers)
        {
            var renderer = p.GetComponentInChildren<Renderer>();
            if (renderer != null)
                renderer.material.color = color.ToColor();
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
        isMoving = false;
    }

    // Move one step and then attempt to find nearest stop and follow path
    System.Collections.IEnumerator MoveThenFindStop(Vector2Int stepPos)
    {
        // Move one step
        yield return StartCoroutine(MoveToCoroutine(stepPos));

        // After arriving, attempt BFS to nearest free stop
        if (grid == null) yield break;
        // Reserve a stop now to avoid race conditions: assign first free stop to this passenger
        var reservation = grid.ReserveFirstFreeStop(this);
        if (reservation != null)
        {
            // Log assignment and then path towards that specific stop
            var (stopPos, stopIndex) = reservation.Value;
            Debug.Log($"Passenger '{name}' assigned to stop index {stopIndex} at {stopPos}");
            // Find path to the reserved stop using targeted pathfinder
            var pathToStop = grid.FindPathToTarget(gridPos, stopPos, this);
            if (pathToStop != null && pathToStop.Count > 0)
            {
                yield return StartCoroutine(FollowPath(pathToStop));
            }
            // When arrived at stop, release reservation and log arrival
            // Note: MoveToCoroutine updates gridPos when arriving, so check if gridPos == stopPos
            if (gridPos == stopPos)
            {
                Debug.Log($"Passenger '{name}' reached stop index {stopIndex} at {stopPos}");
                grid.ReleaseStopReservation(stopIndex, this);
            }
            yield break;
        }

        // If no reservation available, fallback to generic nearest-stop path
        var path = grid.FindNearestStopPath(gridPos);
        if (path != null && path.Count > 0)
        {
            yield return StartCoroutine(FollowPath(path));
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
        var reserved = grid.GetReservedStopFor(this);
        List<Vector2Int> path = null;
        if (reserved != null)
        {
            path = grid.FindPathToTarget(gridPos, reserved.Value.pos, this);
        }
        if (path == null)
            path = grid.FindNearestStopPath(gridPos);
        if (path != null && path.Count > 0)
        {
            yield return StartCoroutine(FollowPath(path));
        }
    }

    // Follow a path given as a list of grid positions (each step will be executed sequentially)
    System.Collections.IEnumerator FollowPath(List<Vector2Int> path)
    {
        // Remember starting position so we can return if blocked
        Vector2Int origin = gridPos;
        foreach (var step in path)
        {
            // Validate target cell
            var targetCell = grid.GetCell(step.x, step.y);
            if (targetCell == null)
            {
                // invalid cell - go back to origin and stop
                yield return StartCoroutine(MoveToCoroutine(origin));
                yield break;
            }

            if (targetCell.cellType == GridCellType.Blocked || targetCell.cellType == GridCellType.Empty)
            {
                // blocked by terrain - return to origin
                yield return StartCoroutine(MoveToCoroutine(origin));
                yield break;
            }

            // If occupied, try jump if allowed, otherwise return to origin
            if (grid.IsOccupied(step))
            {
                var curCell = grid.GetCell(gridPos.x, gridPos.y);
                if (curCell != null && (curCell.cellType == GridCellType.Walkable || curCell.cellType == GridCellType.Stop))
                {
                    Vector2Int dir = step - gridPos;
                    Vector2Int landing = step + dir;
                    var landCell = grid.GetCell(landing.x, landing.y);
                    if (landCell != null && (landCell.cellType == GridCellType.Walkable || landCell.cellType == GridCellType.Stop) && !grid.IsOccupied(landing))
                    {
                        yield return StartCoroutine(JumpToCoroutine(landing));
                        // After jump, prefer our reserved stop when recomputing path
                        var reserved = grid.GetReservedStopFor(this);
                        List<Vector2Int> newPath = null;
                        if (reserved != null)
                        {
                            newPath = grid.FindPathToTarget(gridPos, reserved.Value.pos, this);
                        }
                        if (newPath == null)
                            newPath = grid.FindNearestStopPath(gridPos);
                        if (newPath == null || newPath.Count == 0) { yield return StartCoroutine(MoveToCoroutine(origin)); yield break; }
                        path = newPath;
                        continue;
                    }
                }

                // cannot pass: go back to origin
                yield return StartCoroutine(MoveToCoroutine(origin));
                yield break;
            }

            // Safe to move
            yield return StartCoroutine(MoveToCoroutine(step));
        }
    }

    // Move along straight leg then follow the provided path to the reserved stop
    System.Collections.IEnumerator MoveAlongThenFollow(List<Vector2Int> straightLeg, List<Vector2Int> pathToStop, int stopIndex, Vector2Int stopPos)
    {
        // First, move along straight leg (if any)
        if (straightLeg != null)
        {
            foreach (var step in straightLeg)
            {
                // If occupied or invalid, abort and return to origin
                var c = grid.GetCell(step.x, step.y);
                if (c == null) yield break;
                if (grid.IsOccupied(step)) { StartCoroutine(BounceVisual()); yield break; }
                yield return StartCoroutine(MoveToCoroutine(step));
            }
        }

        // Then follow pathToStop (which is computed from the walkable start)
        if (pathToStop != null && pathToStop.Count > 0)
        {
            yield return StartCoroutine(FollowPath(pathToStop));
        }

        // On arrival, if we are at stopPos, release reservation and log
        if (gridPos == stopPos)
        {
            Debug.LogWarning($"Passenger '{name}' reached reserved stop index {stopIndex} at {stopPos}");
            grid.ReleaseStopReservation(stopIndex, this);
        }
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
        Vector3 start = transform.position;

        // Move along X only
        Vector3 midX = new Vector3(target.x, start.y, start.z);
        if (Mathf.Abs(start.x - target.x) > 0.01f)
        {
            while (Mathf.Abs(transform.position.x - target.x) > 0.01f)
            {
                Vector3 pos = transform.position;
                pos.x = Mathf.MoveTowards(pos.x, target.x, moveSpeed * Time.deltaTime);
                transform.position = pos;
                yield return null;
            }
            // snap X
            var p = transform.position;
            p.x = target.x;
            transform.position = p;
        }

        // Move along Z only
        if (Mathf.Abs(start.z - target.z) > 0.01f)
        {
            while (Mathf.Abs(transform.position.z - target.z) > 0.01f)
            {
                Vector3 pos = transform.position;
                pos.z = Mathf.MoveTowards(pos.z, target.z, moveSpeed * Time.deltaTime);
                transform.position = pos;
                yield return null;
            }
            // snap Z
            var p2 = transform.position;
            p2.z = target.z;
            transform.position = p2;
        }

        // Ensure exact target
        transform.position = new Vector3(target.x, transform.position.y, target.z);

        // Now we arrived: update logical grid position
        gridPos = finalGridPos;
    }
}
