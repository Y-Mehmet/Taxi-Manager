
using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class LevelEarningsDisplayUI : MonoBehaviour
{
    private TMP_Text earningsText;

    private void Awake()
    {
        earningsText = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        // Subscribe to the global coin change event from the ResourceManager
        ResourceManager.OnCoinsChanged += UpdateEarningsText;
        // Update text immediately on enable with the global coin count
        if (ResourceManager.Instance != null)
        {
            UpdateEarningsText(ResourceManager.Instance.CurrentCoins);
        }
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        ResourceManager.OnCoinsChanged -= UpdateEarningsText;
    }

    private void UpdateEarningsText(int newAmount)
    {
        if (earningsText != null)
        {
            earningsText.text = newAmount.ToString();
        }
    }
}
