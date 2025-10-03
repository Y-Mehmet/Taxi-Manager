using System.Collections.Generic;
using UnityEngine;

public class MetroCheckpointPath : MonoBehaviour
{
    [Header("Checkpoint listesi (sirali)")]
    public List<Transform> checkpoints = new List<Transform>();

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        for (int i = 0; i < checkpoints.Count - 1; i++)
        {
            if (checkpoints[i] != null && checkpoints[i + 1] != null)
            {
                Gizmos.DrawLine(checkpoints[i].position, checkpoints[i + 1].position);
            }
        }
    }
}