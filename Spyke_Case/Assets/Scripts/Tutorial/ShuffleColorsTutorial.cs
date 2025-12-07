using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Shuffle Colors ability tutorial implementation.
/// Shows 3 color panels that get shuffled.
/// </summary>
public class ShuffleColorsTutorial : MonoBehaviour, IAbilityTutorial
{
    [Header("Color Panels")]
    [SerializeField] private Transform colorContainer; // Color panellerinin parent'ı
    
    [Header("Cost Display")]
    [SerializeField] private TextMeshProUGUI costText; // Maliyet göstergesi
    
    [Header("Settings")]
    [SerializeField] private AbilityType abilityType = AbilityType.ShuffleWagonColors;
    [SerializeField] private string abilityName = "Shuffle Colors";
    [SerializeField] private string description = "Shuffle wagon colors to create new matching opportunities. Great for breaking deadlocks!\n\n💰 Cost increases with each use:\n1st: 100 | 2nd: 200 | 3rd: 400 | 4th: 800 Coins";
    
    private Image[] colorPanels;
    private Color[] originalColors;
    private int currentCost = 100; // Başlangıç maliyeti, her kullanımda 2 katına çıkar
    
    public bool IsCompleted => false; // Sürekli tekrar eder, skip ile durur
    
    private void Start()
    {
        AutoFillColorPanels();
        InitializePanels();
        UpdateCostDisplay();
    }
    
    /// <summary>
    /// Color container'dan otomatik olarak color panellerini bulur
    /// </summary>
    private void AutoFillColorPanels()
    {
        if (colorContainer == null)
        {
            Debug.LogError("[ShuffleColorsTutorial] Color container not assigned!");
            return;
        }
        
        int childCount = colorContainer.childCount;
        
        if (childCount == 0)
        {
            Debug.LogError("[ShuffleColorsTutorial] Color container has no children!");
            return;
        }
        
        // Get all children as color panels
        colorPanels = new Image[childCount];
        originalColors = new Color[childCount];
        
        for (int i = 0; i < childCount; i++)
        {
            Transform child = colorContainer.GetChild(i);
            colorPanels[i] = child.GetComponent<Image>();
            
            if (colorPanels[i] != null)
            {
                originalColors[i] = colorPanels[i].color;
            }
        }
        
        Debug.Log($"[ShuffleColorsTutorial] Auto-filled {childCount} color panels from children");
    }
    
    /// <summary>
    /// Renkleri başlangıç durumuna getir
    /// </summary>
    private void InitializePanels()
    {
        if (colorPanels == null || originalColors == null) return;
        
        // Set initial colors (Red, Green, Blue)
        Color[] initialColors = new Color[] { Color.red, Color.green, Color.blue };
        
        for (int i = 0; i < Mathf.Min(colorPanels.Length, initialColors.Length); i++)
        {
            if (colorPanels[i] != null)
            {
                colorPanels[i].color = initialColors[i];
                originalColors[i] = initialColors[i];
            }
        }
        
        Debug.Log("[ShuffleColorsTutorial] Initialized color panels");
    }
    
    /// <summary>
    /// Ability kullanıldığında (buton tıklandığında)
    /// </summary>
    public void OnAbilityUsed()
    {
        // Renkleri shuffle et
        ShuffleColors();
        
        // Cost'u 2 katına çıkar
        currentCost *= 2;
        
        // UI'ı güncelle
        UpdateCostDisplay();
        
        Debug.Log($"[ShuffleColorsTutorial] Colors shuffled, Next cost: {currentCost}");
    }
    
    /// <summary>
    /// Renkleri karıştır
    /// </summary>
    private void ShuffleColors()
    {
        if (colorPanels == null) return;
        
        // Simple shuffle - rotate colors
        Color temp = colorPanels[0].color;
        
        for (int i = 0; i < colorPanels.Length - 1; i++)
        {
            if (colorPanels[i] != null && colorPanels[i + 1] != null)
            {
                colorPanels[i].color = colorPanels[i + 1].color;
            }
        }
        
        if (colorPanels[colorPanels.Length - 1] != null)
        {
            colorPanels[colorPanels.Length - 1].color = temp;
        }
        
        Debug.Log("[ShuffleColorsTutorial] Colors shuffled");
    }
    
    /// <summary>
    /// Maliyet metnini günceller (bir sonraki kullanım maliyeti)
    /// </summary>
    private void UpdateCostDisplay()
    {
        if (costText != null)
        {
            costText.text = $"{currentCost} Coin";
        }
    }
    
    /// <summary>
    /// Tutorial'ı sıfırlar
    /// </summary>
    public void ResetTutorial()
    {
        currentCost = 100;
        InitializePanels();
        UpdateCostDisplay();
        
        Debug.Log("[ShuffleColorsTutorial] Tutorial reset");
    }
    
    /// <summary>
    /// Ability'nin maliyetini döndürür (dinamik)
    /// </summary>
    public int GetCost()
    {
        return currentCost;
    }
    
    public string GetAbilityName() => abilityName;
    public string GetDescription() => description;
}
