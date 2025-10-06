
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

/// <summary>
/// Yolcu alma bölgesindeki vagonların renklerini takip eden ve yolcularla vagonları verimli bir şekilde eşleştiren merkezi sistem.
/// </summary>
public class BoardingManager : MonoBehaviour
{
    public static BoardingManager Instance { get; private set; }

    [Header("Bağlantılar")]
    [SerializeField] private MetroCheckpointPath checkpointPath;

    // Event: Yolcu alma bölgesindeki uygun vagon renkleri değiştiğinde tetiklenir.
    public static event Action<List<HyperCasualColor>> OnAvailableColorsChanged;

    // Şu anda yolcu alma bölgesinde bulunan ve dolu olmayan vagonların renklerinin listesi.
    private List<HyperCasualColor> availableWagonColors = new List<HyperCasualColor>();
    
    private int boardingZoneStart;

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

    void Start()
    {
        if (checkpointPath == null)
        {
            Debug.LogError("BoardingManager için CheckpointPath atanmamış!");
            this.enabled = false;
            return;
        }

        // Yolcu alma bölgesinin başlangıç indeksini hesapla.
        boardingZoneStart = checkpointPath.checkpoints.Count - 21;

        // Diğer sistemlerden gelen olayları dinlemeye başla.
        StopManager.OnPassengerArrivedAtStop += HandlePassengerOrWagonChange;
        WagonManager.Instance.OnWagonRemoved += HandleWagonRemoved;
    }

    void OnDestroy()
    {
        // Bellek sızıntılarını önle.
        StopManager.OnPassengerArrivedAtStop -= HandlePassengerOrWagonChange;
        if (WagonManager.Instance != null)
        {
            WagonManager.Instance.OnWagonRemoved -= HandleWagonRemoved;
        }
    }

    void Update()
    {
        CheckAvailableWagons();
    }

    /// <summary>
    /// Her frame yolcu alma bölgesindeki vagonları kontrol eder ve renk listesini günceller.
    /// Değişiklik varsa OnAvailableColorsChanged event'ini tetikler.
    /// </summary>
    private void CheckAvailableWagons()
    {
        // Aktif ve uygun vagonları WagonManager'dan al.
        var activeWagons = WagonManager.Instance.GetActiveWagons();
        
        // Yolcu alma bölgesindeki vagonların renklerini bul.
        var currentColorsInZone = activeWagons
            .Where(wagon => !wagon.isHead && !wagon.IsFull && wagon.GetCurrentCheckpointIndex() >= boardingZoneStart)
            .Select(wagon => wagon.wagonColor)
            .Distinct()
            .ToList();

        // Eski liste ile yeni liste arasında bir fark var mı kontrol et.
        bool hasChanged = !availableWagonColors.All(currentColorsInZone.Contains) || availableWagonColors.Count != currentColorsInZone.Count;

        if (hasChanged)
        {
            availableWagonColors = currentColorsInZone;
            Debug.Log($"<color=lightblue>Yolcu Alma Bölgesi Güncellendi:</color> Mevcut Renkler: {string.Join(", ", availableWagonColors)}");
            
            // Rengi değişen vagonlar olduğu için event'i tetikle.
            OnAvailableColorsChanged?.Invoke(availableWagonColors);
            
            // Eşleştirme mantığını çalıştır.
            TryBoardPassengers();
        }
    }

    private void HandlePassengerOrWagonChange(PassengerGroup passenger, int stopIndex)
    {
        // Bir yolcu durağa vardığında, eşleştirmeyi dene.
        Debug.Log($"<color=lightblue>Yeni Yolcu Geldi:</color> Eşleştirme kontrolü tetiklendi.");
        TryBoardPassengers();
    }
    
    private void HandleWagonRemoved(Transform removedWagonTransform)
    {
        // Bir vagon sistemden kalktığında, anında kontrol tetikle.
        // Update zaten bir sonraki frame'de değişikliği yakalayacak ama bu daha reaktif olmasını sağlar.
        CheckAvailableWagons();
    }

    /// <summary>
    /// Duraklarda bekleyen yolcuları, uygun vagonlarla eşleştirmeyi dener.
    /// </summary>
    private void TryBoardPassengers()
    {
        if (availableWagonColors.Count == 0) return; // Bölgede uygun vagon yoksa denemeye gerek yok.

        var waitingPassengers = StopManager.Instance.GetOccupiedStops();
        if (waitingPassengers.Count == 0) return; // Bekleyen yolcu yoksa denemeye gerek yok.

        // Bekleyen yolcular üzerinden döngüye gir (döngü sırasında liste değişebileceği için kopyasını al).
        foreach (var passengerEntry in waitingPassengers.ToList())
        {
            PassengerGroup passenger = passengerEntry.Value;
            
            // Eğer yolcunun rengi, bölgedeki uygun vagon renklerinden biriyle eşleşiyorsa
            if (availableWagonColors.Contains(passenger.groupColor))
            {
                // Bu renk ve bölgedeki ilk uygun vagonu al.
                MetroWagon availableWagon = WagonManager.Instance.GetAvailableWagon(passenger.groupColor, boardingZoneStart);

                if (availableWagon != null)
                {
                    Debug.Log($"<color=cyan>EŞLEŞME BULUNDU (Yeni Sistem):</color> {availableWagon.wagonColor} vagonu, {passenger.groupColor} renkli yolcuları alıyor.");

                    int stopIndex = passengerEntry.Key;
                    
                    // Aksiyonları gerçekleştir.
                    availableWagon.BoardPassengers(passenger.groupSize);
                    StopManager.Instance.FreeStop(stopIndex);
                    passenger.gameObject.SetActive(false);
                }
            }
        }
    }
}
