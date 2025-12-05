using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Generic ability tutorial button.
/// Herhangi bir IAbilityTutorial implementation ile çalışır.
/// SOLID: Open/Closed Principle - Yeni ability'ler için extend edilebilir, modify edilmez.
/// </summary>
[RequireComponent(typeof(Button))]
public class AbilityTutorialButton : MonoBehaviour
{
    [Header("Tutorial Reference")]
    [SerializeField] private MonoBehaviour tutorialBehaviour; // IAbilityTutorial implement eden MonoBehaviour
    
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI buttonText; // Buton üzerindeki text (opsiyonel)
    [SerializeField] private TextMeshProUGUI costText; // Maliyet göstergesi (opsiyonel)
    [SerializeField] private TextMeshProUGUI descriptionText; // Açıklama metni (ortak, opsiyonel)
    
    private Button button;
    private IAbilityTutorial tutorial;
    
    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnButtonClicked);
        
        // Interface'i al
        if (tutorialBehaviour != null)
        {
            tutorial = tutorialBehaviour as IAbilityTutorial;
            
            if (tutorial == null)
            {
                Debug.LogError($"[AbilityTutorialButton] {tutorialBehaviour.GetType().Name} does not implement IAbilityTutorial!");
            }
        }
        else
        {
            Debug.LogError("[AbilityTutorialButton] No tutorial behaviour assigned!");
        }
    }
    
    private void Start()
    {
        UpdateButtonUI();
    }
    
    /// <summary>
    /// Buton tıklandığında çağrılır
    /// </summary>
    private void OnButtonClicked()
    {
        if (tutorial == null)
        {
            Debug.LogError("[AbilityTutorialButton] Tutorial is null!");
            return;
        }
        
        if (tutorial.IsCompleted)
        {
            Debug.Log($"[AbilityTutorialButton] {tutorial.GetAbilityName()} tutorial already completed!");
            return;
        }
        
        // Ability'yi kullan
        tutorial.OnAbilityUsed();
        
        // UI'ı güncelle
        UpdateButtonUI();
        
        // Tamamlandıysa butonu devre dışı bırak
        if (tutorial.IsCompleted)
        {
            button.interactable = false;
            Debug.Log($"[AbilityTutorialButton] {tutorial.GetAbilityName()} tutorial completed, button disabled");
        }
    }
    
    /// <summary>
    /// Buton UI'ını günceller
    /// </summary>
    private void UpdateButtonUI()
    {
        if (tutorial == null) return;
        
        // Buton metnini güncelle
        if (buttonText != null)
        {
            buttonText.text = tutorial.GetAbilityName();
        }
        
        // Maliyet metnini güncelle
        if (costText != null)
        {
            costText.text = $"{tutorial.GetCost()} Coin";
        }
        
        // Açıklama metnini güncelle (ortak text)
        if (descriptionText != null)
        {
            descriptionText.text = tutorial.GetDescription();
        }
    }
    
    /// <summary>
    /// Tutorial'ı sıfırlar (test için)
    /// </summary>
    public void ResetTutorial()
    {
        if (tutorial != null)
        {
            tutorial.ResetTutorial();
            button.interactable = true;
            UpdateButtonUI();
        }
    }
    
    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClicked);
        }
    }
}
