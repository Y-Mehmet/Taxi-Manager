using UnityEngine;
using TMPro;

/// <summary>
/// Shuffle Colors ability tutorial implementation.
/// Template - İhtiyacınıza göre özelleştirin.
/// </summary>
public class ShuffleColorsTutorial : MonoBehaviour, IAbilityTutorial
{
    [Header("Tutorial Elements")]
    [SerializeField] private GameObject[] coloredObjects; // Renk değişecek objeler
    [SerializeField] private Color[] availableColors; // Kullanılabilir renkler
    
    [Header("Cost Display")]
    [SerializeField] private TextMeshProUGUI costText;
    
    [Header("Settings")]
    [SerializeField] private int costPerUse = 25;
    [SerializeField] private string abilityName = "Shuffle Colors";
    [SerializeField] private string description = "Shuffle wagon colors to create new matching opportunities. Great for breaking deadlocks!";
    
    private int usageCount = 0;
    private int maxUsages = 3;
    
    public bool IsCompleted => usageCount >= maxUsages;
    
    private void Start()
    {
        InitializeTutorial();
    }
    
    private void InitializeTutorial()
    {
        // İlk renkleri ayarla
        for (int i = 0; i < coloredObjects.Length; i++)
        {
            if (coloredObjects[i] != null)
            {
                var renderer = coloredObjects[i].GetComponent<Renderer>();
                if (renderer != null && availableColors.Length > 0)
                {
                    renderer.material.color = availableColors[i % availableColors.Length];
                }
            }
        }
        
        UpdateCostDisplay();
    }
    
    public void OnAbilityUsed()
    {
        if (IsCompleted) return;
        
        // Renkleri karıştır
        foreach (var obj in coloredObjects)
        {
            if (obj != null)
            {
                var renderer = obj.GetComponent<Renderer>();
                if (renderer != null && availableColors.Length > 0)
                {
                    Color randomColor = availableColors[Random.Range(0, availableColors.Length)];
                    renderer.material.color = randomColor;
                }
            }
        }
        
        usageCount++;
        UpdateCostDisplay();
        
        Debug.Log($"[ShuffleColorsTutorial] Used {usageCount}/{maxUsages} times");
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
