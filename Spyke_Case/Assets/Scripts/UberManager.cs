
using UnityEngine;

/// <summary>
/// Yolculuğunu tamamlayan vagonları yönetir.
/// </summary>
public class UberManager : MonoBehaviour
{
    public static UberManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Yolun sonuna gelen bir vagonu işler.
    /// Vagonun doluymuş gibi aynı kaldırma sürecine girmesini sağlar.
    /// </summary>
    /// <param name="wagon">Yolculuğu biten vagon.</param>
    public void ProcessFinishedWagon(MetroWagon wagon)
    {
        if (wagon == null) return;

        Debug.Log($"<color=magenta>UBER:</color> Wagon '{wagon.name}' has reached the end of the line and is calling an Uber.");

        // Vagonun doluymuş gibi aynı sürece girmesi için WagonManager'a bildir.
        // Bu, vagonun kaldırılmasını ve trenin yeniden ayarlanmasını tetikleyecektir.
        WagonManager.Instance.ReportWagonFilled(wagon);
    }
}
