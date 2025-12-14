using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using DG.Tweening;

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

    // MetroManager treni ayarlarken yolcu bindirmeyi duraklatmak için bayrak.
    private static bool isTrainAdjusting = false;

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
        boardingZoneStart = checkpointPath.checkpoints.Count - ResourceManager.Instance.boardingStartIndex;

        // Diğer sistemlerden gelen olayları dinlemeye başla.
        StopManager.OnPassengerArrivedAtStop += HandlePassengerOrWagonChange;
        StopManager.OnStopRegistered += HandleStopRegistered;
        WagonManager.Instance.OnWagonRemoved += HandleWagonRemoved;
        MetroManager.OnTrainAdjustmentStateChanged += HandleTrainAdjustmentStateChanged;
        MetroManager.OnSpeedMultiplierChanged += HandleSpeedChanged;
    }

    void OnDestroy()
    {
        // Bellek sızıntılarını önle.
        StopManager.OnPassengerArrivedAtStop -= HandlePassengerOrWagonChange;
        StopManager.OnStopRegistered -= HandleStopRegistered;
        if (WagonManager.Instance != null)
        {
            WagonManager.Instance.OnWagonRemoved -= HandleWagonRemoved;
        }
        MetroManager.OnTrainAdjustmentStateChanged -= HandleTrainAdjustmentStateChanged;
        MetroManager.OnSpeedMultiplierChanged -= HandleSpeedChanged;
    }

    private static void HandleTrainAdjustmentStateChanged(bool isAdjusting)
    {
        isTrainAdjusting = isAdjusting;
        Debug.Log($"<color=orange>BoardingManager notified: Train adjusting is now {isTrainAdjusting}</color>");
        // If we are no longer adjusting, immediately check for new matches.
        if (!isAdjusting && Instance != null)
        {
            // By clearing the list, we force CheckAvailableWagons to detect a change
            // and re-evaluate boarding, even if the set of colors in the zone is coincidentally the same.
            Instance.availableWagonColors.Clear();
            Instance.CheckAvailableWagons();
        }
    }

    void Update()
    {
        if (isTrainAdjusting) return;
        CheckAvailableWagons();
    }

    /// <summary>
    /// Her frame yolcu alma bölgesindeki vagonları kontrol eder ve renk listesini günceller.
    /// Değişiklik varsa OnAvailableColorsChanged event'ini tetikler.
    /// </summary>
    private void CheckAvailableWagons()
    {
        if (isTrainAdjusting) return;

        // Aktif ve uygun vagonları WagonManager'dan al.
        var activeWagons = WagonManager.Instance.GetActiveWagons();
        
        // Yolcu alma bölgesindeki vagonların renklerini bul.
        var currentColorsInZone = activeWagons
            .Where(wagon => !wagon.IsFull && wagon.GetCurrentCheckpointIndex() >= boardingZoneStart)
            .Select(wagon => wagon.wagonColor)
            .Distinct()
            .ToList();

        // Eski liste ile yeni liste arasında bir fark var mı kontrol et.
        bool hasChanged = !availableWagonColors.All(currentColorsInZone.Contains) || availableWagonColors.Count != currentColorsInZone.Count;

        if (hasChanged)
        {
            availableWagonColors = currentColorsInZone;
            OnAvailableColorsChanged?.Invoke(availableWagonColors);
            TryBoardPassengers();
        }
    }

    private void HandlePassengerOrWagonChange(PassengerGroup passenger, int stopIndex)
    {
        Debug.LogWarning($"🚶 <color=lightblue>PASSENGER ARRIVED AT STOP {stopIndex}:</color> Color={passenger.groupColor}, GroupSize={passenger.GroupSize}, Position={passenger.transform.position}");
        Debug.LogWarning($"🚂 <color=orange>Train Adjusting:</color> {isTrainAdjusting}");
        
        // Force immediate wagon check
        CheckAvailableWagons();
        TryBoardPassengers();
    }
    
    private void HandleStopRegistered()
    {
        Debug.Log($"<color=lightblue>Yeni Durak Eklendi:</color> Eşleştirme kontrolü tetiklendi.");
        TryBoardPassengers();
    }
    
    private void HandleSpeedChanged(string newSpeedText)
    {
        Debug.LogWarning($"⚡ <color=yellow>HIZ DEĞİŞTİ: {newSpeedText}</color> - Wagon listesi temizleniyor ve yeniden kontrol ediliyor.");
        // Clear the list to force a re-check
        availableWagonColors.Clear();
        CheckAvailableWagons();
    }
    
    private void HandleWagonRemoved(MetroWagon removedWagon, Transform removedWagonTransform)
    {
        CheckAvailableWagons();
    }

    /// <summary>
    /// Duraklarda bekleyen yolcuları, uygun vagonlarla eşleştirmeyi dener.
    /// Yolcular gelme zamanına göre sıralanır (ilk gelen önce işlenir).
    /// </summary>
    private void TryBoardPassengers()
    {
        if (isTrainAdjusting)
        {
            Debug.LogWarning("⏸️ <color=red>TryBoardPassengers SKIPPED:</color> Train is adjusting!");
            return;
        }
        
        if (availableWagonColors.Count == 0)
        {
            Debug.LogWarning("⚠️ <color=orange>TryBoardPassengers SKIPPED:</color> No available wagon colors in boarding zone!");
            return;
        }

        Debug.LogWarning($"🎯 <color=cyan>TryBoardPassengers CALLED:</color> Available colors in zone: [{string.Join(", ", availableWagonColors)}]");

        // Get passengers sorted by arrival time (oldest first)
        var waitingPassengers = StopManager.Instance.GetOccupiedStopsSortedByArrivalTime();
        if (waitingPassengers.Count == 0)
        {
            Debug.LogWarning("ℹ️ <color=gray>No waiting passengers at stops.</color>");
            return;
        }

        Debug.LogWarning($"👥 <color=lightblue>Waiting passengers count:</color> {waitingPassengers.Count}");

        foreach (var passengerEntry in waitingPassengers)
        {
            PassengerGroup passenger = passengerEntry.Value;
            int stopIndex = passengerEntry.Key;

            Debug.LogWarning($"🔍 <color=white>Checking passenger at stop {stopIndex}:</color> Color={passenger.groupColor}, GroupSize={passenger.GroupSize}");

            if (availableWagonColors.Contains(passenger.groupColor))
            {
                Debug.LogWarning($"✅ <color=green>Color MATCH found!</color> Searching for {passenger.groupColor} wagon in boarding zone (start index: {boardingZoneStart})...");
                
                MetroWagon availableWagon = WagonManager.Instance.FindWagon(passenger.groupColor, 1, boardingZoneStart);
                
                if (availableWagon != null)
                {
                    Debug.LogWarning($"🎉 <color=cyan>WAGON FOUND!</color> {availableWagon.wagonColor} wagon at checkpoint {availableWagon.GetCurrentCheckpointIndex()}, IsFull={availableWagon.IsFull}");
                    Debug.Log($"<color=cyan>EŞLEŞME BULUNDU:</color> {availableWagon.wagonColor} renkli yük, {passenger.groupColor} renkli işçi tarafından alınıyor.");

                    // Yükü (vagonu) oyundan kaldır.
                    WagonManager.Instance.DeregisterWagon(availableWagon);
                    WagonManager.Instance.TriggerWagonRemovalEvent(availableWagon, availableWagon.transform);

                    // Animate the wagon moving to the passenger, then deactivate it.
                     SoundManager.instance.PlaySfx(SoundType.Slurp);
                    Transform wagonTransform = availableWagon.transform;
                    wagonTransform.SetParent(null); // Unparent to move freely
                    Sequence sequence = DOTween.Sequence();
                    sequence.Append(wagonTransform.DOMove(passenger.transform.position, 0.5f).SetEase(Ease.InQuad));
                    sequence.Join(wagonTransform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InQuad));
                    sequence.OnComplete(() => {
                        availableWagon.gameObject.SetActive(false);
                        Debug.Log($"<color=yellow>YÜK ALINDI:</color> {availableWagon.name} yükü animasyon sonunda kaldırıldı.");
                    });

                    // İşçinin (PassengerGroup) kalan kapasitesini bir azalt.
                    passenger.GroupSize--;
                    Debug.Log($"<color=lightblue>İŞÇİ GÜNCELLENDİ:</color> {passenger.name} işçisinin kalan kapasitesi: {passenger.GroupSize}");

                    // Eğer işçinin kapasitesi dolduysa (yani 0'a ulaştıysa), işçiyi de oyundan kaldır.
                    if (passenger.GroupSize <= 0)
                    {
                        // Notify invoice that passenger completed
                        if (GameManager.Instance != null && GameManager.Instance.CurrentInvoice != null)
                        {
                            GameManager.Instance.CurrentInvoice.OnPassengerCompleted();
                        }
                        
                        // Add to temp resource (20 coins per passenger)
                        if (GameEconomy.Instance != null)
                        {
                            GameEconomy.Instance.AddTempCoins(20);
                            
                            // Show coin animation
                            if (CoinAnimationManager.Instance != null)
                            {
                                CoinAnimationManager.Instance.ShowCoinGain(20, passenger.transform.position);
                            }
                        }
                        
                        // The stop is now free for other passengers.
                        StopManager.Instance.FreeStop(stopIndex);
                        
                        // Start the departure animation instead of deactivating immediately.
                        passenger.PlayDepartureAnimation();

                        Debug.Log($"<color=green>İŞÇİ GÖREVİ TAMAMLADI:</color> {passenger.name} işçisi kapasitesi dolduğu için kaldırıldı ve çıkış animasyonu başladı.");
                        GameManager.Instance.CheckWinCondition(); // Check for win condition after passenger departs
                    }
                    
                    // Continue to check other passengers instead of returning early
                    // This allows multiple passengers to be matched in the same call
                    continue; 
                }
                else
                {
                    Debug.LogError($"❌ <color=red>WAGON NOT FOUND!</color> Color {passenger.groupColor} is in available list but FindWagon returned null! BoardingZoneStart={boardingZoneStart}");
                    
                    // Debug: List all active wagons
                    var allWagons = WagonManager.Instance.GetActiveWagons();
                    Debug.LogError($"📊 <color=yellow>Active wagons count:</color> {allWagons.Count}");
                    foreach (var w in allWagons)
                    {
                        Debug.LogError($"  - Wagon: Color={w.wagonColor}, Checkpoint={w.GetCurrentCheckpointIndex()}, IsFull={w.IsFull}, InBoardingZone={w.GetCurrentCheckpointIndex() >= boardingZoneStart}");
                    }
                }
            }
            else
            {
                Debug.Log($"⏭️ <color=gray>Passenger color {passenger.groupColor} not in available colors. Skipping.</color>");
            }
        }
    }
}
