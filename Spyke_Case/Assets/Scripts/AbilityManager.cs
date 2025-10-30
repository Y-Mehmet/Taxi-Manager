using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;


public class AbilityManager : MonoBehaviour
{
    public static AbilityManager Instance { get; private set; }

    public event Action<AbilityType, int> OnAbilityCountChanged;

    private Dictionary<AbilityType, int> abilityInventory = new Dictionary<AbilityType, int>();

    private bool IsUniversalPathfindingModeActive = false;
    private bool IsCraneModeActive = false;
    public bool IsAbilityModeActive => IsUniversalPathfindingModeActive || IsCraneModeActive;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
           // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.OnDataLoaded += LoadData;
            LoadData(GameDataManager.Instance.GetSaveData());
        }
    }

    private void OnDestroy()
    {
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.OnDataLoaded -= LoadData;
        }
    }

    public void LoadData(SaveGameData data)
    {
        if (data == null) return;

        abilityInventory.Clear();

        // Use AddAbility to ensure OnAbilityCountChanged event is triggered for UI updates
        if (data.abilityUniversalPathfindingCount > 0) AddAbility(AbilityType.UniversalPathfinding, data.abilityUniversalPathfindingCount);
        if (data.abilityRemoveWagonsCount > 0) AddAbility(AbilityType.RemoveWagons, data.abilityRemoveWagonsCount);
        if (data.abilityAddNewStopCount > 0) AddAbility(AbilityType.AddNewStop, data.abilityAddNewStopCount);
        if (data.abilityShuffleWagonColorsCount > 0) AddAbility(AbilityType.ShuffleWagonColors, data.abilityShuffleWagonColorsCount);
    }

    public void SaveData(SaveGameData data)
    {
        if (data == null) return;

        data.abilityUniversalPathfindingCount = GetAbilityCount(AbilityType.UniversalPathfinding);
        data.abilityRemoveWagonsCount = GetAbilityCount(AbilityType.RemoveWagons);
        data.abilityAddNewStopCount = GetAbilityCount(AbilityType.AddNewStop);
        data.abilityShuffleWagonColorsCount = GetAbilityCount(AbilityType.ShuffleWagonColors);
    }

    public bool BuyAbility(AbilityType type, int cost)
    {
        if (ResourceManager.Instance.SpendCoins(cost))
        {
            AddAbility(type);
            return true;
        }
        else
        {
            Debug.LogWarning($"Not enough coins to buy {type}.");
            return false;
        }
    }

    public void AddAbility(AbilityType type, int count = 1)
    {
        if (abilityInventory.ContainsKey(type))
        {
            abilityInventory[type] += count;
        }
        else
        {
            abilityInventory.Add(type, count);
        }
        OnAbilityCountChanged?.Invoke(type, abilityInventory[type]);
        Debug.Log($"Added {count} of {type}. You now have {abilityInventory[type]}.");
    }

    public void UseAbility(AbilityType type)
    {
        Debug.LogWarning($"<color=magenta>ability is active:</color>  [AbilityManager] UI button clicked for ability: {type}");

        if (!abilityInventory.ContainsKey(type) || abilityInventory[type] <= 0)
        {
            Debug.LogWarning($"You don't have the {type} ability.");
            return;
        }

        // Abilities that enter a mode and wait for input
        switch (type)
        {
            case AbilityType.UniversalPathfinding:
                if (IsUniversalPathfindingModeActive) CancelAbilityMode();
                else EnterUniversalPathfindingMode();
                return;
            case AbilityType.RemoveWagons:
                if (IsCraneModeActive) CancelAbilityMode();
                else EnterCraneMode();
                return;
        }

        // Abilities that execute immediately
        ConsumeAbility(type);
        switch (type)
        {
            case AbilityType.AddNewStop:
                ExecuteAddNewStop();
                break;
            case AbilityType.ShuffleWagonColors:
                MetroManager.Instance.ShuffleWagonColors();
                break;
        }
    }

    private void ConsumeAbility(AbilityType type)
    {
        if (abilityInventory.ContainsKey(type) && abilityInventory[type] > 0)
        {
            abilityInventory[type]--;
            OnAbilityCountChanged?.Invoke(type, abilityInventory[type]);
            Debug.Log($"[AbilityManager] ABILITY CONSUMED: {type}. Remaining: {abilityInventory[type]}.");
        }
        else
        {
            Debug.LogWarning($"[AbilityManager] Tried to consume {type}, but none are in inventory.");
        }
    }

    private void EnterUniversalPathfindingMode()
    {
        CancelAbilityMode(); // Cancel any other active modes first
        IsUniversalPathfindingModeActive = true;
        InputManager.OnPassengerGroupTapped += OnPassengerSelectedForUniversalPathfinding;
        Debug.Log("[AbilityManager] Universal Pathfinding mode ACTIVE. Select a passenger.");
    }

    private void OnPassengerSelectedForUniversalPathfinding(PassengerGroup selectedPassenger)
    {
        Debug.Log($"Passenger '{selectedPassenger.name}' selected for Universal Pathfinding.");
        CancelAbilityMode();
        ConsumeAbility(AbilityType.UniversalPathfinding);
        selectedPassenger.TryUniversalMove();
    }

    private void EnterCraneMode()
    {
        CancelAbilityMode(); // Cancel any other active modes first
        IsCraneModeActive = true;
        InputManager.OnPassengerGroupTapped += OnPassengerSelectedForCrane;
        Debug.Log("[AbilityManager] Crane mode ACTIVE. Select a passenger group at a stop.");
    }

    private void OnPassengerSelectedForCrane(PassengerGroup selectedPassenger)
    {
        if (StopManager.Instance == null)
        {
            Debug.LogError("[AbilityManager] StopManager not found. Cannot perform crane ability.");
            CancelAbilityMode();
            return;
        }

        bool isAtStop = StopManager.Instance.GetOccupiedStops().ContainsValue(selectedPassenger);

        if (isAtStop)
        {
            // Evict from the stop first
            StopManager.Instance.EvictPassenger(selectedPassenger);

            bool handled = false;
            // Scenario 1: Passenger is from an Underpass
            if (selectedPassenger.OriginUnderpass != null)
            {
                Debug.Log($"[AbilityManager] Recalling passenger '{selectedPassenger.name}' to its Underpass.");
                selectedPassenger.OriginUnderpass.ReturnPassengerToEndOfQueue(selectedPassenger);
                handled = true;
            }
            // Scenario 2: Passenger is from a Conveyor
            else if (selectedPassenger.fromConveyor)
            {
                Debug.Log($"[AbilityManager] Recalling passenger '{selectedPassenger.name}' to the Conveyor Belt.");
                if (ConveyorBelt.Instance != null)
                {
                    ConveyorBelt.Instance.AddPassengerToEmptySlot(selectedPassenger);
                    handled = true;
                }
                else
                {
                    Debug.LogError("[AbilityManager] ConveyorBelt instance not found! Cannot return passenger.");
                }
            }

            // Fallback for any other passenger type
            if (!handled)
            {
                Debug.LogWarning($"[AbilityManager] Passenger '{selectedPassenger.name}' has an unknown origin. Using generic ReturnToOrigin().");
                selectedPassenger.ReturnToOrigin();
            }

            // Consume the ability and exit the mode
            CancelAbilityMode();
            ConsumeAbility(AbilityType.RemoveWagons);
        }
        else
        {
            Debug.LogWarning($"[AbilityManager] Invalid target for Crane. '{selectedPassenger.name}' is not at a stop. Please select another.");
            // Do not cancel mode, allow user to select another passenger.
        }
    }

    public void CancelAbilityMode()
    {
        if (IsUniversalPathfindingModeActive)
        {
            IsUniversalPathfindingModeActive = false;
            InputManager.OnPassengerGroupTapped -= OnPassengerSelectedForUniversalPathfinding;
            Debug.Log("[AbilityManager] Universal Pathfinding mode CANCELED.");
        }
        if (IsCraneModeActive)
        {
            IsCraneModeActive = false;
            InputManager.OnPassengerGroupTapped -= OnPassengerSelectedForCrane;
            Debug.Log("[AbilityManager] Crane mode CANCELED.");
        }
    }

    private void ExecuteAddNewStop()
    {
        if (StopManager.Instance != null)
        {
            StopManager.Instance.ActivateNextStop();
        }
        else
        {
            Debug.LogError("StopManager instance not found! Cannot execute AddNewStop ability.");
            AddAbility(AbilityType.AddNewStop, 1);
        }
    }

    public int GetAbilityCount(AbilityType type)
    {
        if (abilityInventory.ContainsKey(type))
        {
            return abilityInventory[type];
        }
        return 0;
    }
}
