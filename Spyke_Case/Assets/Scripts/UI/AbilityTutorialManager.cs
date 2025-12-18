using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Ability tutorial sahnesini yöneten script.
/// SaveGameData'dan hangi ability tutorial'ının gösterileceğini okur ve ilgili prefab'ı spawn eder.
/// </summary>
public class AbilityTutorialManager : MonoBehaviour
{
    [Header("Tutorial Panel Prefabs")]
    [SerializeField] private GameObject addStopTutorialPrefab; // Level 4
    [SerializeField] private GameObject universalPathfindingTutorialPrefab; // Level 8
    [SerializeField] private GameObject flasherTutorialPrefab; // Level 16
    [SerializeField] private GameObject shuffleTutorialPrefab; // Level 32
    
    [Header("Spawn Parent")]
    [SerializeField] private Transform panelContainer; // Prefab'ların spawn edileceği parent (Canvas)
    
    [Header("Navigation Button")]
    [SerializeField] private Button continueButton; // Continue/Skip butonu (level'a geçiş için)
    
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI selectionText; // Tutorial açıklama metni (AbilityTutorialButton'a verilecek)
    [SerializeField] private TypewriterEffect typewriterEffect; // Typewriter efekti (AbilityTutorialButton'a verilecek)
    
    private GameObject spawnedPanel; // Spawn edilen panel
    private AbilityType currentAbilityType;
    
    private void Start()
    {
        // Read current ability tutorial from SaveGameData
        if (GameDataManager.Instance != null && GameDataManager.Instance.GetSaveData() != null)
        {
            string tutorialType = GameDataManager.Instance.GetSaveData().currentAbilityTutorial;
            
            /* Debug.Log($"[AbilityTutorialManager] Current tutorial type from SaveData: {tutorialType}"); */
            
            // Spawn appropriate panel based on tutorial type
            SpawnTutorialPanel(tutorialType);
        }
        else
        {
            Debug.LogError("[AbilityTutorialManager] GameDataManager or SaveData is null!");
        }
        
        // Initialize continue button
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueButtonClicked);
        }
    }
    
    /// <summary>
    /// Tutorial type'a göre ilgili prefab'ı spawn eder
    /// </summary>
    private void SpawnTutorialPanel(string tutorialType)
    {
        GameObject prefabToSpawn = null;
        
        switch (tutorialType)
        {
            case "AddStop":
                prefabToSpawn = addStopTutorialPrefab;
                currentAbilityType = AbilityType.AddNewStop;
                /* Debug.Log("[AbilityTutorialManager] Spawning Add Stop Tutorial"); */
                break;
                
            case "UniversalPathfinding":
                prefabToSpawn = universalPathfindingTutorialPrefab;
                currentAbilityType = AbilityType.UniversalPathfinding;
                /* Debug.Log("[AbilityTutorialManager] Spawning Universal Pathfinding Tutorial"); */
                break;
                
            case "Flasher":
                prefabToSpawn = flasherTutorialPrefab;
                currentAbilityType = AbilityType.RemoveWagons;
                /* Debug.Log("[AbilityTutorialManager] Spawning Flasher Tutorial"); */
                break;
                
            case "Shuffle":
                prefabToSpawn = shuffleTutorialPrefab;
                currentAbilityType = AbilityType.ShuffleWagonColors;
                /* Debug.Log("[AbilityTutorialManager] Spawning Shuffle Tutorial"); */
                break;
                
            default:
                Debug.LogError($"[AbilityTutorialManager] Unknown tutorial type: {tutorialType}");
                // Fallback to Add Stop
                prefabToSpawn = addStopTutorialPrefab;
                currentAbilityType = AbilityType.AddNewStop;
                break;
        }
        
        // Spawn prefab
        if (prefabToSpawn != null && panelContainer != null)
        {
            spawnedPanel = Instantiate(prefabToSpawn, panelContainer);
            
            // Reset transform
            RectTransform rectTransform = spawnedPanel.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.localScale = Vector3.one;
            }
            
            /* Debug.Log($"[AbilityTutorialManager] Spawned panel: {spawnedPanel.name}"); */
            
            // Find and connect AbilityTutorialButton to this manager
            ConnectTutorialButton();
        }
        else
        {
            if (prefabToSpawn == null)
                Debug.LogError("[AbilityTutorialManager] Prefab is null!");
            if (panelContainer == null)
                Debug.LogError("[AbilityTutorialManager] Panel container is null!");
        }
    }
    
    /// <summary>
    /// Spawn edilen panel içindeki AbilityTutorialButton'ı bulur ve manager referansını set eder
    /// </summary>
    private void ConnectTutorialButton()
    {
        if (spawnedPanel == null) return;
        
        AbilityTutorialButton tutorialButton = spawnedPanel.GetComponentInChildren<AbilityTutorialButton>();
        
        if (tutorialButton != null)
        {
            // Set manager reference and UI components
            tutorialButton.SetTutorialManager(this);
            tutorialButton.SetUIComponents(selectionText, typewriterEffect);
            /* Debug.Log("[AbilityTutorialManager] Connected tutorial button to manager and set UI components"); */
        }
        else
        {
            /* Debug.LogWarning("[AbilityTutorialManager] No AbilityTutorialButton found in spawned panel!"); */
        }
    }
    
    // OnTutorialCompleted and AutoSkipCountdown removed - no auto-skip
    // User must click Continue button to proceed to level
    
    /// <summary>
    /// Continue button tıklandığında - tutorial'ı tamamla ve level'ı yükle
    /// </summary>
    private void OnContinueButtonClicked()
    {
        /* Debug.Log("[AbilityTutorialManager] Continue button clicked"); */
        CompleteTutorialAndLoadLevel();
    }
    
    /// <summary>
    /// Tutorial'ı tamamla ve level'ı yükle
    /// </summary>
    private void CompleteTutorialAndLoadLevel()
    {
        // Save that this specific ability tutorial has been seen
        if (GameDataManager.Instance != null && GameDataManager.Instance.GetSaveData() != null)
        {
            SaveGameData saveData = GameDataManager.Instance.GetSaveData();
            
            switch (currentAbilityType)
            {
                case AbilityType.AddNewStop:
                    saveData.hasSeenAddStopTutorial = true;
                    break;
                case AbilityType.UniversalPathfinding:
                    saveData.hasSeenUniversalPathfindingTutorial = true;
                    break;
                case AbilityType.RemoveWagons: // Flasher
                    saveData.hasSeenFlasherTutorial = true;
                    break;
                case AbilityType.ShuffleWagonColors:
                    saveData.hasSeenShuffleTutorial = true;
                    break;
            }
            
            // Clear current tutorial type
            saveData.currentAbilityTutorial = "";
            
            GameDataManager.Instance.SaveGame();
            /* Debug.Log($"[AbilityTutorialManager] Saved {currentAbilityType} tutorial as seen"); */
        }
        
        // Load level scene
        LoadLevel();
    }
    
    /// <summary>
    /// Level sahnesini yükle
    /// </summary>
    private void LoadLevel()
    {
        /* Debug.Log("[AbilityTutorialManager] Loading AllLevel scene (build index 1)"); */
        // Directly load AllLevel scene (build index 1)
        UnityEngine.SceneManagement.SceneManager.LoadScene(2); // Go back to AllLevel
    }
    
    private void OnDestroy()
    {
        // Clean up button listener
        if (continueButton != null)
            continueButton.onClick.RemoveListener(OnContinueButtonClicked);
            
        // Destroy spawned panel
        if (spawnedPanel != null)
            Destroy(spawnedPanel);
    }
}
