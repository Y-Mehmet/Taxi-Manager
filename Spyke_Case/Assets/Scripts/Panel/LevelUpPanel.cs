using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class LevelUpPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI earningsText;
    [SerializeField] private List<Image> starImages;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button retryButton;

    [Header("Star Sprites")]
    [SerializeField] private Sprite brightStar;
    [SerializeField] private Sprite greyStar;

    [Header("Invoice UI")]
    [SerializeField] private GameObject invoicePanel;
    [SerializeField] private TextMeshProUGUI passengerIncomeText;
    [SerializeField] private TextMeshProUGUI crashPenaltyText;
    [SerializeField] private TextMeshProUGUI uberPenaltyText;
    [SerializeField] private TextMeshProUGUI taxText;
    [SerializeField] private TextMeshProUGUI netEarningsText;
    [SerializeField] private TextMeshProUGUI insuranceStatusText;
    [SerializeField] private TextMeshProUGUI taxExemptionStatusText;

    private void Awake()
    {
        continueButton.onClick.AddListener(OnContinueButtonClicked);
        retryButton.onClick.AddListener(OnRetryButtonClicked);
    }

    public void Show(int stars, int earnings)
    {
        // Show stars
        for (int i = 0; i < starImages.Count; i++)
        {
            if (starImages[i] != null)
            {
                starImages[i].sprite = (i < stars) ? brightStar : greyStar;
            }
        }

        // Show invoice if available
        if (GameManager.Instance != null && GameManager.Instance.CurrentInvoice != null)
        {
            ShowInvoice(GameManager.Instance.CurrentInvoice);
        }
        else
        {
            // Fallback to old system
            if (earningsText != null)
            {
                earningsText.text = $"{earnings}";
            }
        }

        if (retryButton != null)
        {
            retryButton.gameObject.SetActive(stars < 3);
        }
    }

    private void ShowInvoice(LevelInvoiceData invoice)
    {
        // DON'T auto-activate invoice panel - let it be controlled manually
        // if (invoicePanel != null)
        // {
        //     invoicePanel.SetActive(true);
        // }

        // Income - Always show with label
        if (passengerIncomeText != null)
        {
            int income = invoice.CalculateTotalIncome();
            passengerIncomeText.text = $"Passenger Income: <color=green>+{income}</color> ({invoice.completedPassengers} x 20)";
        }

        // Crash Penalty - Always show with label, even if 0
        if (crashPenaltyText != null)
        {
            if (invoice.crashCount > 0)
            {
                int basePenalty = invoice.crashCount * 500;
                if (invoice.crashPenalty == 0)
                {
                    crashPenaltyText.text = $"Crash Penalty: <color=yellow>0</color> ({invoice.crashCount} x 500 - INSURED)";
                }
                else if (invoice.crashPenalty < basePenalty)
                {
                    crashPenaltyText.text = $"Crash Penalty: <color=yellow>-{invoice.crashPenalty}</color> ({invoice.crashCount} x {invoice.crashPenalty/invoice.crashCount} - OWN REPAIR)";
                }
                else
                {
                    crashPenaltyText.text = $"Crash Penalty: <color=red>-{invoice.crashPenalty}</color> ({invoice.crashCount} x 500)";
                }
            }
            else
            {
                // No crashes - show 0
                crashPenaltyText.text = $"Crash Penalty: <color=green>0</color> (0 x 500)";
            }
            crashPenaltyText.gameObject.SetActive(true);
        }

        // Uber Penalty - Always show with label, even if 0
        if (uberPenaltyText != null)
        {
            if (invoice.uberPickupCount > 0)
            {
                uberPenaltyText.text = $"Uber Penalty: <color=red>-{invoice.uberPenalty}</color> ({invoice.uberPickupCount} x 100)";
            }
            else
            {
                // No uber pickups - show 0
                uberPenaltyText.text = $"Uber Penalty: <color=green>0</color> (0 x 100)";
            }
            uberPenaltyText.gameObject.SetActive(true);
        }

        // Tax - Always show with label
        if (taxText != null)
        {
            if (invoice.taxAmount == 0 && invoice.passengerEarnings > 0)
            {
                taxText.text = $"Tax: <color=yellow>0</color> (TAX JOKER ACTIVE)";
            }
            else if (invoice.taxAmount > 0)
            {
                taxText.text = $"Tax: <color=red>-{invoice.taxAmount}</color> ({invoice.taxRate * 100}%)";
            }
            else
            {
                taxText.text = "Tax: <color=green>0</color> (0%)";
            }
        }

        // Net Earnings
        if (netEarningsText != null)
        {
            int net = invoice.CalculateNetEarnings();
            string color = net >= 0 ? "green" : "red";
            netEarningsText.text = $"<color={color}>{net:+#;-#;0}</color>";
        }

        // Joker Status (check active jokers from JokerSystem)
        if (insuranceStatusText != null)
        {
            bool hasRepairJoker = JokerSystem.Instance != null && 
                (JokerSystem.Instance.IsJokerActive(JokerType.CollisionInsurance) || 
                 JokerSystem.Instance.IsJokerActive(JokerType.OwnRepairStation));
            insuranceStatusText.gameObject.SetActive(hasRepairJoker);
        }

        if (taxExemptionStatusText != null)
        {
            bool hasTaxJoker = JokerSystem.Instance != null && 
                (JokerSystem.Instance.IsJokerActive(JokerType.Bribery) || 
                 JokerSystem.Instance.IsJokerActive(JokerType.HighOperatingExpenses) ||
                 JokerSystem.Instance.IsJokerActive(JokerType.OffshoreAccounts) ||
                 JokerSystem.Instance.IsJokerActive(JokerType.DoubleBookkeeping));
            taxExemptionStatusText.gameObject.SetActive(hasTaxJoker);
        }

        // Old earnings text (net earnings)
        if (earningsText != null)
        {
            int net = invoice.CalculateNetEarnings();
            earningsText.text = $"{net}";
        }
    }

    private void OnContinueButtonClicked()
    {
        // Transfer coins from temp to main BEFORE leaving
        if (GameManager.Instance != null && GameManager.Instance.CurrentInvoice != null)
        {
            if (GameEconomy.Instance != null)
            {
                GameEconomy.Instance.TransferTempToMain(GameManager.Instance.CurrentInvoice);
                Debug.Log("[LevelUpPanel] Coins transferred to main account.");
            }
        }

        // Load Main Scene (build index 0) - Level Select screen
        if (SceneManager.Instance != null)
        {
            SceneManager.Instance.LoadMainMenu();
            Debug.Log("[LevelUpPanel] Continue clicked. Loading Main Scene (Level Select).");
        }
        else
        {
            // Fallback: Load build index 0 directly
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
            Debug.Log("[LevelUpPanel] Continue clicked. Loading Main Scene (build index 0).");
        }
    }

    private void OnRetryButtonClicked()
    {
        // Transfer coins from temp to main BEFORE retrying
        if (GameManager.Instance != null && GameManager.Instance.CurrentInvoice != null)
        {
            if (GameEconomy.Instance != null)
            {
                GameEconomy.Instance.TransferTempToMain(GameManager.Instance.CurrentInvoice);
                Debug.Log("[LevelUpPanel] Coins transferred to main account.");
            }
        }

        // Reload current level (AllLevel scene with same level data)
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        Debug.Log("Retrying current level...");
    }
}