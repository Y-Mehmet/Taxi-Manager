using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Vagonların durumunu (dolu/boş, pozisyon vb.) yöneten ve uygun vagonları bulan merkezi sistem.
/// </summary>
public class WagonManager : MonoBehaviour
{
    public static WagonManager Instance { get; private set; }

    // Event: Bir vagon dolduğunda tetiklenir.
    public event Action<MetroWagon> OnWagonFilled;

    // YENİ EVENT: Bir vagon sistemden kaldırıldığında (deaktif edildiğinde) tetiklenir.
    public event Action<MetroWagon, Transform> OnWagonRemoved;

    // Pending removals to avoid starting multiple coroutines for same wagon
    private HashSet<MetroWagon> pendingRemovals = new HashSet<MetroWagon>();

    private readonly List<MetroWagon> allWagons = new List<MetroWagon>();

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
    /// Sisteme yeni bir vagon kaydeder. MetroManager tarafından çağrılır.
    /// </summary>
    public void RegisterWagon(MetroWagon wagon)
    {
        if (!allWagons.Contains(wagon))
        {
            allWagons.Add(wagon);
        }
    }

    /// <summary>
    /// Bir vagonun dolduğunu bildirir ve ilgili event'i tetikler.
    /// </summary>
    public void ReportWagonFilled(MetroWagon wagon)
    {
        Debug.LogWarning($"WagonManager: VAGON DOLDU -> Enqueue removal wait for wagon '{wagon?.name ?? "null"}' at pos {wagon?.transform.position}");
        OnWagonFilled?.Invoke(wagon);

        if (wagon == null) return;

        // If already pending, don't start another waiter
        if (pendingRemovals.Contains(wagon)) return;
        pendingRemovals.Add(wagon);
        // Start coroutine to wait until wagon arrives at a checkpoint then remove it
        StartCoroutine(WaitAndRemoveWagon(wagon));
    }

    private System.Collections.IEnumerator WaitAndRemoveWagon(MetroWagon wagon)
    {
        float timeout = 5f; // safety timeout
        float elapsed = 0f;
        // Wait until wagon is at/near a checkpoint
        while (wagon != null && !wagon.IsAtCheckpoint() && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (wagon != null)
        {
            Transform removedWagonTransform = wagon.transform;
            Debug.LogWarning($"WagonManager: Removing wagon '{wagon.name}' at pos {removedWagonTransform.position} after waiting {elapsed:F2}s (timeout {timeout}s)");
            
            // Invoke the event BEFORE deactivating/removing, so listeners can inspect the wagon.
            OnWagonRemoved?.Invoke(wagon, removedWagonTransform);

            wagon.gameObject.SetActive(false);
            allWagons.Remove(wagon);
        }

        pendingRemovals.Remove(wagon);
    }

    /// <summary>
    /// Belirtilen renkte, kapasitesi dolmamış ve yolcu alma bölgesinde olan ilk uygun vagonu bulur.
    /// </summary>
    /// <param name="color">Aranan vagon rengi.</param>
    /// <param name="boardingZoneStart">Yolcu alma bölgesinin başlangıç checkpoint indeksi.</param>
    /// <returns>Uygun bir vagon veya null.</returns>
    public MetroWagon GetAvailableWagon(HyperCasualColor color, int boardingZoneStart)
    {
        // LINQ kullanarak hem verimli hem de okunaklı bir arama yapalım.
        return allWagons.FirstOrDefault(wagon =>
            !wagon.IsFull &&                  // Kapasitesi dolu olmamalı.
            wagon.wagonColor == color &&      // Rengi eşleşmeli.
            wagon.GetCurrentCheckpointIndex() >= boardingZoneStart // Yolcu alma bölgesinde olmalı.
        );
    }

    /// <summary>
    /// Aktif olan tüm vagonların sıralı bir listesini döndürür.
    /// </summary>
    public List<MetroWagon> GetActiveWagons()
    {
        return new List<MetroWagon>(allWagons);
    }

    /// <summary>
    /// Bir vagonu aktif vagonlar listesinden kaldırır. UberManager tarafından kullanılır.
    /// </summary>
    public void DeregisterWagon(MetroWagon wagon)
    {
        if (wagon != null)
        {
            allWagons.Remove(wagon);
        }
    }

    /// <summary>
    /// OnWagonRemoved olayını dışarıdan tetiklemek için kullanılır. UberManager tarafından kullanılır.
    /// </summary>
    public void TriggerWagonRemovalEvent(MetroWagon wagon, Transform transform)
    {
        OnWagonRemoved?.Invoke(wagon, transform);
    }

    /// <summary>
    /// Verilen bir renk listesini, bitişik aynı renkleri bir arada tutarak gruplar halinde karıştırır.
    /// Örnek: [R, R, B, Y, Y] -> Gruplar: ([R,R], [B], [Y,Y]) -> Karışmış Gruplar: ([Y,Y], [R,R], [B]) -> Sonuç: [Y, Y, R, R, B]
    /// </summary>
    /// <param name="originalColors">Karıştırılacak orijinal renk listesi.</param>
    /// <returns>Gruplar halinde karıştırılmış yeni renk listesi.</returns>
    public static List<HyperCasualColor> ShuffleColorGroups(List<HyperCasualColor> originalColors)
    {
        if (originalColors == null || originalColors.Count == 0)
        {
            return new List<HyperCasualColor>();
        }

        // 1. Renkleri gruplara ayır
        List<List<HyperCasualColor>> colorGroups = new List<List<HyperCasualColor>>();
        if (originalColors.Count > 0)
        {
            colorGroups.Add(new List<HyperCasualColor> { originalColors[0] });
            for (int i = 1; i < originalColors.Count; i++)
            {
                if (originalColors[i] == originalColors[i - 1])
                {
                    colorGroups.Last().Add(originalColors[i]);
                }
                else
                {
                    colorGroups.Add(new List<HyperCasualColor> { originalColors[i] });
                }
            }
        }

        // 2. Grupları karıştır
        // System.Linq ve System.Guid kullanarak basit bir karıştırma
        var shuffledGroups = colorGroups.OrderBy(x => Guid.NewGuid()).ToList();

        // 3. Karıştırılmış grupları tek bir listeye düzleştir
        List<HyperCasualColor> finalColors = shuffledGroups.SelectMany(group => group).ToList();

        return finalColors;
    }
}