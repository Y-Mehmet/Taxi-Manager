using UnityEngine;
using TMPro;

public class CoinDisplayUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI coinText;

    private void OnEnable()
    {
        if (ResourceManager.Instance != null)
        {
            // Subscribe to the event that fires when coins change
            ResourceManager.Instance.OnCoinsChanged += UpdateCoinText;
            // Set the initial value
            UpdateCoinText(ResourceManager.Instance.CurrentCoins);
        }
    }

    private void OnDisable()
    {
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.OnCoinsChanged -= UpdateCoinText;
        }
    }

    private void UpdateCoinText(int newCoinAmount)
    {
        if (coinText != null)
        {
            coinText.text = newCoinAmount.ToString();
        }
    }
}
