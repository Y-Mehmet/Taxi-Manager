using System.Collections.Generic;
using UnityEngine;

public class MetroManager : MonoBehaviour
{
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
        headWagon.Init(checkpointPath, 1);
        headWagon.wagonToFollow = null;
        wagons.Add(headWagon);

        MetroWagon prevWagon = headWagon;
        // Mid vagonlar
        for (int i = 0; i < midCount; i++)
        {
            Vector3 spawnPos = basePos - forward * wagonSpacing * (i + 1);
            GameObject midObj = Instantiate(midPrefab, spawnPos, Quaternion.LookRotation(forward));
            MetroWagon midWagon = midObj.GetComponent<MetroWagon>();
            if (midWagon == null)
            {
                Debug.LogError($"Mid prefabında MetroWagon scripti yok! Index: {i}");
                continue;
            }
            midWagon.wagonToFollow = prevWagon;
            wagons.Add(midWagon);
            prevWagon = midWagon;
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
        tailWagon.wagonToFollow = prevWagon;
        wagons.Add(tailWagon);
    }
}