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
    private bool isInitializing = false; // Prevent multiple simultaneous initializations

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

    /// <summary>
    /// Refresh the level selection to show max opened level
    /// </summary>
    private void RefreshSelection()
    {
        if (!isInitializing)
        {
            StartCoroutine(InitializeSelection());
        }
    }

    private System.Collections.IEnumerator InitializeSelection()
    {
        isInitializing = true;
        
        // Wait for end of frame to ensure all UI elements are ready
        yield return new WaitForEndOfFrame();
        
        // Initialize with max opened level
        if (ResourceManager.Instance != null)
        {
            int maxLevel = ResourceManager.Instance.MaxOpenedLevel;
            int currentLevel = ResourceManager.Instance.CurrentLevel;
            
            Debug.Log($"[LevelSelectionManager] INIT - MaxOpenedLevel: {maxLevel}, CurrentLevel: {currentLevel}");
            
            // Always select the max opened level when entering main menu
            selectedLevelIndex = maxLevel;
            Debug.Log($"[LevelSelectionManager] Selected level set to: {selectedLevelIndex}");
            
            // Notify listeners (this will trigger wheel rotation and play button update)
            OnLevelSelected?.Invoke(selectedLevelIndex);
        }
        else
        {
            Debug.LogError("[LevelSelectionManager] ResourceManager.Instance is NULL!");
        }
        
        isInitializing = false;
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
