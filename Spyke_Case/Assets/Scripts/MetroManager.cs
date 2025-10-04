using System.Collections.Generic;
using UnityEngine;

public class MetroManager : MonoBehaviour
{
    [Header("Mid vagon renkleri (sıralı)")]
    public List<HyperCasualColor> midWagonColors = new List<HyperCasualColor> { HyperCasualColor.Blue, HyperCasualColor.Red, HyperCasualColor.Green, HyperCasualColor.Yellow, HyperCasualColor.Orange, HyperCasualColor.Purple, HyperCasualColor.Pink, HyperCasualColor.Cyan, HyperCasualColor.Lime, HyperCasualColor.White };
    [Header("Prefablar")]
    public GameObject headPrefab;
    public GameObject midPrefab;
    public GameObject endPrefab;

    [Header("Vagon ayarları")]
    public int midCount = 10;
    [Tooltip("Vagonlar arası mesafe (birim)")]
    public float wagonSpacing = 1.5f;
    public MetroCheckpointPath checkpointPath;

    private List<MetroWagon> wagons = new List<MetroWagon>();

    // Tüm vagonların hareketini kontrol etmek için statik değişken
    public static bool IsMovementStopped { get; private set; }

    public static void StopMovement()
    {
        IsMovementStopped = true;
    }

    void Start()
    {
        if (checkpointPath == null || checkpointPath.checkpoints == null || checkpointPath.checkpoints.Count == 0)
        {
            Debug.LogError("Checkpoint path atanmadı veya boş!");
            return;
        }
        if (headPrefab == null || midPrefab == null || endPrefab == null)
        {
            Debug.LogError("Prefab referansları atanmadı!");
            return;
        }

        // Oyuna başlarken hareketi başlat
        IsMovementStopped = false;

        // HEAD vagonu en önde spawn et
        // Head vagonu en küçük z'de, tail en büyük z'de olacak şekilde spawn et
        Vector3 basePos = checkpointPath.checkpoints[0].position;
        Vector3 forward = (checkpointPath.checkpoints.Count > 1) ?
            (checkpointPath.checkpoints[1].position - checkpointPath.checkpoints[0].position).normalized : Vector3.forward;

        // Head vagonu
        GameObject headObj = Instantiate(headPrefab, basePos, Quaternion.LookRotation(forward));
        MetroWagon headWagon = headObj.GetComponent<MetroWagon>();
        if (headWagon == null)
        {
            Debug.LogError("Head prefabında MetroWagon scripti yok!");
            return;
        }
        // Head vagonu en yakın checkpoint'ten başlat
        headWagon.isHead = true; // Bu vagonun lider olduğunu belirt
        headWagon.Init(checkpointPath, FindClosestCheckpointIndex(headObj.transform.position));
        wagons.Add(headWagon);

        for (int i = 0; i < midCount; i++) // Mid vagonlar
        {
            Vector3 spawnPos = basePos - forward * wagonSpacing * (i + 1);
            GameObject midObj = Instantiate(midPrefab, spawnPos, Quaternion.LookRotation(forward));
            MetroWagon midWagon = midObj.GetComponent<MetroWagon>();
            if (midWagon == null)
            {
                Debug.LogError($"Mid prefabında MetroWagon scripti yok! Index: {i}");
                continue;
            }
            // Her vagonu kendi en yakın checkpoint'inden başlat
            midWagon.Init(checkpointPath, FindClosestCheckpointIndex(midObj.transform.position));
            // Renk ata
            if (midWagonColors != null && midWagonColors.Count > 0)
            {
                int colorIndex = i % midWagonColors.Count;
                var renderer = midWagon.GetComponentInChildren<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = midWagonColors[colorIndex].ToColor();
                }
            }
            wagons.Add(midWagon);
        }

        // Tail vagon
        Vector3 tailPos = basePos - forward * wagonSpacing * (midCount + 1);
        GameObject tailObj = Instantiate(endPrefab, tailPos, Quaternion.LookRotation(forward));
        MetroWagon tailWagon = tailObj.GetComponent<MetroWagon>();
        if (tailWagon == null)
        {
            Debug.LogError("End prefabında MetroWagon scripti yok!");
            return;
        }
        // Tail vagonu kendi en yakın checkpoint'inden başlat
        tailWagon.Init(checkpointPath, FindClosestCheckpointIndex(tailObj.transform.position));
        wagons.Add(tailWagon);
    }

    // Verilen pozisyona en yakın checkpoint'in index'ini bulur.
    private int FindClosestCheckpointIndex(Vector3 position)
    {
        int closestIndex = 0;
        float minDistance = float.MaxValue;

        for (int i = 0; i < checkpointPath.checkpoints.Count; i++)
        {
            float dist = Vector3.Distance(position, checkpointPath.checkpoints[i].position);
            if (dist < minDistance)
            {
                minDistance = dist;
                closestIndex = i;
            }
        }

        // En yakın checkpoint'ten bir sonraki hedef olarak başla, eğer son checkpoint değilse.
        // Bu, vagonun geriye gitmesini engeller.
        return Mathf.Min(closestIndex + 1, checkpointPath.checkpoints.Count - 1);
    }
}