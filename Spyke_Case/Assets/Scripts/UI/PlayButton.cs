using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// "Play" butonuna eklenmek Ã¼zere tasarlanmÄ±ÅŸ script.
/// SeÃ§ili level'i oynatÄ±r ve child TextMeshPro'da level numarasÄ±nÄ± gÃ¶sterir.
/// </summary>
[RequireComponent(typeof(Button))]
public class PlayButton : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI levelText; // Child TextMeshPro to show selected level

    private void Awake()
    {
        // Butonun OnClick olayÄ±na LoadSelectedLevel metodunu programatik olarak ekle.
        GetComponent<Button>().onClick.AddListener(LoadSelectedLevel);
    }

    private void OnEnable()
    {
        // Subscribe to level selection events
        LevelSelectionManager.OnLevelSelected += UpdateLevelText;
        
        // Force select max opened level when returning to main menu
        if (LevelSelectionManager.Instance != null && ResourceManager.Instance != null)
        {
            int maxLevel = ResourceManager.Instance.MaxOpenedLevel;
            Debug.Log($"[PlayButton] OnEnable - Forcing selection to max level: {maxLevel}");
            LevelSelectionManager.Instance.SelectLevel(maxLevel);
        }
        else if (LevelSelectionManager.Instance != null)
        {
            // Fallback: just update text with current selection
            UpdateLevelText(LevelSelectionManager.Instance.SelectedLevelIndex);
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from events
        LevelSelectionManager.OnLevelSelected -= UpdateLevelText;
    }

    /// <summary>
    /// Update level text when selection changes
    /// </summary>
    private void UpdateLevelText(int levelIndex)
    {
        if (levelText != null)
        {
            // Display as "Level 1", "Level 2", etc. (levelIndex is 0-based, so +1 for display)
            levelText.text = $"Level {levelIndex + 1}";
        }
    }

    /// <summary>
    /// SceneManager'Ä± Ã§aÄŸÄ±rarak SEÃ‡Ä°LÄ° SEVÄ°YEYÄ° yÃ¼kler.
    /// </summary>
    public void LoadSelectedLevel()
    {
        if (LevelSelectionManager.Instance != null)
        {
            LevelSelectionManager.Instance.PlaySelectedLevel();
        }
        else
        {
            Debug.LogError("[PlayButton] LevelSelectionManager not found!");
        }
    }

    private void OnDestroy()
    {
        // Bellek sÄ±zÄ±ntÄ±sÄ±nÄ± Ã¶nlemek iÃ§in listener'Ä± kaldÄ±r
        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveListener(LoadSelectedLevel);
        }
        
        // Unsubscribe from events
        LevelSelectionManager.OnLevelSelected -= UpdateLevelText;
    }
}
