using UnityEngine;
using TMPro;

/// <summary>
/// TextMeshPro component'ine eklenir.
/// totalStarsEarned deÄŸerini otomatik olarak gÃ¶sterir ve gÃ¼nceller.
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class StarCountText : MonoBehaviour
{
    private TextMeshProUGUI textComponent;

    private void Awake()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        // Subscribe to star change events
        JokerSystem.OnTotalStarsChanged += UpdateStarCount;
        
        // Update immediately
        UpdateStarCount();
    }

    private void OnDisable()
    {
        // Unsubscribe from events
        JokerSystem.OnTotalStarsChanged -= UpdateStarCount;
    }

    /// <summary>
    /// Update star count display
    /// </summary>
    private void UpdateStarCount()
    {
        if (textComponent == null) return;

        int starCount = 0;
        
        // Get totalStarsEarned from save data
        if (GameDataManager.Instance != null && GameDataManager.Instance.GetSaveData() != null)
        {
            starCount = GameDataManager.Instance.GetSaveData().totalStarsEarned;
        }

        // Update text
        textComponent.text = starCount.ToString();
    }

    /// <summary>
    /// Overload for event callback
    /// </summary>
    private void UpdateStarCount(int newStarCount)
    {
        if (textComponent != null)
        {
            textComponent.text = newStarCount.ToString();
        }
    }
}
