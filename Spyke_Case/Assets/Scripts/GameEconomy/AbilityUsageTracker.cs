using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Ability kullanÄ±m sayaÃ§larÄ±nÄ± takip eder
/// Her ability iÃ§in ilk kullanÄ±m 100, sonraki her kullanÄ±mda 2x artar (100, 200, 400, 800...)
/// </summary>
public class AbilityUsageTracker : MonoBehaviour
{
    public static AbilityUsageTracker Instance { get; private set; }

    // Her ability iÃ§in kullanÄ±m sayÄ±sÄ±
    private Dictionary<AbilityType, int> usageCount = new Dictionary<AbilityType, int>();

    // Ä°lk kullanÄ±m maliyeti
    private const int BASE_COST = 100;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // Level baÅŸÄ±nda sayaÃ§larÄ± sÄ±fÄ±rla
        ResetUsageCounts();
    }

    /// <summary>
    /// Level baÅŸÄ±nda tÃ¼m sayaÃ§larÄ± sÄ±fÄ±rla
    /// </summary>
    public void ResetUsageCounts()
    {
        usageCount.Clear();
        /* Debug.Log("[AbilityUsageTracker] Usage counts reset for new level"); */
    }

    /// <summary>
    /// Ability'nin mevcut maliyetini hesapla
    /// Ä°lk kullanÄ±m: 100
    /// 2. kullanÄ±m: 200
    /// 3. kullanÄ±m: 400
    /// 4. kullanÄ±m: 800
    /// </summary>
    public int GetAbilityCost(AbilityType type)
    {
        int currentUsage = GetUsageCount(type);
        
        // 2^currentUsage * BASE_COST
        int cost = BASE_COST * (int)Mathf.Pow(2, currentUsage);
        
        return cost;
    }

    /// <summary>
    /// Ability kullanÄ±ldÄ±ÄŸÄ±nda sayacÄ± artÄ±r
    /// </summary>
    public void OnAbilityUsed(AbilityType type)
    {
        if (!usageCount.ContainsKey(type))
        {
            usageCount[type] = 0;
        }

        usageCount[type]++;
        
        /* Debug.Log($"[AbilityUsageTracker] {type} used {usageCount[type]} times. Next cost: {GetAbilityCost(type)}"); */
    }

    /// <summary>
    /// Ability'nin kaÃ§ kez kullanÄ±ldÄ±ÄŸÄ±nÄ± dÃ¶ndÃ¼r
    /// </summary>
    public int GetUsageCount(AbilityType type)
    {
        if (usageCount.ContainsKey(type))
        {
            return usageCount[type];
        }
        return 0;
    }

    /// <summary>
    /// Ability'nin bir sonraki kullanÄ±m maliyetini dÃ¶ndÃ¼r (kullanÄ±m sonrasÄ±)
    /// </summary>
    public int GetNextCost(AbilityType type)
    {
        int nextUsage = GetUsageCount(type) + 1;
        return BASE_COST * (int)Mathf.Pow(2, nextUsage);
    }
}
