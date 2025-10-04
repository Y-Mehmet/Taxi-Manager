using UnityEngine;

public class MetroWagon : MonoBehaviour
{
    public float speed = 5f;
    public float rotationSpeed = 2.0f; // Dönüş yumuşaklığı için hız
    public bool isHead = false; // Bu vagonun lider olup olmadığını belirtir

    private int currentCheckpointIndex = 0;
    private MetroCheckpointPath path;
    public void Init(MetroCheckpointPath path, int startCheckpointIndex)
    {
        this.path = path;
        currentCheckpointIndex = startCheckpointIndex;
    }

    void Update()
    {
        // Eğer genel hareket durdurulduysa, hiçbir şey yapma.
        if (MetroManager.IsMovementStopped)
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
                currentCheckpointIndex++;
            }
        }
        else if (isHead)
        {
            // Eğer bu lider vagonsa ve yolu tamamladıysa, tüm vagonları durdur.
            MetroManager.StopMovement();
        }
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