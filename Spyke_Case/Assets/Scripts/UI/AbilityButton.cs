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
    [SerializeField] private TextMeshProUGUI costText;  // Yeteneğin maliyetini gösteren text
    // Not: Butonun ana metnini (örn: "Use", "Buy") değiştirmek için buraya bir referans daha eklenebilir.

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        button.onClick.AddListener(HandleButtonClick);

        // İlgili event'lere abone ol
        if (AbilityManager.Instance != null)
        {
            AbilityManager.Instance.OnAbilityCountChanged += OnAbilityCountChanged;
        }
        if (ResourceManager.Instance != null)
        {
            ResourceManager.OnCoinsChanged += OnCoinsChanged;
        }

        InitializeButtonState();
    }

    private void OnDisable()
    {
        button.onClick.RemoveListener(HandleButtonClick);

        if (AbilityManager.Instance != null)
        {
            AbilityManager.Instance.OnAbilityCountChanged -= OnAbilityCountChanged;
        }
        if (ResourceManager.Instance != null)
        {
            ResourceManager.OnCoinsChanged -= OnCoinsChanged;
        }
    }

    /// <summary>
    /// Butonun başlangıç durumunu mevcut envanter ve coin durumuna göre ayarlar.
    /// </summary>
    private void InitializeButtonState()
    {
        if (AbilityManager.Instance == null || ResourceManager.Instance == null) return;

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
        if (abilityCount > 0)
        {
            // --- KULLAN MODU ---
            if (countText != null) 
            {
                countText.text = abilityCount.ToString();
                countText.gameObject.SetActive(true);
            }
            if (costText != null) 
            {
                costText.gameObject.SetActive(false);
            }

            button.interactable = true;
        }
        else
        { 
            // --- SATIN AL MODU ---
            if (countText != null) 
            {
                countText.gameObject.SetActive(false);
            }
            if (costText != null) 
            {
                costText.text = cost.ToString();
                costText.gameObject.SetActive(true);
            }

            // Coin yeterli mi diye kontrol et
            button.interactable = coinCount >= cost;
        }
    }
}
