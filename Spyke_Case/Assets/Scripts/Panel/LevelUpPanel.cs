using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class LevelUpPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI levelCompletedText; // Shows "Level X Completed!"
    [SerializeField] private TextMeshProUGUI earningsText;
    [SerializeField] private List<Image> starImages;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button retryButton;

    [Header("Star Sprites")]
    [SerializeField] private Sprite brightStar;
    [SerializeField] private Sprite greyStar;

    [Header("Invoice UI - Table Structure")]
    [SerializeField] private GameObject invoicePanel;
    [SerializeField] private Transform innerPanel; // Parent with 3 children: Column0(QTY), Column1(Description), Column2(Amount)
    
    // Table structure: 3 columns x 9 rows
    // Row 0: Decorative line (---)
    // Row 1: Headers (QTY, DESCRIPTION, AMOUNT)
    // Row 2: Decorative line (---)
    // Row 3: Passenger Income
    // Row 4: Crash Penalty
    // Row 5: Uber Penalty
    // Row 6: Subtotal
    // Row 7: Tax
    // Row 8: Net Earnings

    private void Awake()
    {
        continueButton.onClick.AddListener(OnContinueButtonClicked);
        retryButton.onClick.AddListener(OnRetryButtonClicked);
    }

    public void Show(int stars, int earnings, int completedLevelIndex = -1)
    {
        // Show level number with animation
        if (levelCompletedText != null)
        {
            int displayIdx = 0;
            if (completedLevelIndex != -1)
            {
                displayIdx = completedLevelIndex;
            }
            else
            {
                displayIdx = ResourceManager.Instance != null ? ResourceManager.Instance.CurrentLevel : 0;
            }

            levelCompletedText.text = $"{displayIdx + 1}";
            
            // Scale animation
            levelCompletedText.transform.localScale = Vector3.zero;
            levelCompletedText.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack);
        }

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
        if (innerPanel == null)
        {
            Debug.LogError("[LevelUpPanel] innerPanel is not assigned!");
            return;
        }

        // Get the 3 columns
        if (innerPanel.childCount < 3)
        {
            Debug.LogError($"[LevelUpPanel] innerPanel should have 3 children (columns), but has {innerPanel.childCount}!");
            return;
        }

        Transform qtyColumn = innerPanel.GetChild(0);      // Column 0: QTY
        Transform descColumn = innerPanel.GetChild(1);     // Column 1: Description
        Transform amountColumn = innerPanel.GetChild(2);   // Column 2: Amount

        // Verify each column has 10 TextMeshPro children
        if (qtyColumn.childCount < 10 || descColumn.childCount < 10 || amountColumn.childCount < 10)
        {
            Debug.LogError($"[LevelUpPanel] Each column should have 10 TextMeshPro children! QTY:{qtyColumn.childCount}, Desc:{descColumn.childCount}, Amount:{amountColumn.childCount}");
            return;
        }

        // Calculate all values
        int income = invoice.CalculateTotalIncome();
        invoice.CalculateTotalExpenses(); // This updates crashPenalty, uberPenalty, boosterCost, taxAmount
        int subtotal = income - invoice.crashPenalty - invoice.uberPenalty - invoice.boosterCost;
        int netEarnings = invoice.CalculateNetEarnings();

        // Row 0, 1, 2: Skip (decorative lines and headers - already set in Unity)
        
        // Row 3: Passenger Income
        SetTableRow(qtyColumn, descColumn, amountColumn, 3,
            invoice.completedPassengers.ToString(),
            "Passenger Income",
            $"+{income}");

        // Row 4: Crash Penalty
        string crashQty = invoice.crashCount > 0 ? invoice.crashCount.ToString() : "";
        string crashDesc = "Crash Penalty";
        string crashAmount = "";
        
        if (invoice.crashCount > 0)
        {
            int basePenalty = invoice.crashCount * 500;
            
            if (invoice.crashPenalty == 0)
            {
                // Insurance active - free repair
                crashDesc = "Crash Penalty <color=green>(P)</color>";
                crashAmount = "0";
            }
            else if (invoice.crashPenalty < basePenalty)
            {
                // Own repair station - reduced cost
                crashDesc = "Crash Penalty <color=green>(P)</color>";
                crashAmount = $"-{invoice.crashPenalty}";
            }
            else
            {
                // No perk - full cost
                crashDesc = "Crash Penalty";
                crashAmount = $"-{invoice.crashPenalty}";
            }
        }
        else
        {
            crashAmount = "0";
        }
        
        SetTableRow(qtyColumn, descColumn, amountColumn, 4, crashQty, crashDesc, crashAmount);

        // Row 5: Uber Penalty
        string uberQty = invoice.uberPickupCount > 0 ? invoice.uberPickupCount.ToString() : "";
        string uberAmount = invoice.uberPickupCount > 0 ? $"-{invoice.uberPenalty}" : "0";
        SetTableRow(qtyColumn, descColumn, amountColumn, 5,
            uberQty,
            "Uber Penalty",
            uberAmount);

        // Row 6: Booster Cost
        string boosterAmount = invoice.boosterCost > 0 ? $"-{invoice.boosterCost}" : "0";
        SetTableRow(qtyColumn, descColumn, amountColumn, 6,
            "",
            "Booster Cost",
            boosterAmount);

        // Row 7: Subtotal
        SetTableRow(qtyColumn, descColumn, amountColumn, 7,
            "",
            "Subtotal",
            subtotal >= 0 ? $"+{subtotal}" : $"{subtotal}");

        // Row 7: Tax
        string taxDesc = "Tax";
        string taxAmount = "";
        
        if (invoice.taxAmount == 0 && subtotal > 0)
        {
            // Tax joker active - 0% tax
            taxDesc = "Tax (0%) <color=green>(P)</color>";
            taxAmount = "0";
        }
        else if (invoice.taxAmount > 0)
        {
            int taxPercent = Mathf.RoundToInt(invoice.taxRate * 100);
            
            // Check if tax rate is reduced (less than 20%)
            if (invoice.taxRate < 0.20f)
            {
                taxDesc = $"Tax ({taxPercent}%) <color=green>(P)</color>";
            }
            else
            {
                taxDesc = $"Tax ({taxPercent}%)";
            }
            
            taxAmount = $"-{invoice.taxAmount}";
        }
        else
        {
            taxDesc = "Tax (0%)";
            taxAmount = "0";
        }
        
        SetTableRow(qtyColumn, descColumn, amountColumn, 8, "", taxDesc, taxAmount);


        // Row 9: Net Earnings
        SetTableRow(qtyColumn, descColumn, amountColumn, 9,
            "",
            "NET EARNINGS",
            netEarnings >= 0 ? $"+{netEarnings}" : $"{netEarnings}");

        // Update old earnings text for compatibility
        if (earningsText != null)
        {
            earningsText.text = $"{netEarnings}";
        }
    }

    /// <summary>
    /// Helper method to set text for a specific row in the table
    /// </summary>
    private void SetTableRow(Transform qtyCol, Transform descCol, Transform amountCol, int rowIndex, 
        string qtyText, string descText, string amountText)
    {
        // Set QTY column
        TextMeshProUGUI qtyTMP = qtyCol.GetChild(rowIndex).GetComponent<TextMeshProUGUI>();
        if (qtyTMP != null)
        {
            qtyTMP.text = qtyText;
        }
        else
        {
//             Debug.LogWarning($"[LevelUpPanel] QTY column row {rowIndex} has no TextMeshProUGUI component!");
        }

        // Set Description column
        TextMeshProUGUI descTMP = descCol.GetChild(rowIndex).GetComponent<TextMeshProUGUI>();
        if (descTMP != null)
        {
            descTMP.text = descText;
        }
        else
        {
//             Debug.LogWarning($"[LevelUpPanel] Description column row {rowIndex} has no TextMeshProUGUI component!");
        }

        // Set Amount column
        TextMeshProUGUI amountTMP = amountCol.GetChild(rowIndex).GetComponent<TextMeshProUGUI>();
        if (amountTMP != null)
        {
            amountTMP.text = amountText;
        }
        else
        {
//             Debug.LogWarning($"[LevelUpPanel] Amount column row {rowIndex} has no TextMeshProUGUI component!");
        }
    }

    private void OnContinueButtonClicked()
    {
        // Transfer coins from temp to main (apply net balance)
        // Level is COMPLETED, so booster cost is NOT refunded
        if (GameManager.Instance != null && GameManager.Instance.CurrentInvoice != null)
        {
            if (GameEconomy.Instance != null)
            {
                GameEconomy.Instance.TransferTempToMain(GameManager.Instance.CurrentInvoice);
                /* Debug.Log("[LevelUpPanel] Net balance applied to main account (Continue)."); */
            }
        }

        // Load Main Scene (build index 0) - Level Select screen
        if (SceneManager.Instance != null)
        {
            SceneManager.Instance.LoadMainMenu();
            /* Debug.Log("[LevelUpPanel] Continue clicked. Loading Main Scene (Level Select)."); */
        }
        else
        {
            // Fallback: Load build index 0 directly
            UnityEngine.SceneManagement.SceneManager.LoadScene(1); // Load MainMenu
            /* Debug.Log("[LevelUpPanel] Continue clicked. Loading Main Scene (build index 0)."); */
        }
    }

    private void OnRetryButtonClicked()
    {
        // DO NOT transfer net balance (level not completed)
        // Only refund booster cost (player wants to retry)
        if (GameManager.Instance != null && GameManager.Instance.CurrentInvoice != null)
        {
            int boosterCost = GameManager.Instance.CurrentInvoice.boosterCost;
            if (boosterCost > 0 && GameEconomy.Instance != null)
            {
                GameEconomy.Instance.AddMainCoins(boosterCost);
                /* Debug.Log($"[LevelUpPanel] Refunded {boosterCost} coins for booster usage (Retry)."); */
            }
        }

        // Reload current level (AllLevel scene with same level data)
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        /* Debug.Log("Retrying current level..."); */
    }
}
