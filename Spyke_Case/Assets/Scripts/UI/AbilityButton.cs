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
        // Subscribe to events in Start() to ensure Singletons are ready.
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

        // Set the initial state of the button.
        InitializeButtonState();
    }

    private void OnDisable()
    {
        // It's safe to remove listeners even if they weren't added.
        if (AbilityManager.Instance != null)
        {
            AbilityManager.Instance.OnAbilityCountChanged -= OnAbilityCountChanged;
        }
        if (ResourceManager.Instance != null)
        {
            ResourceManager.OnCoinsChanged -= OnCoinsChanged;
        }
    }

    private void OnDestroy()
    {
        button.onClick.RemoveListener(HandleButtonClick);
    }

    /// <summary>
    /// Butonun başlangıç durumunu mevcut envanter ve coin durumuna göre ayarlar.
    /// </summary>
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

    /// <summary>
    /// Butona tıklandığında mevcut duruma göre Satın Al veya Kullan işlevini çağırır.
    /// </summary>
    private void HandleButtonClick()
    {
        if (AbilityManager.Instance == null) return;

        int currentCount = AbilityManager.Instance.GetAbilityCount(abilityType);

        if (currentCount > 0)
        {
            // Envanterde varsa KULLAN
            AbilityManager.Instance.UseAbility(abilityType);
        }
        else
        {
            // Envanterde yoksa SATIN AL
            AbilityManager.Instance.BuyAbility(abilityType, cost);
        }
    }

    // AbilityManager'dan gelen event'i dinler
    private void OnAbilityCountChanged(AbilityType type, int newCount)
    {
        if (type != this.abilityType) return; // Sadece ilgili yetenek için güncelle
        if (ResourceManager.Instance == null) return;

        UpdateButtonUI(newCount, ResourceManager.Instance.CurrentCoins);
    }

    // ResourceManager'dan gelen event'i dinler
    private void OnCoinsChanged(int newCoins)
    {
        if (AbilityManager.Instance == null) return;

        // Sadece "Satın Al" modundaysak (yani yetenek sayımız 0 ise) butonun durumunu güncelle
        if (AbilityManager.Instance.GetAbilityCount(abilityType) == 0)
        {
            UpdateButtonUI(0, newCoins);
        }
    }

    /// <summary>
    /// Gelen verilere göre butonun tüm görsel durumunu günceller.
    /// </summary>
    private void UpdateButtonUI(int abilityCount, int coinCount)
    {
        if (countText == null)
        {
            Debug.LogError($"[AbilityButton:{abilityType}] CountText reference is not set in the inspector!");
            return;
        }

        if (abilityCount > 0)
        {
            // --- KULLAN MODU ---
            countText.text = abilityCount.ToString();
            countText.gameObject.SetActive(true);
            button.interactable = true;
        }
        else
        { 
            // --- SATIN AL MODU ---
            countText.text = "+"; // Show '+' when count is 0
            countText.gameObject.SetActive(true);
            button.interactable = coinCount >= cost;
        }
    }
}