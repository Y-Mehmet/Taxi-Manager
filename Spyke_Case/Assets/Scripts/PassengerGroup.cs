
using UnityEngine;
using System.Collections.Generic;

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
                MoveTo(nextPos);
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
        // Sadece Walkable veya Stop ise ilerlenebilir
        if (cell.cellType == GridCellType.Walkable || cell.cellType == GridCellType.Stop)
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
        if (CanMoveForward())
        {
            MoveTo(gridPos + moveDirection);
        }
    }


    void Start()
    {
        SpawnPassengers();
        SetGroupColor(groupColor);
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
    public void MoveTo(Vector2Int newGridPos)
    {
        if (grid == null) return;
        var cell = grid.GetCell(newGridPos.x, newGridPos.y);
        if (cell != null && cell.cellType == GridCellType.Walkable)
        {
            gridPos = newGridPos;
            Vector3 worldTarget = cell.cellTransform != null ? cell.cellTransform.position : new Vector3(newGridPos.x * grid.cellSize, 0, newGridPos.y * grid.cellSize);
            StartCoroutine(MoveToWorld(worldTarget));
        }
    }

    System.Collections.IEnumerator MoveToWorld(Vector3 target)
    {
        while (Vector3.Distance(transform.position, target) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = target;
    }
}
