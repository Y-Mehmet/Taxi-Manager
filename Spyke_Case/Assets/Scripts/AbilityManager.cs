using System.Collections.Generic;
using UnityEngine;
using System;

public class AbilityManager : MonoBehaviour
{
    public static AbilityManager Instance { get; private set; }

    public event Action<AbilityType, int> OnAbilityCountChanged;

    private Dictionary<AbilityType, int> abilityInventory = new Dictionary<AbilityType, int>();

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
        if (abilityInventory.ContainsKey(type) && abilityInventory[type] > 0)
        {
            abilityInventory[type]--;
            OnAbilityCountChanged?.Invoke(type, abilityInventory[type]);
            Debug.Log($"Used {type}. You have {abilityInventory[type]} left.");

            switch (type)
            {
                case AbilityType.AddNewStop:
                    ExecuteAddNewStop();
                    break;
                case AbilityType.UniversalPathfinding:
                    // To be implemented
                    break;
                case AbilityType.RemoveWagons:
                    // To be implemented
                    break;
                case AbilityType.ShuffleWagonColors:
                    // To be implemented
                    break;
            }
        }
        else
        {
            Debug.LogWarning($"You don't have the {type} ability.");
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
            // Refund the ability if the manager doesn't exist
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