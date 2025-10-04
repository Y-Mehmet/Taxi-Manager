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
    public event Action<Transform> OnWagonRemoved;

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
        Debug.Log($"<color=orange>VAGON DOLDU:</color> {wagon.wagonColor} renkli vagon kapasitesine ulaştı ve devre dışı bırakıldı.");
        OnWagonFilled?.Invoke(wagon);

        Transform removedWagonTransform = wagon.transform;
        // Vagonu deaktif et ve listeden kaldır.
        wagon.gameObject.SetActive(false);
        allWagons.Remove(wagon);
        OnWagonRemoved?.Invoke(removedWagonTransform);
        // Burada vagonun görünümünü değiştirecek bir işlem yapılabilir (örn. ışıkları söndürmek).
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
            !wagon.isHead &&                  // Lider vagon olmamalı.
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
}