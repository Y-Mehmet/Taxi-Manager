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
        GameObject stopParent = GameObject.Find("StopParent");
        if (stopParent == null)
        {
            Debug.LogError("'StopParent' GameObject not found in the scene!");
            // If we failed, refund the ability use
            AddAbility(AbilityType.AddNewStop, 1);
            return;
        }

        Transform parentTransform = stopParent.transform;
        if (parentTransform.childCount > 0)
        {
            Transform lastChild = parentTransform.GetChild(parentTransform.childCount - 1);
            if (!lastChild.gameObject.activeSelf)
            {
                lastChild.gameObject.SetActive(true);

                SpriteRenderer sr = lastChild.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = Color.white;
                }
                Debug.Log("Successfully activated a new stop!");
            }
            else
            {
                Debug.LogWarning("The last stop is already active. No new stop to activate.");
                // Optional: refund if no action was taken
                AddAbility(AbilityType.AddNewStop, 1);
            }
        }
        else
        {
            Debug.LogWarning("'StopParent' has no children to activate.");
            // Optional: refund if no action was taken
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
