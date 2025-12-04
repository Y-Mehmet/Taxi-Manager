using UnityEngine;

public class MetroWagon : MonoBehaviour
{
    public float speed = 5f;
    public float rotationSpeed = 2.0f; // DÃ¶nÃ¼ÅŸ yumuÅŸaklÄ±ÄŸÄ± iÃ§in hÄ±z
    public bool isHead = false; // Bu vagonun lider olup olmadÄ±ÄŸÄ±nÄ± belirtir
    public HyperCasualColor wagonColor { get; private set; }
    public int passengerCount { get; private set; } = 0;
    public int maxPassengerCount = 4; // Maksimum yolcu kapasitesi
    public bool IsFull => passengerCount >= maxPassengerCount;

    private int currentCheckpointIndex = 0;
    private MetroCheckpointPath path;
    private bool isInitialized = false;

    public void Init(MetroCheckpointPath path, int startCheckpointIndex, HyperCasualColor color = HyperCasualColor.White)
    {
        this.path = path;
        this.wagonColor = color;
        currentCheckpointIndex = startCheckpointIndex;
        isInitialized = true;
    }

    void Update()
    {
        if (!isInitialized) return;

        // EÄŸer genel hareket durdurulduysa, hiÃ§bir ÅŸey yapma.
        if (MetroManager.IsMovementStopped || MetroManager.Instance.IsAdjusting())
        {
            return;
        }

        // Her vagon doÄŸrudan checkpoint'ler boyunca ilerler
        if (path != null && path.checkpoints.Count > 0 && currentCheckpointIndex < path.checkpoints.Count)
        {
            var checkpoint = path.checkpoints[currentCheckpointIndex];
            if (!checkpoint)
            {
                Debug.LogError($"HATA: '{path.name}' adlÄ± yoldaki {currentCheckpointIndex}. checkpoint objesi null veya yok edilmiÅŸ. LÃ¼tfen MetroCheckpointPath objesini kontrol et.", this.gameObject);
                MetroManager.StopMovement(); // HatalÄ± yolda hareketi durdur.
                this.enabled = false; // Bu vagonun Update dÃ¶ngÃ¼sÃ¼nÃ¼ kapat.
                return;
            }

            Vector3 target = checkpoint.position;
            MoveTowards(target);

            // Hedefe yeterince yaklaÅŸtÄ±ysak bir sonraki checkpoint'e geÃ§
            if (Vector3.Distance(transform.position, target) < 0.1f)
            {
                currentCheckpointIndex++;
            }
        }
        
        // Yolu tamamlayan vagonlarÄ± iÅŸle.
        if (currentCheckpointIndex >= path.checkpoints.Count)
        {
            // EÄŸer bu vagon Head ise, tÃ¼m trenin hareketini durdur.
            if (isHead)
            {
                MetroManager.StopMovement();
            }

            // Ä°ster Head olsun ister olmasÄ±n, yolu bitiren her vagon Uber'e bildirilir.
            if (UberManager.Instance != null)
            {
                UberManager.Instance.ProcessFinishedWagon(this);
            }
            
            // Bu script'i devre dÄ±ÅŸÄ± bÄ±rak ki tekrar tekrar Ã§aÄŸrÄ±lmasÄ±n.
            this.enabled = false;
        }
    }
    
    public int GetCurrentCheckpointIndex()
    {
        return currentCheckpointIndex;
    }

    public void BoardPassengers(int count)
    {
        passengerCount += count;
        Debug.Log($"<color={wagonColor.ToString().ToLower()}>{wagonColor} vagonuna</color> {count} yolcu bindi. Toplam: {passengerCount}", this.gameObject);

        // EÄŸer vagon dolduysa, durumu WagonManager'a bildir.
        if (IsFull)
        {
            WagonManager.Instance?.ReportWagonFilled(this);
        }
        // Burada yolcularÄ±n vagonda gÃ¶rÃ¼nmesi iÃ§in gÃ¶rsel bir efekt veya animasyon tetiklenebilir.
    }

    /// <summary>
    /// Check if the wagon is currently at (or very near) a checkpoint.
    /// Used to ensure we only disable a wagon while it's parked at a checkpoint.
    /// </summary>
    public bool IsAtCheckpoint(float threshold = 0.15f)
    {
        if (path == null || path.checkpoints == null || path.checkpoints.Count == 0) return true;

        float minDist = float.MaxValue;
        for (int i = 0; i < path.checkpoints.Count; i++)
        {
            float d = Vector3.Distance(transform.position, path.checkpoints[i].position);
            if (d < minDist) minDist = d;
        }
        return minDist <= threshold;
    }

    /// <summary>
    /// Vagonun mevcut checkpoint hedefini gÃ¼nceller.
    /// </summary>
    /// <param name="newIndex">Yeni checkpoint indeksi.</param>
    public void SetTargetCheckpoint(int newIndex)
    {
        currentCheckpointIndex = Mathf.Clamp(newIndex, 0, path.checkpoints.Count);
    }

    public void SetWagonColorProperty(HyperCasualColor newColor)
    {
        wagonColor = newColor;
    }

    public void SetColor(HyperCasualColor newColor)
    {
        wagonColor = newColor;
        
        // Try to load a material from Resources/Materials folder matching the color name
        string materialPath = $"Materials/{newColor.ToString()}";
        Material colorMaterial = Resources.Load<Material>(materialPath);
        
        var renderer = GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            if (colorMaterial != null)
            {
                // Material found in Resources/Materials - use it
                Debug.Log($"[MetroWagon] Using custom material from Resources: {materialPath}");
                renderer.material = colorMaterial;
            }
            else
            {
                // No custom material found - use old system (set color property)
                Debug.Log($"[MetroWagon] No custom material found for {newColor}, using color property");
                renderer.material.color = newColor.ToColor();
            }
        }
    }

    void MoveTowards(Vector3 target)
    {
        // Pozisyonu hedefe doÄŸru sabit hÄ±zla ilerlet
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

        // Rotasyonu hedefe doÄŸru yumuÅŸak bir ÅŸekilde dÃ¶ndÃ¼r
        Vector3 direction = (target - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
}
