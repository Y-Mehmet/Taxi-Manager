using System.Collections.Generic;
using UnityEngine;

public class MetroWagon : MonoBehaviour
{
    public float speed = 5f;
    public MetroWagon wagonToFollow; // Önündeki vagon (head için null)
    public List<Vector3> pathHistory = new List<Vector3>(); // Sadece head için kullanılır
    [Tooltip("Takip edilen pathHistory aralığı (mesafe ayarı)")]
    public int followDelay = 10; // Kaç adım geriden takip edecek (mid/end için)

    private int currentCheckpointIndex = 0;
    private MetroCheckpointPath path;

    public void Init(MetroCheckpointPath path, int startCheckpoint)
    {
        this.path = path;
        currentCheckpointIndex = startCheckpoint;
        // Başlangıçta pathHistory'yi doldur (beklemesinler)
        pathHistory.Clear();
        for (int i = 0; i <= followDelay; i++)
        {
            pathHistory.Add(transform.position);
        }
    }

    void Update()
    {
        bool moved = false;
        if (wagonToFollow == null)
        {
            // Head vagon: checkpoint'ler boyunca ilerle
            if (path != null && path.checkpoints.Count > 0 && currentCheckpointIndex < path.checkpoints.Count)
            {
                Vector3 target = path.checkpoints[currentCheckpointIndex].position;
                float dist = Vector3.Distance(transform.position, target);
                if (dist > 0.01f)
                {
                    MoveTowards(target);
                    moved = true;
                }
                if (dist < 0.1f)
                {
                    currentCheckpointIndex++;
                }
            }
        }
        else
        {
            // Mid/end vagon: önündeki vagonun pathHistory'sini takip et
            if (wagonToFollow.pathHistory.Count > followDelay)
            {
                Vector3 followPos = wagonToFollow.pathHistory[followDelay];
                float dist = Vector3.Distance(transform.position, followPos);
                if (dist > 0.01f)
                {
                    MoveTowards(followPos);
                    moved = true;
                }
            }
        }
        // Sadece hareket ettiyse pathHistory'ye ekle
        if (moved)
        {
            pathHistory.Insert(0, transform.position);
            if (pathHistory.Count > 1000) pathHistory.RemoveAt(pathHistory.Count - 1);
        }
    }

    void MoveTowards(Vector3 target)
    {
        Vector3 dir = (target - transform.position).normalized;
        transform.position += dir * speed * Time.deltaTime;
        if (dir != Vector3.zero)
        {
            transform.forward = dir;
        }
    }
}