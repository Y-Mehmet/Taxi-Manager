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
        WagonManager.Instance.OnWagonRemoved += HandleWagonRemoved;
        MetroManager.OnTrainAdjustmentStateChanged += HandleTrainAdjustmentStateChanged;
    }

    void OnDestroy()
    {
        // Bellek sızıntılarını önle.
        StopManager.OnPassengerArrivedAtStop -= HandlePassengerOrWagonChange;
        if (WagonManager.Instance != null)
        {
            WagonManager.Instance.OnWagonRemoved -= HandleWagonRemoved;
        }
        MetroManager.OnTrainAdjustmentStateChanged -= HandleTrainAdjustmentStateChanged;
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
        Debug.Log($"<color=lightblue>Yeni Yolcu Geldi:</color> Eşleştirme kontrolü tetiklendi.");
        TryBoardPassengers();
    }
    
    private void HandleWagonRemoved(MetroWagon removedWagon, Transform removedWagonTransform)
    {
        CheckAvailableWagons();
    }

    /// <summary>
    /// Duraklarda bekleyen yolcuları, uygun vagonlarla eşleştirmeyi dener.
    /// </summary>
    private void TryBoardPassengers()
    {
        if (isTrainAdjusting) return; 
        if (availableWagonColors.Count == 0) return;

        var waitingPassengers = StopManager.Instance.GetOccupiedStops();
        if (waitingPassengers.Count == 0) return;

        foreach (var passengerEntry in waitingPassengers.ToList())
        {
            PassengerGroup passenger = passengerEntry.Value;
            int stopIndex = passengerEntry.Key;

            if (availableWagonColors.Contains(passenger.groupColor))
            {
                MetroWagon availableWagon = WagonManager.Instance.FindWagon(passenger.groupColor, 1, boardingZoneStart);
                if (availableWagon != null)
                {
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

                    // Add earnings
                    GameManager.Instance.AddLevelEarnings(20);

                    // İşçinin (PassengerGroup) kalan kapasitesini bir azalt.
                    passenger.GroupSize--;
                    Debug.Log($"<color=lightblue>İŞÇİ GÜNCELLENDİ:</color> {passenger.name} işçisinin kalan kapasitesi: {passenger.GroupSize}");

                    // Eğer işçinin kapasitesi dolduysa (yani 0'a ulaştıysa), işçiyi de oyundan kaldır.
                    if (passenger.GroupSize <= 0)
                    {
                        // The stop is now free for other passengers.
                        StopManager.Instance.FreeStop(stopIndex);
                        
                        // Start the departure animation instead of deactivating immediately.
                        passenger.PlayDepartureAnimation();

                        Debug.Log($"<color=green>İŞÇİ GÖREVİ TAMAMLADI:</color> {passenger.name} işçisi kapasitesi dolduğu için kaldırıldı ve çıkış animasyonu başladı.");
                        GameManager.Instance.CheckWinCondition(); // Check for win condition after passenger departs
                    }
                    
                    // Bir eşleşme bulunduktan sonra bu frame için işlemi bitir.
                    return; 
                }
            }
        }
    }
}