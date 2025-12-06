using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Remove Wagons (Flasher) ability tutorial implementation.
/// Shows 3 wagon panels that get removed one by one.
/// </summary>
public class RemoveWagonsTutorial : MonoBehaviour, IAbilityTutorial
{
    [Header("Wagon Panels")]
    [SerializeField] private Transform wagonContainer; // Wagon panellerinin parent'ı
    
    [Header("Cost Display")]
    [SerializeField] private TextMeshProUGUI costText; // Maliyet göstergesi
    
    [Header("Settings")]
    [SerializeField] private AbilityType abilityType = AbilityType.RemoveWagons;
    [SerializeField] private string abilityName = "Remove Wagons";
    [SerializeField] private string description = "Remove a specific passenger group from the map. Use this to clear space when you're stuck.\n\n💰 Cost increases with each use:\n1st: 100 | 2nd: 200 | 3rd: 400 | 4th: 800 Coins";
    
    private GameObject[] wagonPanels;
    private int currentWagonIndex = 0;
    private int maxWagons = 3;
    
    public bool IsCompleted => currentWagonIndex >= maxWagons;
    
    private void Start()
    {
        AutoFillWagonPanels();
        InitializePanels();
        UpdateCostDisplay();
    }
    
    /// <summary>
    /// Wagon container'dan otomatik olarak wagon panellerini bulur
    /// </summary>
    private void AutoFillWagonPanels()
    {
        if (wagonContainer == null)
        {
            Debug.LogError("[RemoveWagonsTutorial] Wagon container not assigned!");
            return;
        }
        
        int childCount = wagonContainer.childCount;
        
        if (childCount == 0)
        {
            Debug.LogError("[RemoveWagonsTutorial] Wagon container has no children!");
            return;
        }
        
        // Take first 3 children as wagon panels
        maxWagons = Mathf.Min(childCount, 3);
        wagonPanels = new GameObject[maxWagons];
        
        for (int i = 0; i < maxWagons; i++)
        {
            wagonPanels[i] = wagonContainer.GetChild(i).gameObject;
        }
        
        Debug.Log($"[RemoveWagonsTutorial] Auto-filled {maxWagons} wagon panels from children");
    }
    
    /// <summary>
    /// Tüm wagon panellerini göster
    /// </summary>
    private void InitializePanels()
    {
        if (wagonPanels == null) return;
        
        foreach (var wagon in wagonPanels)
        {
            if (wagon != null)
                wagon.SetActive(true);
        }
        
        Debug.Log("[RemoveWagonsTutorial] Initialized wagon panels");
    }
    
    /// <summary>
    /// Ability kullanıldığında (buton tıklandığında)
    /// </summary>
    public void OnAbilityUsed()
    {
        if (IsCompleted)
        {
            Debug.Log("[RemoveWagonsTutorial] Tutorial already completed!");
            return;
        }
        
        // Mevcut vagonu kaldır
        RemoveWagon(currentWagonIndex);
        
        // Sonraki vagona geç
        currentWagonIndex++;
        
        // UI'ı güncelle
        UpdateCostDisplay();
        
        Debug.Log($"[RemoveWagonsTutorial] Removed wagon {currentWagonIndex}/{maxWagons}, Next cost: {GetCost()}");
        
        if (IsCompleted)
        {
            Debug.Log("[RemoveWagonsTutorial] Tutorial completed!");
        }
    }
    
    /// <summary>
    /// Belirtilen wagon'u kaldır (fade out animasyonu)
    /// </summary>
    private void RemoveWagon(int index)
    {
        if (wagonPanels == null || index >= wagonPanels.Length) return;
        
        GameObject wagon = wagonPanels[index];
        if (wagon != null)
        {
            // Simple fade out - set inactive
            wagon.SetActive(false);
            Debug.Log($"[RemoveWagonsTutorial] Wagon {index + 1} removed");
        }
    }
    
    /// <summary>
    /// Maliyet metnini günceller (bir sonraki kullanım maliyeti)
    /// </summary>
    private void UpdateCostDisplay()
    {
        if (costText != null)
        {
            if (IsCompleted)
            {
                costText.text = "Completed!";
            }
            else
            {
                int nextCost = GetCost();
                costText.text = $"{nextCost} Coin";
            }
        }
    }
    
    /// <summary>
    /// Tutorial'ı sıfırlar
    /// </summary>
    public void ResetTutorial()
    {
        currentWagonIndex = 0;
        InitializePanels();
        UpdateCostDisplay();
        
        Debug.Log("[RemoveWagonsTutorial] Tutorial reset");
    }
    
    /// <summary>
    /// Ability'nin maliyetini döndürür (dinamik)
    /// </summary>
    public int GetCost()
    {
        int usageCount = currentWagonIndex;
        int baseCost = 100;
        int cost = baseCost * (int)Mathf.Pow(2, usageCount);
        
        return cost;
    }
    
    public string GetAbilityName() => abilityName;
    public string GetDescription() => description;
}
