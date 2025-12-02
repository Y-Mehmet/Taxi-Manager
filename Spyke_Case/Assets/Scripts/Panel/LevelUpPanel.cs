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
        if (invoicePanel != null)
        {
            invoicePanel.SetActive(true);
        }

        // Income
        if (passengerIncomeText != null)
        {
            int income = invoice.CalculateTotalIncome();
            passengerIncomeText.text = $"<color=green>+{income}</color> ({invoice.completedPassengers} x 20)";
        }

        // Crash Penalty
        if (crashPenaltyText != null)
        {
            if (invoice.crashCount > 0)
            {
                int basePenalty = invoice.crashCount * 500;
                if (invoice.crashPenalty == 0)
                {
                    crashPenaltyText.text = $"<color=yellow>0</color> ({invoice.crashCount} x 500 - INSURED)";
                }
                else if (invoice.crashPenalty < basePenalty)
                {
                    crashPenaltyText.text = $"<color=yellow>-{invoice.crashPenalty}</color> ({invoice.crashCount} x {invoice.crashPenalty/invoice.crashCount} - OWN REPAIR)";
                }
                else
                {
                    crashPenaltyText.text = $"<color=red>-{invoice.crashPenalty}</color> ({invoice.crashCount} x 500)";
                }
                crashPenaltyText.gameObject.SetActive(true);
            }
            else
            {
                crashPenaltyText.gameObject.SetActive(false);
            }
        }

        // Uber Penalty
        if (uberPenaltyText != null)
        {
            if (invoice.uberPickupCount > 0)
            {
                uberPenaltyText.text = $"<color=red>-{invoice.uberPenalty}</color> ({invoice.uberPickupCount} x 100)";
                uberPenaltyText.gameObject.SetActive(true);
            }
            else
            {
                uberPenaltyText.gameObject.SetActive(false);
            }
        }

        // Tax
        if (taxText != null)
        {
            if (invoice.taxAmount == 0 && invoice.passengerEarnings > 0)
            {
                taxText.text = $"<color=yellow>0</color> (TAX JOKER ACTIVE)";
            }
            else if (invoice.taxAmount > 0)
            {
                taxText.text = $"<color=red>-{invoice.taxAmount}</color> ({invoice.taxRate * 100}%)";
            }
            else
            {
                taxText.text = "0";
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
        // As requested, reload the current scene. A manager script should handle loading the correct level data upon scene start.
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        Debug.Log("Continue button clicked. Reloading scene to start next level.");
    }

    private void OnRetryButtonClicked()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        Debug.Log("Retrying current level...");
    }
}