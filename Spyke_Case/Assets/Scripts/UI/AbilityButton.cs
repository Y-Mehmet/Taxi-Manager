using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Yetenekleri dinamik olarak satın almak veya kullanmak için UI butonlarına eklenen script.
/// Buton, sahip olunan yetenek sayısına göre "Satın Al" veya "Kullan" modları arasında geçiş yapar.
/// </summary>
[RequireComponent(typeof(Button))]
public class AbilityButton : MonoBehaviour
{
    [Header("Yetenek Ayarları")]
    [SerializeField] private AbilityType abilityType; // Bu butonun kontrol ettiği yetenek
    [SerializeField] private int cost = 100; // Yeteneğin maliyeti

    [Header("UI Referansları")]
    [SerializeField] private TextMeshProUGUI countText; // Sahip olunan yetenek sayısını gösteren text
    [SerializeField] private TextMeshProUGUI abilityNameText;  // Yeteneğin adını gösteren text

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

        if (ResourceManager.Instance != null)
        {
            ResourceManager.OnCoinsChanged += OnCoinsChanged;
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
        if (ResourceManager.Instance != null)
        {
            ResourceManager.OnCoinsChanged -= OnCoinsChanged;
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
        if (AbilityManager.Instance == null || ResourceManager.Instance == null) 
        {
            Debug.LogError($"[AbilityButton:{abilityType}] Cannot initialize, a manager is missing.");
            return;
        }

        int currentAbilityCount = AbilityManager.Instance.GetAbilityCount(abilityType);
        int currentCoins = ResourceManager.Instance.CurrentCoins;

        UpdateButtonUI(currentAbilityCount, currentCoins);
    }

    private void HandleButtonClick()
    {
        if (AbilityManager.Instance == null || ResourceManager.Instance == null) return;

        int currentCount = AbilityManager.Instance.GetAbilityCount(abilityType);

        if (currentCount > 0)
        {
            // If we already own the ability, just use it.
            AbilityManager.Instance.UseAbility(abilityType);
        }
        else
        {
            // If we don't own the ability, try to buy and use it immediately.
            if (ResourceManager.Instance.CurrentCoins < cost)
            {
                Debug.LogWarning($"[AbilityButton] Not enough coins to buy {abilityType}. Required: {cost}, Have: {ResourceManager.Instance.CurrentCoins}");
                return; // Exit if not enough coins
            }

            // Buy and execute immediately
            if (AbilityManager.Instance.BuyAndUseAbility(abilityType, cost))
            {
                 Debug.Log($"[AbilityButton] Successfully purchased and used {abilityType}.");
            }
        }
    }

    private void OnAbilityCountChanged(AbilityType type, int newCount)
    {
        if (type != this.abilityType) return;
        if (ResourceManager.Instance == null) return;

        UpdateButtonUI(newCount, ResourceManager.Instance.CurrentCoins);
    }

    private void OnCoinsChanged(int newCoins)
    {
        if (AbilityManager.Instance == null) return;

        if (AbilityManager.Instance.GetAbilityCount(abilityType) == 0)
        {
            UpdateButtonUI(0, newCoins);
        }
    }

    private void OnStopRegistered()
    {
        if (AbilityManager.Instance == null || ResourceManager.Instance == null) return;
        // Refresh UI to check availability
        UpdateButtonUI(AbilityManager.Instance.GetAbilityCount(abilityType), ResourceManager.Instance.CurrentCoins);
    }

    private void UpdateButtonUI(int abilityCount, int coinCount)
    {
        if (countText == null)
        {
            Debug.LogError($"[AbilityButton:{abilityType}] CountText reference is not set in the inspector!");
            return;
        }

        // Check if ability is available (e.g., max stops not reached)
        bool isAvailable = AbilityManager.Instance.IsAbilityAvailable(abilityType);

        if (!isAvailable)
        {
            countText.text = "MAX";
            countText.gameObject.SetActive(true);
            button.interactable = false;
            return;
        }

        if (abilityCount > 0)
        {
            // --- USE MODE ---
            countText.text = abilityCount.ToString();
            countText.gameObject.SetActive(true);
            button.interactable = true;
        }
        else
        { 
            // --- BUY & USE MODE ---
            // Show price instead of "+"
            countText.text = cost.ToString(); 
            countText.gameObject.SetActive(true);
            button.interactable = coinCount >= cost;
        }
    }
}