using System.Collections.Generic;
using UnityEngine;
using System;

public class AbilityManager : MonoBehaviour
{
    public static AbilityManager Instance { get; private set; }

    public event Action<AbilityType, int> OnAbilityCountChanged;

    private Dictionary<AbilityType, int> abilityInventory = new Dictionary<AbilityType, int>();

    public bool IsAbilityModeActive { get; private set; } = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
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

        if (type == AbilityType.UniversalPathfinding)
        {
            if (IsAbilityModeActive)
            {
                CancelAbilityMode();
            }
            else
            {
                EnterUniversalPathfindingMode();
            }
            return;
        }

        ConsumeAbility(type);

        switch (type)
        {
            case AbilityType.AddNewStop:
                ExecuteAddNewStop();
                break;
            case AbilityType.RemoveWagons:
                // To be implemented
                break;
            case AbilityType.ShuffleWagonColors:
                // To be implemented
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
        IsAbilityModeActive = true;
        InputManager.OnPassengerGroupTapped += OnPassengerSelectedForAbility;
        Debug.Log("[AbilityManager] Universal Pathfinding mode ACTIVE. Select a passenger.");
    }

    private void OnPassengerSelectedForAbility(PassengerGroup selectedPassenger)
    {
        Debug.Log($"[AbilityManager] Passenger '{selectedPassenger.name}' selected for Universal Pathfinding.");

        CancelAbilityMode();

        ConsumeAbility(AbilityType.UniversalPathfinding);

        selectedPassenger.TryUniversalMove();
    }

    public void CancelAbilityMode()
    {
        if (IsAbilityModeActive)
        {
            IsAbilityModeActive = false;
            InputManager.OnPassengerGroupTapped -= OnPassengerSelectedForAbility;
            Debug.Log("[AbilityManager] Ability selection mode CANCELED.");
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