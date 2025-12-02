using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Joker shop panel with card-based UI
/// </summary>
public class JokerShopPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI totalStarsText;
    [SerializeField] private Button closeButton;
    
    [Header("Joker Cards")]
    [SerializeField] private List<JokerCard> jokerCards = new List<JokerCard>();

    private void Awake()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseButtonClicked);
        }
    }

    private void OnEnable()
    {
        // Subscribe to events
        if (JokerSystem.Instance != null)
        {
            JokerSystem.OnTotalStarsChanged += UpdateTotalStars;
            JokerSystem.OnJokerCountChanged += OnJokerCountChanged;
        }

        RefreshUI();
    }

    private void OnDisable()
    {
        // Unsubscribe from events
        if (JokerSystem.Instance != null)
        {
            JokerSystem.OnTotalStarsChanged -= UpdateTotalStars;
            JokerSystem.OnJokerCountChanged -= OnJokerCountChanged;
        }
    }

    private void RefreshUI()
    {
        if (JokerSystem.Instance == null) return;

        // Calculate total stars
        JokerSystem.Instance.CalculateTotalStars();

        // Update total stars display
        UpdateTotalStars(JokerSystem.Instance.GetTotalStars());

        // Initialize all joker cards
        InitializeJokerCards();

        // Update all card statuses
        UpdateAllCards();
    }

    private void InitializeJokerCards()
    {
        // If cards are not assigned in inspector, find them
        if (jokerCards.Count == 0)
        {
            jokerCards.AddRange(GetComponentsInChildren<JokerCard>(true));
        }

        // Initialize each card with its joker type
        // Assuming cards are in order: DoubleBookkeeping, Bribery, HighOperatingExpenses, 
        // OffshoreAccounts, CollisionInsurance, OwnRepairStation
        JokerType[] types = new JokerType[]
        {
            JokerType.DoubleBookkeeping,
            JokerType.Bribery,
            JokerType.HighOperatingExpenses,
            JokerType.OffshoreAccounts,
            JokerType.CollisionInsurance,
            JokerType.OwnRepairStation
        };

        for (int i = 0; i < jokerCards.Count && i < types.Length; i++)
        {
            if (jokerCards[i] != null)
            {
                jokerCards[i].Initialize(types[i]);
            }
        }
    }

    private void UpdateTotalStars(int totalStars)
    {
        if (totalStarsText != null)
        {
            totalStarsText.text = $"⭐ {totalStars}";
        }
    }

    private void OnJokerCountChanged(JokerType type, int remaining)
    {
        // Update all cards when any joker changes
        UpdateAllCards();
    }

    private void UpdateAllCards()
    {
        foreach (var card in jokerCards)
        {
            if (card != null)
            {
                card.UpdateStatus();
            }
        }
    }

    public void OnJokerPurchased()
    {
        // Recalculate stars after purchase
        if (JokerSystem.Instance != null)
        {
            JokerSystem.Instance.CalculateTotalStars();
        }

        // Update all cards
        UpdateAllCards();
    }

    private void OnCloseButtonClicked()
    {
        gameObject.SetActive(false);
    }
}
