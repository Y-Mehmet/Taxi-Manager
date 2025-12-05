using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Ability tutorial sahnesini yöneten script.
/// Farklı ability'leri tanıtan panelleri gösterir ve kullanıcının tutorial'ı tamamlamasını sağlar.
/// </summary>
public class AbilityTutorialManager : MonoBehaviour
{
    [Header("Tutorial Panels")]
    [SerializeField] private GameObject[] tutorialPanels; // Tüm tutorial panelleri
    
    [Header("Navigation Buttons")]
    [SerializeField] private Button nextButton;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button skipButton; // "Bir daha gösterme" butonu
    [SerializeField] private Button startGameButton; // Son panelde "Oyuna Başla" butonu
    
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI pageIndicatorText; // "1/4" gibi sayfa göstergesi
    
    private int currentPanelIndex = 0;
    
    private void Start()
    {
        // Initialize buttons
        if (nextButton != null)
            nextButton.onClick.AddListener(ShowNextPanel);
            
        if (previousButton != null)
            previousButton.onClick.AddListener(ShowPreviousPanel);
            
        if (skipButton != null)
            skipButton.onClick.AddListener(SkipTutorial);
            
        if (startGameButton != null)
            startGameButton.onClick.AddListener(StartGame);
        
        // Show first panel
        ShowPanel(0);
    }
    
    /// <summary>
    /// Belirtilen panel'i gösterir
    /// </summary>
    private void ShowPanel(int index)
    {
        if (tutorialPanels == null || tutorialPanels.Length == 0)
        {
            Debug.LogError("[AbilityTutorialManager] No tutorial panels assigned!");
            return;
        }
        
        // Clamp index
        currentPanelIndex = Mathf.Clamp(index, 0, tutorialPanels.Length - 1);
        
        // Hide all panels
        for (int i = 0; i < tutorialPanels.Length; i++)
        {
            if (tutorialPanels[i] != null)
                tutorialPanels[i].SetActive(i == currentPanelIndex);
        }
        
        // Update navigation buttons
        UpdateNavigationButtons();
        
        // Update page indicator
        UpdatePageIndicator();
        
        Debug.Log($"[AbilityTutorialManager] Showing panel {currentPanelIndex + 1}/{tutorialPanels.Length}");
    }
    
    /// <summary>
    /// Sonraki panel'i gösterir
    /// </summary>
    private void ShowNextPanel()
    {
        if (currentPanelIndex < tutorialPanels.Length - 1)
        {
            ShowPanel(currentPanelIndex + 1);
        }
    }
    
    /// <summary>
    /// Önceki panel'i gösterir
    /// </summary>
    private void ShowPreviousPanel()
    {
        if (currentPanelIndex > 0)
        {
            ShowPanel(currentPanelIndex - 1);
        }
    }
    
    /// <summary>
    /// Navigation butonlarını günceller (ilk/son panelde disable et)
    /// </summary>
    private void UpdateNavigationButtons()
    {
        if (previousButton != null)
            previousButton.interactable = currentPanelIndex > 0;
            
        if (nextButton != null)
            nextButton.interactable = currentPanelIndex < tutorialPanels.Length - 1;
            
        // Son panelde "Oyuna Başla" butonunu göster
        if (startGameButton != null)
            startGameButton.gameObject.SetActive(currentPanelIndex == tutorialPanels.Length - 1);
            
        // İlk panellerde "Oyuna Başla" butonunu gizle
        if (nextButton != null)
            nextButton.gameObject.SetActive(currentPanelIndex < tutorialPanels.Length - 1);
    }
    
    /// <summary>
    /// Sayfa göstergesini günceller
    /// </summary>
    private void UpdatePageIndicator()
    {
        if (pageIndicatorText != null)
        {
            pageIndicatorText.text = $"{currentPanelIndex + 1}/{tutorialPanels.Length}";
        }
    }
    
    /// <summary>
    /// Tutorial'ı atla ve bir daha gösterme
    /// </summary>
    private void SkipTutorial()
    {
        Debug.Log("[AbilityTutorialManager] Tutorial skipped - will not show again");
        
        // Save that tutorial is completed
        if (GameDataManager.Instance != null && GameDataManager.Instance.GetSaveData() != null)
        {
            GameDataManager.Instance.GetSaveData().isAbilityTutorialCompleted = true;
            GameDataManager.Instance.SaveGame();
        }
        
        // Load level scene
        LoadLevel();
    }
    
    /// <summary>
    /// Tutorial'ı tamamla ve oyunu başlat
    /// </summary>
    private void StartGame()
    {
        Debug.Log("[AbilityTutorialManager] Tutorial completed");
        
        // Save that tutorial is completed
        if (GameDataManager.Instance != null && GameDataManager.Instance.GetSaveData() != null)
        {
            GameDataManager.Instance.GetSaveData().isAbilityTutorialCompleted = true;
            GameDataManager.Instance.SaveGame();
        }
        
        // Load level scene
        LoadLevel();
    }
    
    /// <summary>
    /// Level sahnesini yükle
    /// </summary>
    private void LoadLevel()
    {
        if (SceneManager.Instance != null)
        {
            // Directly load AllLevel scene (build index 1)
            UnityEngine.SceneManagement.SceneManager.LoadScene(1);
        }
        else
        {
            Debug.LogError("[AbilityTutorialManager] SceneManager not found!");
        }
    }
    
    private void OnDestroy()
    {
        // Clean up button listeners
        if (nextButton != null)
            nextButton.onClick.RemoveListener(ShowNextPanel);
            
        if (previousButton != null)
            previousButton.onClick.RemoveListener(ShowPreviousPanel);
            
        if (skipButton != null)
            skipButton.onClick.RemoveListener(SkipTutorial);
            
        if (startGameButton != null)
            startGameButton.onClick.RemoveListener(StartGame);
    }
}
