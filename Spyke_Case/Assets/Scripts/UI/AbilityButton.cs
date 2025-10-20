using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
public class AbilityButton : MonoBehaviour
{
    [Header("Ability Settings")]
    [SerializeField] private AbilityType abilityType;

    [Header("Button Mode")]
    [SerializeField] private bool isBuyButton = false;
    [SerializeField] private int abilityCost = 100;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI countText;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(HandleClick);
    }

    private void OnEnable()
    {
        if (AbilityManager.Instance != null)
        {
            AbilityManager.Instance.OnAbilityCountChanged += UpdateCountText;
            // Set initial count on enable
            UpdateCountText(abilityType, AbilityManager.Instance.GetAbilityCount(abilityType));
        }
    }

    private void OnDisable()
    {
        if (AbilityManager.Instance != null)
        {
            AbilityManager.Instance.OnAbilityCountChanged -= UpdateCountText;
        }
    }

    public void HandleClick()
    {
        if (isBuyButton)
        {
            AbilityManager.Instance.BuyAbility(abilityType, abilityCost);
        }
        else
        {
            AbilityManager.Instance.UseAbility(abilityType);
        }
    }

    private void UpdateCountText(AbilityType type, int count)
    {
        // Only update if the change is for this button's ability type
        if (type == abilityType && countText != null && !isBuyButton)
        {
            countText.text = count.ToString();
            // Optionally, hide the text or button if count is zero
            gameObject.SetActive(count > 0);
        }
    }
}
