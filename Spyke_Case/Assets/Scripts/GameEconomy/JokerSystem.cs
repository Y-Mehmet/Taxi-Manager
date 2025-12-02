using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Manages joker purchases and effects using stars
/// Only the LAST purchased joker in each category is active
/// </summary>
public class JokerSystem : MonoBehaviour
{
    public static JokerSystem Instance { get; private set; }

    // Joker costs (in stars)
    public const int DOUBLE_BOOKKEEPING_COST = 10;
    public const int BRIBERY_COST = 10;
    public const int HIGH_OPERATING_EXPENSES_COST = 30;
    public const int OFFSHORE_ACCOUNTS_COST = 100;
    public const int COLLISION_INSURANCE_COST = 10;
    public const int OWN_REPAIR_STATION_COST = 100;

    // Joker durations (in game sessions)
    private const int DOUBLE_BOOKKEEPING_DURATION = 10;
    private const int BRIBERY_DURATION = 5;
    private const int HIGH_OPERATING_EXPENSES_DURATION = 20;
    private const int COLLISION_INSURANCE_DURATION = 5;

    // Active joker tracking (only last purchased in each category)
    private JokerType activeTaxJoker = JokerType.None;
    private JokerType activeRepairJoker = JokerType.None;

    // Remaining games for each joker
    private Dictionary<JokerType, int> jokerRemainingGames = new Dictionary<JokerType, int>();

    // Total stars (calculated from ResourceManager)
    private int totalStars = 0;

    // Events
    public static event Action<int> OnTotalStarsChanged;
    public static event Action<JokerType, int> OnJokerCountChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeJokers();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeJokers()
    {
        // Initialize all joker types with 0 remaining games
        foreach (JokerType type in Enum.GetValues(typeof(JokerType)))
        {
            if (type != JokerType.None)
            {
                jokerRemainingGames[type] = 0;
            }
        }
    }

    /// <summary>
    /// Calculate total stars from ResourceManager
    /// </summary>
    public void CalculateTotalStars()
    {
        if (ResourceManager.Instance == null) return;

        int newTotal = 0;
        var levelStars = ResourceManager.Instance.LevelStars;
        
        if (levelStars != null)
        {
            foreach (var stars in levelStars)
            {
                newTotal += stars;
            }
        }

        if (newTotal != totalStars)
        {
            totalStars = newTotal;
            OnTotalStarsChanged?.Invoke(totalStars);
        }
    }

    public int GetTotalStars() => totalStars;

    /// <summary>
    /// Buy a joker with stars
    /// IMPORTANT: Buying a new joker in the same category deactivates the previous one
    /// </summary>
    public bool BuyJoker(JokerType type)
    {
        int cost = GetJokerCost(type);
        
        if (totalStars < cost)
        {
            Debug.LogWarning($"[JokerSystem] Not enough stars to buy {type}. Need: {cost}, Have: {totalStars}");
            return false;
        }

        // Determine category
        bool isTaxJoker = IsTaxJoker(type);
        bool isRepairJoker = IsRepairJoker(type);

        // DEACTIVATE previous joker in the same category
        if (isTaxJoker && activeTaxJoker != JokerType.None)
        {
            Debug.Log($"[JokerSystem] Deactivating previous tax joker: {activeTaxJoker}");
            jokerRemainingGames[activeTaxJoker] = 0;
            OnJokerCountChanged?.Invoke(activeTaxJoker, 0);
        }

        if (isRepairJoker && activeRepairJoker != JokerType.None)
        {
            Debug.Log($"[JokerSystem] Deactivating previous repair joker: {activeRepairJoker}");
            jokerRemainingGames[activeRepairJoker] = 0;
            OnJokerCountChanged?.Invoke(activeRepairJoker, 0);
        }

        // Activate new joker
        if (IsUnlimitedJoker(type))
        {
            jokerRemainingGames[type] = -1; // -1 means unlimited
            Debug.Log($"[JokerSystem] Purchased unlimited joker: {type}");
        }
        else
        {
            int duration = GetJokerDuration(type);
            jokerRemainingGames[type] = duration;
            Debug.Log($"[JokerSystem] Purchased {type} for {duration} games");
        }

        // Set as active joker in category
        if (isTaxJoker)
        {
            activeTaxJoker = type;
        }

        if (isRepairJoker)
        {
            activeRepairJoker = type;
        }

        OnJokerCountChanged?.Invoke(type, jokerRemainingGames[type]);

        // Save data
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.SaveGame();
        }

