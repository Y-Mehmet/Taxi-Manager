using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Seviye seÃ§im ekranÄ±ndaki her bir butona eklenmek Ã¼zere tasarlanmÄ±ÅŸ script.
/// Butonun hiyerarÅŸideki sÄ±rasÄ±nÄ± (sibling index) alarak ilgili seviyeyi yÃ¼kler.
/// </summary>
[RequireComponent(typeof(Button))]
public class LevelButton : MonoBehaviour
{
    [Header("Star Display")]
    [SerializeField] private Transform starPanel; // Panel with 3 child GameObjects (star containers)
    
    [Header("Wheel Rotation")]
    [SerializeField] private RotateZAxis wheelRotator; // Reference to the wheel's RotateZAxis component
    
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(LoadLevel);
    }

    private void OnEnable()
    {
        UpdateStarDisplay();
        
        // Subscribe to level selection events
        LevelSelectManager.OnLevelSelected += HandleLevelSelected;
    }
    
    private void OnDisable()
    {
        // Unsubscribe from level selection events
        LevelSelectManager.OnLevelSelected -= HandleLevelSelected;
    }
    
    /// <summary>
    /// Handle level selection event - start/stop wheel rotation
    /// </summary>
    private void HandleLevelSelected(int selectedLevelIndex)
    {
        int myLevelIndex = transform.GetSiblingIndex();
        
        if (wheelRotator != null)
        {
            if (selectedLevelIndex == myLevelIndex)
            {
                // This is the selected level, start rotation
                wheelRotator.StartRotation();
            }
            else
            {
                // This is not the selected level, stop rotation
                wheelRotator.StopRotation();
            }
        }
    }

    /// <summary>
    /// Update star display based on saved stars for this level
    /// </summary>
    private void UpdateStarDisplay()
    {
        if (starPanel == null) return;
        
        // Get level index from sibling index
        int levelIndex = transform.GetSiblingIndex();
        
        // Get stars for this level from save data
        int stars = 0;
        if (ResourceManager.Instance != null && ResourceManager.Instance.LevelStars != null)
        {
            if (levelIndex < ResourceManager.Instance.LevelStars.Count)
            {
                stars = ResourceManager.Instance.LevelStars[levelIndex];
            }
        }
        
        // Ensure star panel has 3 children
        if (starPanel.childCount < 3)
        {
            return; // Silently skip if not configured
        }
        
        // Update each star container
        for (int i = 0; i < 3; i++)
        {
            Transform starContainer = starPanel.GetChild(i);
            
            // Each star container should have a child (the filled star)
            if (starContainer.childCount > 0)
            {
                GameObject filledStar = starContainer.GetChild(0).gameObject;
                
                // Activate filled star if this level has enough stars
                filledStar.SetActive(i < stars);
            }
        }
    }

    /// <summary>
    /// Bu butona tÄ±klandÄ±ÄŸÄ±nda Ã§aÄŸrÄ±lÄ±r.
    /// ArtÄ±k level'i yÃ¼kleme, sadece SEÃ‡.
    /// PlayButton seÃ§ili level'i yÃ¼kleyecek.
    /// </summary>
    public void LoadLevel()
    {
        // Butonun interactable deÄŸilse iÅŸlem yapma (zaten tÄ±klanamaz ama garanti olsun)
        if (!button.interactable) return;

        // HiyerarÅŸideki sÄ±rayÄ± al (bu bizim level index'imiz olacak)
        int levelIndex = transform.GetSiblingIndex();

        Debug.Log($"[LevelButton] Level {levelIndex} selected (not loaded yet)");

        // LevelSelectManager'a seÃ§ili level'i bildir
        if (LevelSelectManager.Instance != null)
        {
            LevelSelectManager.Instance.SelectLevel(levelIndex);
        }
        else
        {
            Debug.LogError("[LevelButton] LevelSelectManager not found!");
        }
    }

    private void OnDestroy()
    {
        // Bellek sÄ±zÄ±ntÄ±sÄ±nÄ± Ã¶nlemek iÃ§in listener'Ä± kaldÄ±r
        if (button != null)
        {
            button.onClick.RemoveListener(LoadLevel);
        }
    }
}
