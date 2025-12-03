using UnityEngine;
using System;

/// <summary>
/// Manages level selection in the level select screen
/// Singleton pattern for easy access
/// </summary>
public class LevelSelectionManager : MonoBehaviour
{
    public static LevelSelectionManager Instance { get; private set; }

    // Event fired when selected level changes
    public static event Action<int> OnLevelSelected;

    // Currently selected level index
    private int selectedLevelIndex = 0;

    public int SelectedLevelIndex => selectedLevelIndex;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // Initialize with max opened level
        if (ResourceManager.Instance != null)
        {
            selectedLevelIndex = ResourceManager.Instance.MaxOpenedLevel;
            Debug.Log($"[LevelSelectionManager] Initialized with level {selectedLevelIndex}");
            
            // Notify listeners
            OnLevelSelected?.Invoke(selectedLevelIndex);
        }
    }

    /// <summary>
    /// Select a level (called by LevelButton)
    /// </summary>
    public void SelectLevel(int levelIndex)
    {
        selectedLevelIndex = levelIndex;
        Debug.Log($"[LevelSelectionManager] Level {levelIndex} selected");
        
        // Notify all listeners (PlayButton will update its text)
        OnLevelSelected?.Invoke(selectedLevelIndex);
    }

    /// <summary>
    /// Play the currently selected level (called by PlayButton)
    /// </summary>
    public void PlaySelectedLevel()
    {
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.CurrentLevel = selectedLevelIndex;
            Debug.Log($"[LevelSelectionManager] Playing level {selectedLevelIndex}");
        }

        if (SceneManager.Instance != null)
        {
            SceneManager.Instance.LoadLevelSceene();
        }
    }
}
