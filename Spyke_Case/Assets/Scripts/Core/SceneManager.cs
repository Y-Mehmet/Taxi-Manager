using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Oyun içindeki sahne geçişlerini yöneten merkezi sistem.
/// Build Order: Splash(0) -> MainMenu(1) -> AllLevel(2) -> AbilityTutorial(3)
/// </summary>
public class SceneManager : Singleton<SceneManager>
{
    [Header("Scene Build Indices")]
    [Tooltip("Ana Menü sahnesinin Build Settings'deki index'i")]
    [SerializeField] private int _mainMenuIndex = 1;

    [Tooltip("Tüm levellerin yüklendiği sahne (AllLevel scene)")]
    [SerializeField] private int _allLevelIndex = 2;

    [Tooltip("Ability öğretici sahnesinin Build Settings'deki index'i")]
    [SerializeField] private int _tutorialIndex = 3;


    /// <summary>
    /// ResourceManager'dan alınan mevcut seviyeyi yükler.
    /// Belirli levellerde ability tutorial'ı gösterir (ilk kez açıldığında).
    /// </summary>
    public void LoadLevelSceene()
    {
        int currentLevel = ResourceManager.Instance.CurrentLevel;
        
        // Check if we should show ability tutorial for this level
        if (GameDataManager.Instance != null && GameDataManager.Instance.GetSaveData() != null)
        {
            SaveGameData saveData = GameDataManager.Instance.GetSaveData();
            
            // Add Stop Tutorial - check against configured unlock level
            if (currentLevel == saveData.abilityAddNewStopUnlockLevel && !saveData.hasSeenAddStopTutorial)
            {
                Debug.Log($"[SceneManager] Level {currentLevel} - Loading Add Stop Tutorial");
                saveData.currentAbilityTutorial = "AddStop";
                GameDataManager.Instance.SaveGame();
                LoadSceneByIndex(_tutorialIndex);
                return;
            }
            
            // Universal Pathfinding Tutorial - check against configured unlock level
            if (currentLevel == saveData.abilityUniversalPathfindingUnlockLevel && !saveData.hasSeenUniversalPathfindingTutorial)
            {
                Debug.Log($"[SceneManager] Level {currentLevel} - Loading Universal Pathfinding Tutorial");
                saveData.currentAbilityTutorial = "UniversalPathfinding";
                GameDataManager.Instance.SaveGame();
                LoadSceneByIndex(_tutorialIndex);
                return;
            }
            
            // Flasher Tutorial - check against configured unlock level
            if (currentLevel == saveData.abilityRemoveWagonsUnlockLevel && !saveData.hasSeenFlasherTutorial)
            {
                Debug.Log($"[SceneManager] Level {currentLevel} - Loading Flasher Tutorial");
                saveData.currentAbilityTutorial = "Flasher";
                GameDataManager.Instance.SaveGame();
                LoadSceneByIndex(_tutorialIndex);
                return;
            }
            
            // Shuffle Tutorial - check against configured unlock level
            if (currentLevel == saveData.abilityShuffleWagonColorsUnlockLevel && !saveData.hasSeenShuffleTutorial)
            {
                Debug.Log($"[SceneManager] Level {currentLevel} - Loading Shuffle Tutorial");
                saveData.currentAbilityTutorial = "Shuffle";
                GameDataManager.Instance.SaveGame();
                LoadSceneByIndex(_tutorialIndex);
                return;
            }
        }
        
        // No tutorial needed, load level directly
        Debug.Log($"[SceneManager] Loading Level {currentLevel} directly");
        LoadSceneByIndex(_allLevelIndex);
    }

    /// <summary>
    /// Belirtilen level index'i için AllLevel sahnesini yükler.
    /// IMPORTANT: levelIndex scene build index DEĞİLDİR!
    /// Tüm level'lar aynı sahneden (build index 1) yüklenir.
    /// </summary>
    /// <param name="levelIndex">Yüklenecek level'ın index'i (0, 1, 2, 3...)</param>
    public void LoadSpecificLevel(int levelIndex)
    {
        // Set the level index in ResourceManager BEFORE loading the scene
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.CurrentLevel = levelIndex;
            Debug.Log($"[SceneManager] Set CurrentLevel to {levelIndex}");
        }

        // Always load the same scene (AllLevel - build index 1)
        // The level data will be loaded based on ResourceManager.CurrentLevel
        Debug.Log($"[SceneManager] Loading AllLevel scene (build index {_allLevelIndex}) for level {levelIndex}");
        LoadSceneByIndex(_allLevelIndex);
    }

    /// <summary>
    /// Ana menü sahnesini yükler.
    /// </summary>
    public void LoadMainMenu()
    {
        // Set CurrentLevel to the highest unlocked level before loading main menu
        // This ensures the level selection shows the latest unlocked level
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.CurrentLevel = ResourceManager.Instance.MaxOpenedLevel;
            Debug.Log($"[SceneManager] Set CurrentLevel to MaxOpenedLevel: {ResourceManager.Instance.MaxOpenedLevel}");
        }
        
        Debug.Log($"Loading Main Menu. Build Index: {_mainMenuIndex}");
        LoadSceneByIndex(_mainMenuIndex);
    }

    /// <summary>
    /// Verilen build index'e göre sahneyi yükleyen temel metot.
    /// </summary>
    private void LoadSceneByIndex(int sceneBuildIndex)
    {
        // Build Settings'de bu index'te bir sahne olup olmadığını kontrol et
        if (sceneBuildIndex < 0 || sceneBuildIndex >= UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError($"Invalid Scene Build Index: {sceneBuildIndex}. Make sure the scene is added to Build Settings.");
            return;
        }

        // TODO: Asenkron yükleme ve bir loading ekranı gösterme mantığı buraya eklenebilir.
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneBuildIndex);
    }


    /// <summary>
    /// Şu anki sahneyi yeniden yükler (Restart için).
    /// </summary>
    public void LoadCurrentScene()
    {
        int currentSceneIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
        Debug.Log($"[SceneManager] Reloading current scene (Build Index: {currentSceneIndex})");
        LoadSceneByIndex(currentSceneIndex);
    }
}
