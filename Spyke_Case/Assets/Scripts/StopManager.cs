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
    /// Bir yolcu için ilk boş durağı rezerve eder.
    /// </summary>
    /// <returns>Rezerve edilen durağın pozisyonu ve indeksi.</returns>
    public (Vector2Int pos, int index)? ReserveFirstFreeStop(PassengerGroup passenger)
    {
        if (passengerGrid == null || passenger == null) return null;

        for (int i = 0; i < passengerGrid.gridData.stopSlots.Count; i++)
        {
            // Eğer durak ne rezerve edilmiş ne de doluysa, bu durağı ata.
            if (!reservedStops.ContainsKey(i) && !occupiedStops.ContainsKey(i))
            {
                var stopPos = passengerGrid.gridData.stopSlots[i];
                reservedStops[i] = passenger; // Rezervasyon listesine ekle
                Debug.Log($"<color=#00FFFF>ATAMA:</color> <color={passenger.groupColor.ToString().ToLower()}>{passenger.groupColor}</color> renkli '{passenger.name}' yolcusu, {i} index'li durağa atandı.");
                return (stopPos, i);
            }
        }

        // --- Hata Ayıklama Logları ---
        // Eğer buraya ulaştıysak, boş durak bulunamamıştır. Nedenini detaylıca loglayalım.
        System.Text.StringBuilder report = new System.Text.StringBuilder();
        report.AppendLine($"[Reserve] Passenger '{passenger.name}' için boş durak bulunamadı. Durak Durum Raporu:");
        report.AppendLine($"Toplam Rezerve Durak: {reservedStops.Count}, Toplam Dolu Durak: {occupiedStops.Count}");
        report.AppendLine($"Kontrol Edilen Toplam Durak Sayısı: {passengerGrid.gridData.stopSlots.Count}, Rezerve: {reservedStops.Count}, Dolu: {occupiedStops.Count}");

        if (reservedStops.Count > 0)
        {
            report.AppendLine("--- Rezerve Edilmiş Duraklar ---");
            foreach (var item in reservedStops)
            {
                report.AppendLine($"Durak [{item.Key}] -> Rezerve Eden: '{item.Value.name}'");
            }
        }

        Debug.LogWarning(report.ToString());
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
    /// Belirtilen yolcu için rezerve edilmiş durağı (pozisyon ve index) döndürür.
    /// </summary>
    public (Vector2Int pos, int index)? GetReservedStopFor(PassengerGroup passenger)
    {
        if (passengerGrid == null || passenger == null) return null;
        foreach (var kvp in reservedStops)
        {
            if (kvp.Value == passenger)
            {
                int stopIndex = kvp.Key;
                if (stopIndex >= 0 && stopIndex < passengerGrid.gridData.stopSlots.Count)
                {
                    return (passengerGrid.gridData.stopSlots[stopIndex], stopIndex);
                }
            }
        }
        return null;
    }

    // --- Gizmo ve Debug için Yardımcı Metotlar ---
    public bool IsStopReserved(int stopIndex) => reservedStops.ContainsKey(stopIndex);
    public bool IsStopOccupied(int stopIndex) => occupiedStops.ContainsKey(stopIndex);
}