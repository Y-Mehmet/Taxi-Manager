using UnityEngine;
using System;
using System.Collections.Generic;
using GridSystem;

/// <summary>
/// Durak rezervasyonlarını ve doluluğunu yöneten merkezi sistem.
/// Yolcuların duraklara atanmasını ve varışlarını koordine eder.
/// </summary>
public class StopManager : MonoBehaviour
{
    public static StopManager Instance { get; private set; }

    [Tooltip("Sahnedeki PassengerGrid referansı")]
    public PassengerGrid passengerGrid;
    [Tooltip("Stop noktalarını içeren parent. Stop pozisyonları child Transform'lar olarak burada tutulacak.")]
    public Transform stopsParent;

    // runtime list of stop transforms (ordered by hierarchy)
    private List<Transform> stopTransforms = new List<Transform>();

    // YENİ EVENT: Belirli bir yolcu, belirli bir durağa vardığında tetiklenir.
    public static event Action<PassengerGroup, int> OnPassengerArrivedAtStop;

    // Event: Duraklardaki yolcu listesi değiştiğinde tetiklenir.
    public static event Action OnOccupiedStopsChanged;

    // Hangi durakta hangi yolcunun olduğunu tutar (sadece durağa varmış olanlar).
    private Dictionary<int, PassengerGroup> occupiedStops = new Dictionary<int, PassengerGroup>();

    // Hangi durağın hangi yolcuya rezerve edildiğini tutar (henüz yolda olanlar).
    private Dictionary<int, PassengerGroup> reservedStops = new Dictionary<int, PassengerGroup>();

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

        // Build stopTransforms from parent children (if provided)
        stopTransforms.Clear();
        if (stopsParent != null)
        {
            var comps = stopsParent.GetComponentsInChildren<Transform>(true);
            foreach (var t in comps)
            {
                if (t == stopsParent) continue;
                stopTransforms.Add(t);
            }
        }

