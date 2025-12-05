using UnityEngine;
using TMPro;

/// <summary>
/// Remove Wagons ability tutorial implementation.
/// Template - İhtiyacınıza göre özelleştirin.
/// </summary>
public class RemoveWagonsTutorial : MonoBehaviour, IAbilityTutorial
{
    [Header("Tutorial Elements")]
    [SerializeField] private GameObject[] wagonObjects; // Kaldırılacak vagonlar
    
    [Header("Cost Display")]
    [SerializeField] private TextMeshProUGUI costText;
    
    [Header("Settings")]
    [SerializeField] private int costPerUse = 30;
    [SerializeField] private string abilityName = "Remove Wagons";
    [SerializeField] private string description = "Remove a specific passenger group from the map. Use this to clear space when you're stuck.";
    
    private int usageCount = 0;
    private int maxUsages = 3; // Kaç kez kullanılabilir
    
    public bool IsCompleted => usageCount >= maxUsages;
    
    private void Start()
    {
        InitializeTutorial();
    }
    
    private void InitializeTutorial()
    {
        // Tüm vagonları göster
        foreach (var wagon in wagonObjects)
        {
            if (wagon != null)
                wagon.SetActive(true);
        }
        
        UpdateCostDisplay();
    }
    
    public void OnAbilityUsed()
    {
        if (IsCompleted) return;
        
        // Bir vagonu kaldır
        if (usageCount < wagonObjects.Length && wagonObjects[usageCount] != null)
        {
            wagonObjects[usageCount].SetActive(false);
        }
        
        usageCount++;
        UpdateCostDisplay();
        
        Debug.Log($"[RemoveWagonsTutorial] Used {usageCount}/{maxUsages} times");
    }
    
    private void UpdateCostDisplay()
    {
        if (costText != null)
        {
            int totalCost = usageCount * costPerUse;
            costText.text = $"Harcanan: {totalCost} Coin";
        }
    }
    
    public void ResetTutorial()
    {
        usageCount = 0;
        InitializeTutorial();
    }
    
    public int GetCost() => costPerUse;
    public string GetAbilityName() => abilityName;
    public string GetDescription() => description;
}
