using UnityEngine;

public class MetroWagon : MonoBehaviour
{
    public float speed = 5f;
    public float rotationSpeed = 2.0f; // Dönüş yumuşaklığı için hız
    public bool isHead = false; // Bu vagonun lider olup olmadığını belirtir
    public HyperCasualColor wagonColor { get; private set; }
    public int passengerCount { get; private set; } = 0;
    public int maxPassengerCount = 4; // Maksimum yolcu kapasitesi
    public bool IsFull => passengerCount >= maxPassengerCount;

    private int currentCheckpointIndex = 0;
    private MetroCheckpointPath path;

    public void Init(MetroCheckpointPath path, int startCheckpointIndex, HyperCasualColor color = HyperCasualColor.White)
    {
        this.path = path;
        this.wagonColor = color;
        currentCheckpointIndex = startCheckpointIndex;
    }

    void Update()
    {
        // Eğer genel hareket durdurulduysa, hiçbir şey yapma.
        if (MetroManager.IsMovementStopped || MetroManager.Instance.IsAdjusting())
        {
            return;
        }

        // Her vagon doğrudan checkpoint'ler boyunca ilerler
        if (path != null && path.checkpoints.Count > 0 && currentCheckpointIndex < path.checkpoints.Count)
        {
            Vector3 target = path.checkpoints[currentCheckpointIndex].position;
            MoveTowards(target);

            // Hedefe yeterince yaklaştıysak bir sonraki checkpoint'e geç
            if (Vector3.Distance(transform.position, target) < 0.1f)
            {
                // Yeni bir checkpoint'e ulaştık, yolcu kontrolü yap.
                // Sadece renkli vagonlar yolcu alabilir.
                // BU MANTIK ARTIK METROMANAGER'DA EVENT İLE YÖNETİLİYOR.
                // if (!isHead) MetroManager.Instance?.CheckForPassengers(this, currentCheckpointIndex);
                currentCheckpointIndex++;
            }
        }
        else if (isHead)
        {
            // Eğer bu lider vagonsa ve yolu tamamladıysa, tüm vagonları durdur.
            MetroManager.StopMovement();
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

        // Eğer vagon dolduysa, durumu WagonManager'a bildir.
        if (IsFull)
        {
            WagonManager.Instance?.ReportWagonFilled(this);
        }
        // Burada yolcuların vagonda görünmesi için görsel bir efekt veya animasyon tetiklenebilir.
    }

    /// <summary>
    /// Vagonun mevcut checkpoint hedefini günceller.
    /// </summary>
    /// <param name="newIndex">Yeni checkpoint indeksi.</param>
    public void SetTargetCheckpoint(int newIndex)
    {
        currentCheckpointIndex = Mathf.Clamp(newIndex, 0, path.checkpoints.Count);
    }

    void MoveTowards(Vector3 target)
    {
        // Pozisyonu hedefe doğru sabit hızla ilerlet
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

        // Rotasyonu hedefe doğru yumuşak bir şekilde döndür
        Vector3 direction = (target - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
}