        // Yeni event'i dinlemeye başla.
        OnPassengerArrivedAtStop += HandlePassengerArrival;
    }

    void OnDestroy()
    {
        // Bellek sızıntılarını önlemek için event aboneliğini kaldır.
        OnPassengerArrivedAtStop -= HandlePassengerArrival;
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Bir yolcu için ilk boş durağı rezerve eder (stopTransforms kullanır).
    /// Dönen değer: (worldPosition, index) ya da null.
    /// </summary>
    public (Vector3 pos, int index)? ReserveFirstFreeStop(PassengerGroup passenger)
    {
        if (passenger == null) return null;

        // Eğer zaten rezervesi varsa onu döndür
        foreach (var kvp in reservedStops)
        {
            if (kvp.Value == passenger)
            {
                int idx = kvp.Key;
                Vector3 pos = GetStopWorldPosition(idx);
                Debug.LogWarning($"'{passenger.name}' yolcusu zaten {idx} nolu durağı rezerve etmiş. Mevcut rezervasyon kullanılıyor.");
                return (pos, idx);
            }
        }

        // stopTransforms listesinden ilk uygun (aktif ve boş) durağı bul
        for (int i = 0; i < stopTransforms.Count; i++)
        {
            if (reservedStops.ContainsKey(i) || occupiedStops.ContainsKey(i)) continue;
            var t = stopTransforms[i];
            if (t == null) continue;
            if (!t.gameObject.activeInHierarchy) continue; // kapalıysa atla
            reservedStops[i] = passenger;
            Vector3 worldPos = t.position;
            Debug.Log($"<color=#00FFFF>ATAMA:</color> <color={passenger.groupColor.ToString().ToLower()}>{passenger.groupColor}</color> renkli '{passenger.name}' yolcusu, {i} index'li durağa atandı.");
            return (worldPos, i);
        }

        // Debug raporu
        Debug.LogWarning($"[Reserve] Passenger '{passenger.name}' için boş durak bulunamadı. Durak Durum Raporu: Total stops: {stopTransforms.Count}, Reserved: {reservedStops.Count}, Occupied: {occupiedStops.Count}");
        return null;
    }

    /// <summary>
    /// Yolcunun rezerve ettiği durağa ulaştığını onaylar.
    /// Bu metot çağrıldığında durak "dolu" hale gelir ve event tetiklenir.
    /// </summary>
    public void ConfirmArrivalAtStop(int stopIndex, PassengerGroup passenger)
    {
        // Rezervasyonu kaldır ve doluluk listesine ekle.
        if (reservedStops.ContainsKey(stopIndex) && reservedStops[stopIndex] == passenger)
        {
            reservedStops.Remove(stopIndex);
            occupiedStops[stopIndex] = passenger;

            Debug.Log($"<color=lime>VARIŞ:</color> <color={passenger.groupColor.ToString().ToLower()}>{passenger.groupColor}</color> renkli '{passenger.name}' yolcusu, {stopIndex} index'li durağa ulaştı. Durak şimdi dolu.");

            // YENİ YAPI: Doğrudan genel event'i çağırmak yerine, spesifik varış event'ini tetikle.
            OnPassengerArrivedAtStop?.Invoke(passenger, stopIndex);
        }
        else
        {
            // Eğer bir yolcu rezerve etmediği bir durağa vardığını iddia ederse, bu bir hatadır.
            string reservedBy = "kimse";
            if (reservedStops.ContainsKey(stopIndex))
            {
                reservedBy = reservedStops[stopIndex].name;
            }
            Debug.LogError($"[HATA] '{passenger.name}' yolcusu, rezerve etmediği {stopIndex} nolu durağa vardığını bildirdi. Bu durak '{reservedBy}' tarafından rezerve edilmişti.");
        }
    }

    /// <summary>
    /// Bir yolcunun durağa varması olayını işler ve genel durum değişikliği event'ini tetikler.
    /// </summary>
    private void HandlePassengerArrival(PassengerGroup passenger, int stopIndex)
    {
        // Bu metot, OnPassengerArrivedAtStop event'i tarafından çağrılır.
        OnOccupiedStopsChanged?.Invoke();
    }

    /// <summary>
    /// Bir yolcu trene bindiğinde durağı tamamen boşaltır.
    /// </summary>
    public void FreeStop(int stopIndex)
    {
        if (occupiedStops.Remove(stopIndex))
        {
            Debug.Log($"Durak {stopIndex} serbest bırakıldı ve yeni yolcular için hazır.");
            OnOccupiedStopsChanged?.Invoke();
        }
    }

    /// <summary>
    /// Bir yolcunun hedefine ulaşamaması durumunda rezervasyonunu iptal eder.
    /// </summary>
    public void CancelReservation(int stopIndex, PassengerGroup passenger)
    {
        if (reservedStops.TryGetValue(stopIndex, out var owner) && owner == passenger)
        {
            reservedStops.Remove(stopIndex);
            Debug.LogWarning($"<color=orange>İPTAL:</color> '{passenger.name}' yolcusunun {stopIndex} nolu durak rezervasyonu, hedefe ulaşılamadığı için iptal edildi.");
        }
    }

    /// <summary>
    /// Sistemde herhangi bir boş durak olup olmadığını kontrol eder.
    /// </summary>
    public bool HasAvailableStops()
    {
        if (passengerGrid == null || passengerGrid.gridData == null) return false;
        int totalStopCount = passengerGrid.gridData.stopSlots.Count;
        int unavailableStopCount = reservedStops.Count + occupiedStops.Count;
        return unavailableStopCount < totalStopCount;
    }

    /// <summary>
    /// Belirtilen durakta bekleyen bir yolcu olup olmadığını döndürür.
    /// </summary>
    public PassengerGroup GetPassengerAtStop(int stopIndex)
    {
        occupiedStops.TryGetValue(stopIndex, out var passenger);
        return passenger;
    }

    /// <summary>
    /// Duraklarda bekleyen tüm yolcuların bir kopyasını döndürür.
    /// </summary>
    public Dictionary<int, PassengerGroup> GetOccupiedStops()
    {
        return new Dictionary<int, PassengerGroup>(occupiedStops);
    }

    /// <summary>
    /// Belirtilen yolcu için rezerve edilmiş durağı (world pozisyon ve index) döndürür.
    /// </summary>
    public (Vector3 pos, int index)? GetReservedStopFor(PassengerGroup g)
    {
        if (g == null) return null;
        foreach (var kvp in reservedStops)
        {
            if (kvp.Value == g)
            {
                int idx = kvp.Key;
                return (GetStopWorldPosition(idx), idx);
            }
        }
        return null;
    }

    public Vector3 GetStopWorldPosition(int index)
    {
        if (index < 0 || index >= stopTransforms.Count) return Vector3.zero;
        var t = stopTransforms[index];
        return t != null ? t.position : Vector3.zero;
    }

    // --- Gizmo ve Debug için Yardımcı Metotlar ---
    public bool IsStopReserved(int stopIndex) => reservedStops.ContainsKey(stopIndex);
    public bool IsStopOccupied(int stopIndex) => occupiedStops.ContainsKey(stopIndex);
}