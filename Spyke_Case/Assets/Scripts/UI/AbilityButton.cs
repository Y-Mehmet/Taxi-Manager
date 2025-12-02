using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Yetenekleri dinamik olarak satın almak veya kullanmak için UI butonlarına eklenen script.
/// Buton, sahip olunan yetenek sayısına göre "Satın Al" veya "Kullan" modları arasında geçiş yapar.
/// Fiyat her kullanımda katlanır: 100, 200, 400, 800...
/// </summary>
[RequireComponent(typeof(Button))]
public class AbilityButton : MonoBehaviour
{
    [Header("Yetenek Ayarları")]
    [SerializeField] private AbilityType abilityType; // Bu butonun kontrol ettiği yetenek

    [Header("UI Referansları")]
    [SerializeField] private TextMeshProUGUI costText; // Maliyeti gösteren text (ability adı yerine)
    [SerializeField] private TextMeshProUGUI abilityNameText;  // Yeteneğin adını gösteren text (opsiyonel)

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(HandleButtonClick);
    }

    private void Start()
    {
        if (AbilityManager.Instance != null)
        {
            AbilityManager.Instance.OnAbilityCountChanged += OnAbilityCountChanged;
        }
        else
        {
            Debug.LogError($"[AbilityButton:{abilityType}] AbilityManager.Instance is null. Cannot subscribe to events.");
        }

        // Listen to main coins changes (from GameEconomy)
        if (GameEconomy.Instance != null)
        {
            GameEconomy.OnMainCoinsChanged += OnCoinsChanged;
        }

        // Listen for stop changes to update availability
        if (abilityType == AbilityType.AddNewStop)
        {
             StopManager.OnStopRegistered += OnStopRegistered;
        }

        InitializeButtonState();
    }

    private void OnDisable()
    {
        if (AbilityManager.Instance != null)
        {
            AbilityManager.Instance.OnAbilityCountChanged -= OnAbilityCountChanged;
        }
        
        if (GameEconomy.Instance != null)
        {
            GameEconomy.OnMainCoinsChanged -= OnCoinsChanged;
        }
        
        if (abilityType == AbilityType.AddNewStop)
        {
             StopManager.OnStopRegistered -= OnStopRegistered;
        }
    }

    private void OnDestroy()
    {
        button.onClick.RemoveListener(HandleButtonClick);
    }

    /// <summary>
    /// Resets the button's state, e.g., when a new level starts.
    /// </summary>
    public void ResetState()
    {
        InitializeButtonState();
    }

    private void InitializeButtonState()
    {
        if (AbilityManager.Instance == null || GameEconomy.Instance == null) 
        {
            Debug.LogError($"[AbilityButton:{abilityType}] Cannot initialize, a manager is missing.");
            return;
        }

        int currentCoins = GameEconomy.Instance.GetMainCoins();
        UpdateButtonUI(currentCoins);
    }

    private void HandleButtonClick()
    {
        if (AbilityManager.Instance == null || GameEconomy.Instance == null) return;
        if (AbilityUsageTracker.Instance == null) return;

        // Get current cost based on usage count
        int currentCost = AbilityUsageTracker.Instance.GetAbilityCost(abilityType);
        int currentCoins = GameEconomy.Instance.GetMainCoins();

        if (currentCoins < currentCost)
        {
            Debug.LogWarning($"[AbilityButton] Not enough coins to buy {abilityType}. Required: {currentCost}, Have: {currentCoins}");
            return;
        }

        // Convert UI position to world position for animation
        Vector3 worldPosition = GetWorldPositionFromUI();
        
        Debug.Log($"[AbilityButton] Buying ability from position: {worldPosition}, Cost: {currentCost}");
        
        // Spend coins from main resource
        if (GameEconomy.Instance.SpendMainCoins(currentCost))
        {
            // Show spending animation
            if (CoinAnimationManager.Instance != null)
            {
                CoinAnimationManager.Instance.ShowSpendingFeedback(currentCost, worldPosition);
            }

            // Track usage (increments counter for next use)
            AbilityUsageTracker.Instance.OnAbilityUsed(abilityType);

            // Execute ability
            AbilityManager.Instance.ExecuteAbilityDirect(abilityType);

            Debug.Log($"[AbilityButton] Successfully purchased and used {abilityType}.");
            
            // Update UI immediately
            UpdateButtonUI(GameEconomy.Instance.GetMainCoins());
        }
    }

    /// <summary>
    /// UI pozisyonunu dünya pozisyonuna çevirir (animasyon için)
    /// </summary>
    private Vector3 GetWorldPositionFromUI()
    {
        RectTransform rectTransform = GetComponent<RectTransform>();
        Canvas canvas = GetComponentInParent<Canvas>();
        
        if (canvas == null)
        {
            Debug.LogWarning("[AbilityButton] Canvas not found, using transform.position");
            return transform.position;
        }

        // UI pozisyonunu dünya pozisyonuna çevir
        Vector3 worldPosition;
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            // Overlay mode için ekran merkezinden offset hesapla
            worldPosition = rectTransform.position;
        }
        else
        {
            // Camera mode için dünya pozisyonunu al
            worldPosition = rectTransform.position;
        }

        Debug.Log($"[AbilityButton] UI Position: {rectTransform.position}, World Position: {worldPosition}");
        return worldPosition;
    }

    private void OnAbilityCountChanged(AbilityType type, int newCount)
    {
        if (type != this.abilityType) return;
        if (GameEconomy.Instance == null) return;

        UpdateButtonUI(GameEconomy.Instance.GetMainCoins());
    }

    private void OnCoinsChanged(int newCoins)
    {
        UpdateButtonUI(newCoins);
    }

    private void OnStopRegistered()
    {
        if (GameEconomy.Instance == null) return;
        UpdateButtonUI(GameEconomy.Instance.GetMainCoins());
    }

    private void UpdateButtonUI(int coinCount)
    {
        if (costText == null)
        {
            Debug.LogError($"[AbilityButton:{abilityType}] CostText reference is not set in the inspector!");
            return;
        }

        // Check if ability is available (e.g., max stops not reached)
        bool isAvailable = AbilityManager.Instance.IsAbilityAvailable(abilityType);

        if (!isAvailable)
        {
            costText.text = "MAX";
            costText.color = Color.white;
            button.interactable = false;
            return;
        }

        // Get current cost based on usage count
        int currentCost = AbilityUsageTracker.Instance.GetAbilityCost(abilityType);

        // Display cost
        costText.text = currentCost.ToString();

        // Set color based on affordability
        if (coinCount >= currentCost)
        {
            costText.color = Color.white; // Can afford - white
            button.interactable = true;
        }
        else
        {
            costText.color = Color.red; // Cannot afford - red
            button.interactable = false;
        }
    }
}