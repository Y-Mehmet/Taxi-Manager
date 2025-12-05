using UnityEngine;
using TMPro;

/// <summary>
/// Universal Pathfinding ability tutorial implementation.
/// Template - İhtiyacınıza göre özelleştirin.
/// </summary>
public class UniversalPathfindingTutorial : MonoBehaviour, IAbilityTutorial
{
    [Header("Tutorial Elements")]
    [SerializeField] private GameObject pathVisualization; // Yol görselleştirmesi
    [SerializeField] private GameObject[] pathSteps; // Yol adımları
    
    [Header("Cost Display")]
    [SerializeField] private TextMeshProUGUI costText;
    
    [Header("Settings")]
    [SerializeField] private int costPerUse = 40;
    [SerializeField] private string abilityName = "Universal Pathfinding";
    [SerializeField] private string description = "Allow a passenger group to go to any stop. Very useful when you're stuck and need flexibility!\n\n💰 Cost increases with each use:\n1st: 100 | 2nd: 200 | 3rd: 400 | 4th: 800 Coins";
    
    private int usageCount = 0;
    private int maxUsages = 2;
    
    public bool IsCompleted => usageCount >= maxUsages;
    
    private void Start()
    {
        InitializeTutorial();
    }
    
    private void InitializeTutorial()
    {
        if (pathVisualization != null)
            pathVisualization.SetActive(false);
            
        foreach (var step in pathSteps)
        {
            if (step != null)
                step.SetActive(false);
        }
        
        UpdateCostDisplay();
    }
    
    public void OnAbilityUsed()
    {
        if (IsCompleted) return;
        
        // Yolu göster
        if (pathVisualization != null)
            pathVisualization.SetActive(true);
            
        // Adımları sırayla göster
        if (usageCount < pathSteps.Length && pathSteps[usageCount] != null)
        {
            pathSteps[usageCount].SetActive(true);
        }
        
        usageCount++;
        UpdateCostDisplay();
        
        Debug.Log($"[UniversalPathfindingTutorial] Used {usageCount}/{maxUsages} times");
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
