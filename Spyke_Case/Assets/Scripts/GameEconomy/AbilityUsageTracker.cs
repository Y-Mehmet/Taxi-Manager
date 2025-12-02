using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Ability kullanım sayaçlarını takip eder
/// Her ability için ilk kullanım 100, sonraki her kullanımda 2x artar (100, 200, 400, 800...)
/// </summary>
public class AbilityUsageTracker : MonoBehaviour
{
    public static AbilityUsageTracker Instance { get; private set; }

    // Her ability için kullanım sayısı
    private Dictionary<AbilityType, int> usageCount = new Dictionary<AbilityType, int>();

    // İlk kullanım maliyeti
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
        // Level başında sayaçları sıfırla
        ResetUsageCounts();
    }

    /// <summary>
    /// Level başında tüm sayaçları sıfırla
    /// </summary>
    public void ResetUsageCounts()
    {
        usageCount.Clear();
        Debug.Log("[AbilityUsageTracker] Usage counts reset for new level");
    }

    /// <summary>
    /// Ability'nin mevcut maliyetini hesapla
    /// İlk kullanım: 100
    /// 2. kullanım: 200
    /// 3. kullanım: 400
    /// 4. kullanım: 800
    /// </summary>
    public int GetAbilityCost(AbilityType type)
    {
        int currentUsage = GetUsageCount(type);
        
        // 2^currentUsage * BASE_COST
        int cost = BASE_COST * (int)Mathf.Pow(2, currentUsage);
        
        return cost;
    }

    /// <summary>
    /// Ability kullanıldığında sayacı artır
    /// </summary>
    public void OnAbilityUsed(AbilityType type)
    {
        if (!usageCount.ContainsKey(type))
        {
            usageCount[type] = 0;
        }

        usageCount[type]++;
        
        Debug.Log($"[AbilityUsageTracker] {type} used {usageCount[type]} times. Next cost: {GetAbilityCost(type)}");
    }

    /// <summary>
    /// Ability'nin kaç kez kullanıldığını döndür
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
    /// Ability'nin bir sonraki kullanım maliyetini döndür (kullanım sonrası)
    /// </summary>
    public int GetNextCost(AbilityType type)
    {
        int nextUsage = GetUsageCount(type) + 1;
        return BASE_COST * (int)Mathf.Pow(2, nextUsage);
    }
}
