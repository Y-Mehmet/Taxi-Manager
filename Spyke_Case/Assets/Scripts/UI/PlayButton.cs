using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// "Play" butonuna eklenmek üzere tasarlanmış script.
/// Seçili level'i oynatır ve child TextMeshPro'da level numarasını gösterir.
/// </summary>
[RequireComponent(typeof(Button))]
public class PlayButton : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI levelText; // Child TextMeshPro to show selected level

    private void Awake()
    {
        // Butonun OnClick olayına LoadSelectedLevel metodunu programatik olarak ekle.
        GetComponent<Button>().onClick.AddListener(LoadSelectedLevel);
    }

    private void OnEnable()
    {
        // Subscribe to level selection events
        LevelSelectionManager.OnLevelSelected += UpdateLevelText;
        
        // Update text immediately if manager exists
        if (LevelSelectionManager.Instance != null)
        {
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
    /// SceneManager'ı çağırarak SEÇİLİ SEVİYEYİ yükler.
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
        // Bellek sızıntısını önlemek için listener'ı kaldır
        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveListener(LoadSelectedLevel);
        }
        
        // Unsubscribe from events
        LevelSelectionManager.OnLevelSelected -= UpdateLevelText;
    }
}
