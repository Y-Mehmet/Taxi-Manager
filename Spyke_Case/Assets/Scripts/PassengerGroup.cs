
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
            else if (cell.cellType != GridCellType.Walkable && cell.cellType != GridCellType.Stop)
            {
                reason = $"Hedef cell engelli veya geçilemez: {cell.cellType}";
            }
            else
            {
                Debug.Log($"PassengerGroup hareket ediyor. Şu anki slot: {gridPos}, yön: {moveDirection}, hedef: {nextPos}");
                if (!isMoving)
                    StartCoroutine(MoveThenFindStop(nextPos));
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
        var groups = FindObjectsOfType<PassengerGroup>();
        bool occupied = false;
        foreach (var g in groups)
        {
            if (g != null && g != this && g.gridPos == nextPos)
            {
                occupied = true; break;
            }
        }

        if (occupied)
        {
            // Can we jump over? Allowed only if current cell is Walkable or Stop
            var curCell = grid.GetCell(gridPos.x, gridPos.y);
            if (curCell != null && (curCell.cellType == GridCellType.Walkable || curCell.cellType == GridCellType.Stop))
            {
                Vector2Int landing = nextPos + moveDirection; // two steps ahead
                var landCell = grid.GetCell(landing.x, landing.y);
                bool landOccupied = false;
                foreach (var g in groups)
                {
                    if (g != null && g.gridPos == landing) { landOccupied = true; break; }
                }
                if (landCell != null && (landCell.cellType == GridCellType.Walkable || landCell.cellType == GridCellType.Stop) && !landOccupied)
                {
                    if (!isMoving)
                        StartCoroutine(JumpThenFindStop(landing));
                    return;
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
        var path = grid.FindNearestStopPath(gridPos);
        if (path != null && path.Count > 0)
        {
            yield return StartCoroutine(FollowPath(path));
        }
    }

    // Follow a path given as a list of grid positions (each step will be executed sequentially)
    System.Collections.IEnumerator FollowPath(List<Vector2Int> path)
    {
        foreach (var step in path)
        {
            // Before moving, check occupancy again
            var groups = FindObjectsOfType<PassengerGroup>();
            bool blocked = false;
            foreach (var g in groups)
            {
                if (g != null && g != this && g.gridPos == step)
                {
                    blocked = true; break;
                }
            }
            if (blocked)
            {
                // If current cell allows jumping (Walkable/Stop), try to jump over the blocking passenger
                var curCell = grid.GetCell(gridPos.x, gridPos.y);
                if (curCell != null && (curCell.cellType == GridCellType.Walkable || curCell.cellType == GridCellType.Stop))
                {
                    Vector2Int dir = step - gridPos;
                    Vector2Int landing = step + dir;
                    var landCell = grid.GetCell(landing.x, landing.y);
                    bool landOccupied = false;
                    foreach (var g in groups)
                    {
                        if (g != null && g.gridPos == landing) { landOccupied = true; break; }
                    }
                    if (landCell != null && (landCell.cellType == GridCellType.Walkable || landCell.cellType == GridCellType.Stop) && !landOccupied)
                    {
                        // perform jump
                        yield return StartCoroutine(JumpToCoroutine(landing));
                        // recompute path from new position
                        var newPath = grid.FindNearestStopPath(gridPos);
                        if (newPath == null || newPath.Count == 0) yield break;
                        path = newPath;
                        continue;
                    }
                }

                // Try to recompute path from current position
                var updatedPath = grid.FindNearestStopPath(gridPos);
                if (updatedPath == null || updatedPath.Count == 0) yield break;
                path = updatedPath;
                continue;
            }

            yield return StartCoroutine(MoveToCoroutine(step));
        }
    }

    // Jump coroutine using DOTween
    System.Collections.IEnumerator JumpToCoroutine(Vector2Int landingGridPos)
    {
        if (grid == null) yield break;
        var landCell = grid.GetCell(landingGridPos.x, landingGridPos.y);
        if (landCell == null) yield break;

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
