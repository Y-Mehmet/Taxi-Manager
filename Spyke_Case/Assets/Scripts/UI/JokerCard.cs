using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Individual joker card UI component
/// </summary>
public class JokerCard : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private TextMeshProUGUI effectText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button buyButton;
    [SerializeField] private Image cardBackground;

    [Header("Colors")]
    [SerializeField] private Color affordableColor = Color.white;
    [SerializeField] private Color unaffordableColor = Color.red;
    [SerializeField] private Color ownedColor = Color.green;

    private JokerType jokerType;
    private int cost;
    private bool isOwned;

    private void Awake()
    {
        if (buyButton != null)
        {
            buyButton.onClick.AddListener(OnBuyButtonClicked);
        }
    }

    public void Initialize(JokerType type)
    {
        jokerType = type;
        cost = JokerSystem.Instance != null ? JokerSystem.Instance.GetJokerCost(type) : 0;

        // Set card info
        if (nameText != null)
        {
            nameText.text = GetJokerName(type);
        }

        if (costText != null)
        {
            costText.text = $"{cost}";
        }

        if (effectText != null)
        {
            effectText.text = GetJokerEffect(type);
        }

        UpdateStatus();
    }

    public void UpdateStatus()
    {
        if (JokerSystem.Instance == null) return;

        int totalStars = JokerSystem.Instance.GetTotalStars();
        int remaining = JokerSystem.Instance.GetJokerRemainingGames(jokerType);
        bool isActive = JokerSystem.Instance.IsJokerActive(jokerType);
        
        // Check if owned (has remaining games)
        isOwned = remaining != 0; // 0 = not owned, -1 = unlimited, >0 = limited

        // Update status text
        if (statusText != null)
        {
            if (isActive)
            {
                // Active joker
                if (remaining == -1)
                {
                    statusText.text = "(Unlimited)";
                    statusText.color = ownedColor;
                }
                else if (remaining > 0)
                {
                    statusText.text = $"{remaining} games";
                    statusText.color = ownedColor;
                }
            }
            else if (isOwned)
            {
                // Owned but inactive (replaced by another joker in same category)
                statusText.text = "Inactive (Replaced)";
                statusText.color = Color.gray;
            }
            else
            {
                // Not owned
                statusText.text = "Not Owned";
                statusText.color = Color.white;
            }
        }

        // Update button state
        if (buyButton != null)
        {
            bool canAfford = totalStars >= cost;
            // Can buy if: affordable AND (not owned OR owned but inactive)
            buyButton.interactable = canAfford && !isActive;

            // Update cost text color
            if (costText != null)
            {
                if (isActive)
                {
                    costText.color = ownedColor;
                }
                else if (canAfford)
                {
                    costText.color = affordableColor;
                }
                else
                {
                    costText.color = unaffordableColor;
                }
            }
        }

        // Update card background
        if (cardBackground != null)
        {
            if (isActive)
            {
                // Active joker - bright green
                cardBackground.color = new Color(0.5f, 1f, 0.5f, 0.5f);
            }
            else if (isOwned)
            {
                // Owned but inactive - gray
                cardBackground.color = new Color(0.7f, 0.7f, 0.7f, 0.3f);
            }
            else
            {
                // Not owned - white
                cardBackground.color = Color.white;
            }
        }
    }

    private void OnBuyButtonClicked()
    {
        if (JokerSystem.Instance == null) return;

        if (JokerSystem.Instance.BuyJoker(jokerType))
        {
            /* Debug.Log($"[JokerCard] Successfully purchased {jokerType}"); */
            UpdateStatus();
            
            // Notify parent panel
            JokerShopPanel panel = GetComponentInParent<JokerShopPanel>();
            if (panel != null)
            {
                panel.OnJokerPurchased();
            }
        }
        else
        {
//             Debug.LogWarning($"[JokerCard] Failed to purchase {jokerType}");
        }
    }

    private string GetJokerName(JokerType type)
    {
        switch (type)
        {
            case JokerType.DoubleBookkeeping: return "Double Bookkeeping";
            case JokerType.Bribery: return "Bribery";
            case JokerType.HighOperatingExpenses: return "High Operating Expenses";
            case JokerType.OffshoreAccounts: return "Offshore Accounts";
            case JokerType.CollisionInsurance: return "Collision Insurance";
            case JokerType.OwnRepairStation: return "Own Repair Station";
            default: return "Unknown";
        }
    }

    private string GetJokerEffect(JokerType type)
    {
        switch (type)
        {
            case JokerType.DoubleBookkeeping: return "10% tax for 10 sessions";
            case JokerType.Bribery: return "0% tax for 5 sessions";
            case JokerType.HighOperatingExpenses: return "0% tax for 20 sessions";
            case JokerType.OffshoreAccounts: return "Unlimited 5% tax";
            case JokerType.CollisionInsurance: return "Zero repair for 5 sessions";
            case JokerType.OwnRepairStation: return "Unlimited 100 coin repair";
            default: return "";
        }
    }
}
