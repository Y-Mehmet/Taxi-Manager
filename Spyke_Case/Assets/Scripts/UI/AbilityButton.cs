using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Yetenekleri dinamik olarak satÄ±n almak veya kullanmak iÃ§in UI butonlarÄ±na eklenen script.
/// Buton, sahip olunan yetenek sayÄ±sÄ±na gÃ¶re "SatÄ±n Al" veya "Kullan" modlarÄ± arasÄ±nda geÃ§iÅŸ yapar.
/// Fiyat her kullanÄ±mda katlanÄ±r: 100, 200, 400, 800...
/// </summary>
[RequireComponent(typeof(Button))]
public class AbilityButton : MonoBehaviour
{
    [Header("Yetenek AyarlarÄ±")]
    [SerializeField] private AbilityType abilityType; // Bu butonun kontrol ettiÄŸi yetenek

    [Header("UI ReferanslarÄ±")]
    [SerializeField] private TextMeshProUGUI costText; // Maliyeti gÃ¶steren text (ability adÄ± yerine)
    [SerializeField] private TextMeshProUGUI abilityNameText;  // YeteneÄŸin adÄ±nÄ± gÃ¶steren text (opsiyonel)
    [SerializeField] private Image abilityIconImage; // Ability icon image

    private Button button;
    private Sprite originalIconSprite; // Original icon sprite
    private Vector2 originalIconSize; // Original icon size (to restore after SetNativeSize)

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
        
        // Save original icon sprite and size
        if (abilityIconImage != null)
        {
            originalIconSprite = abilityIconImage.sprite;
            originalIconSize = abilityIconImage.rectTransform.sizeDelta;
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
        
        // Check if we're in AllLevel scene (build index 1)
        int currentSceneIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
        
        if (currentSceneIndex == 1) // AllLevel scene
        {
            // Check if ability is unlocked
            if (AbilityUnlockManager.Instance != null)
            {
                bool isUnlocked = AbilityUnlockManager.Instance.IsAbilityUnlocked(abilityType);
                
                if (!isUnlocked)
                {
                    // Ability is locked - show unlock level and disable button
                    SetLockedState();
                    return;
                }
            }
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
//             Debug.LogWarning($"[AbilityButton] Not enough coins to buy {abilityType}. Required: {currentCost}, Have: {currentCoins}");
            return;
        }

        // Convert UI position to world position for animation
        Vector3 worldPosition = GetWorldPositionFromUI();
        
        Debug.Log($"[AbilityButton] Buying ability from position: {worldPosition}, Cost: {currentCost}");
        
        // Spend coins from main resource
        if (GameEconomy.Instance.SpendMainCoins(currentCost))
        {
            // Show spending animation with shake effect (only for abilities)
            if (CoinAnimationManager.Instance != null)
            {
                CoinAnimationManager.Instance.ShowCoinSpendWithShake(currentCost, worldPosition);
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
    /// UI pozisyonunu dÃ¼nya pozisyonuna Ã§evirir (animasyon iÃ§in)
    /// </summary>
    private Vector3 GetWorldPositionFromUI()
    {
        RectTransform rectTransform = GetComponent<RectTransform>();
        Canvas canvas = GetComponentInParent<Canvas>();
        
        if (canvas == null)
        {
//             Debug.LogWarning("[AbilityButton] Canvas not found, using transform.position");
            return transform.position;
        }

        // UI pozisyonunu dÃ¼nya pozisyonuna Ã§evir
        Vector3 worldPosition;
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            // Overlay mode iÃ§in ekran merkezinden offset hesapla
            worldPosition = rectTransform.position;
        }
        else
        {
            // Camera mode iÃ§in dÃ¼nya pozisyonunu al
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
          //  Debug.LogError($"[AbilityButton:{abilityType}] CostText reference is not set in the inspector!");
            return;
        }
        
        // Check if we're in AllLevel scene (build index 1)
        int currentSceneIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
        
        if (currentSceneIndex == 1) // AllLevel scene
        {
            // Check if ability is unlocked
            if (AbilityUnlockManager.Instance != null)
            {
                bool isUnlocked = AbilityUnlockManager.Instance.IsAbilityUnlocked(abilityType);
                
                if (!isUnlocked)
                {
                    // Ability is locked - keep locked state
                    SetLockedState();
                    return;
                }
                else
                {
                    // Ability is unlocked - restore original icon and size
                    if (abilityIconImage != null && originalIconSprite != null)
                    {
                        abilityIconImage.sprite = originalIconSprite;
                        abilityIconImage.rectTransform.sizeDelta = originalIconSize;
                    }
                }
            }
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
    
    /// <summary>
    /// Locked durumunu ayarlar (icon, text, interactable)
    /// </summary>
    private void SetLockedState()
    {
        if (AbilityUnlockManager.Instance == null) return;
        
        // Set unlock level text
        string unlockText = AbilityUnlockManager.Instance.GetUnlockLevelText(abilityType);
        costText.text = unlockText;
        // Don't change text color - keep original color
        
        // Change icon to locked sprite
        if (abilityIconImage != null)
        {
            Sprite lockedSprite = AbilityUnlockManager.Instance.GetLockedIconSprite();
            if (lockedSprite != null)
            {
                abilityIconImage.sprite = lockedSprite;
                // Use locked icon's native size instead of stretching
                abilityIconImage.SetNativeSize();
            }
        }
        
        // Disable button
        button.interactable = false;
        
        Debug.Log($"[AbilityButton] {abilityType} is LOCKED - {unlockText}");
    }
}