        return true;
    }

    /// <summary>
    /// Called at the start of each game to decrement joker counters
    /// </summary>
    public void OnGameStarted()
    {
        // Decrement tax joker
        if (activeTaxJoker != JokerType.None && !IsUnlimitedJoker(activeTaxJoker))
        {
            if (jokerRemainingGames[activeTaxJoker] > 0)
            {
                jokerRemainingGames[activeTaxJoker]--;
                OnJokerCountChanged?.Invoke(activeTaxJoker, jokerRemainingGames[activeTaxJoker]);
                
                if (jokerRemainingGames[activeTaxJoker] == 0)
                {
                    Debug.Log($"[JokerSystem] {activeTaxJoker} has expired!");
                    activeTaxJoker = JokerType.None;
                }
            }
        }

        // Decrement repair joker
        if (activeRepairJoker != JokerType.None && !IsUnlimitedJoker(activeRepairJoker))
        {
            if (jokerRemainingGames[activeRepairJoker] > 0)
            {
                jokerRemainingGames[activeRepairJoker]--;
                OnJokerCountChanged?.Invoke(activeRepairJoker, jokerRemainingGames[activeRepairJoker]);
                
                if (jokerRemainingGames[activeRepairJoker] == 0)
                {
                    Debug.Log($"[JokerSystem] {activeRepairJoker} has expired!");
                    activeRepairJoker = JokerType.None;
                }
            }
        }

        // Save after decrementing
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.SaveGame();
        }
    }

    /// <summary>
    /// Check if a joker is currently active
    /// </summary>
    public bool IsJokerActive(JokerType type)
    {
        if (type == JokerType.None) return false;

        // Check if it's the active joker in its category
        if (IsTaxJoker(type))
        {
            return activeTaxJoker == type && jokerRemainingGames[type] != 0;
        }

        if (IsRepairJoker(type))
        {
            return activeRepairJoker == type && jokerRemainingGames[type] != 0;
        }

        return false;
    }

    /// <summary>
    /// Get remaining games for a joker
    /// </summary>
    public int GetJokerRemainingGames(JokerType type)
    {
        if (jokerRemainingGames.ContainsKey(type))
        {
            return jokerRemainingGames[type];
        }
        return 0;
    }

    /// <summary>
    /// Get the cost of a joker in stars
    /// </summary>
    public int GetJokerCost(JokerType type)
    {
        switch (type)
        {
            case JokerType.DoubleBookkeeping: return DOUBLE_BOOKKEEPING_COST;
            case JokerType.Bribery: return BRIBERY_COST;
            case JokerType.HighOperatingExpenses: return HIGH_OPERATING_EXPENSES_COST;
            case JokerType.OffshoreAccounts: return OFFSHORE_ACCOUNTS_COST;
            case JokerType.CollisionInsurance: return COLLISION_INSURANCE_COST;
            case JokerType.OwnRepairStation: return OWN_REPAIR_STATION_COST;
            default: return 0;
        }
    }

    /// <summary>
    /// Get the duration of a joker in games
    /// </summary>
    private int GetJokerDuration(JokerType type)
    {
        switch (type)
        {
            case JokerType.DoubleBookkeeping: return DOUBLE_BOOKKEEPING_DURATION;
            case JokerType.Bribery: return BRIBERY_DURATION;
            case JokerType.HighOperatingExpenses: return HIGH_OPERATING_EXPENSES_DURATION;
            case JokerType.CollisionInsurance: return COLLISION_INSURANCE_DURATION;
            default: return 0;
        }
    }

    /// <summary>
    /// Check if a joker is unlimited (permanent)
    /// </summary>
    private bool IsUnlimitedJoker(JokerType type)
    {
        return type == JokerType.OffshoreAccounts || type == JokerType.OwnRepairStation;
    }

    /// <summary>
    /// Check if a joker is a tax joker
    /// </summary>
    private bool IsTaxJoker(JokerType type)
    {
        return type == JokerType.DoubleBookkeeping ||
               type == JokerType.Bribery ||
               type == JokerType.HighOperatingExpenses ||
               type == JokerType.OffshoreAccounts;
    }

    /// <summary>
    /// Check if a joker is a repair joker
    /// </summary>
    private bool IsRepairJoker(JokerType type)
    {
        return type == JokerType.CollisionInsurance ||
               type == JokerType.OwnRepairStation;
    }

    /// <summary>
    /// Calculate tax rate based on active tax joker
    /// </summary>
    public float GetTaxRate()
    {
        if (activeTaxJoker == JokerType.None)
        {
            return 0.10f; // Default 10% tax
        }

        switch (activeTaxJoker)
        {
            case JokerType.Bribery:
            case JokerType.HighOperatingExpenses:
                return 0f; // 0% tax
            case JokerType.OffshoreAccounts:
                return 0.05f; // 5% tax
            case JokerType.DoubleBookkeeping:
                return 0.10f; // 10% tax
            default:
                return 0.10f; // Default
        }
    }

    /// <summary>
    /// Calculate crash penalty based on active repair joker
    /// </summary>
    public int GetCrashPenalty(int basePenalty)
    {
        if (activeRepairJoker == JokerType.None)
        {
            return basePenalty; // Default penalty (500)
        }

        switch (activeRepairJoker)
        {
            case JokerType.CollisionInsurance:
                return 0; // Zero repair cost
            case JokerType.OwnRepairStation:
                return 100; // Fixed 100 coin repair cost
            default:
                return basePenalty;
        }
    }

    // Save/Load methods
    public void SaveToData(SaveGameData data)
    {
        data.jokerRemainingGames = new Dictionary<int, int>();
        foreach (var kvp in jokerRemainingGames)
        {
            data.jokerRemainingGames[(int)kvp.Key] = kvp.Value;
        }

        data.activeTaxJoker = (int)activeTaxJoker;
        data.activeRepairJoker = (int)activeRepairJoker;
    }

    public void LoadFromData(SaveGameData data)
    {
        if (data.jokerRemainingGames != null)
        {
            jokerRemainingGames.Clear();
            foreach (var kvp in data.jokerRemainingGames)
            {
                jokerRemainingGames[(JokerType)kvp.Key] = kvp.Value;
            }
        }

        activeTaxJoker = (JokerType)data.activeTaxJoker;
        activeRepairJoker = (JokerType)data.activeRepairJoker;

        CalculateTotalStars();
    }
}
