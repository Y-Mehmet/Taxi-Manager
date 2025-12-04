using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Oyun iÃ§indeki sahne geÃ§iÅŸlerini yÃ¶neten merkezi sistem.
/// TÃ¼m level'lar aynÄ± sahneden (AllLevel - Build Index 1) yÃ¼klenir.
/// </summary>
public class SceneManager : Singleton<SceneManager>
{
    [Header("Scene Build Indices")]
    [Tooltip("Ana MenÃ¼ sahnesinin Build Settings'deki index'i")]
    [SerializeField] private int mainMenuBuildIndex = 0;

    [Tooltip("TÃ¼m levellerin yÃ¼klendiÄŸi sahne (AllLevel scene)")]
    [SerializeField] private int allLevelSceneBuildIndex = 1;

    /// <summary>
    /// ResourceManager'dan alÄ±nan mevcut seviyeyi yÃ¼kler.
    /// TÃ¼m level'lar aynÄ± sahneden (AllLevel) yÃ¼klenir.
    /// </summary>
    public void LoadLevelSceene()
    {
        // Always load the AllLevel scene (build index 1)
        // The actual level data is loaded based on ResourceManager.CurrentLevel
        LoadSceneByIndex(allLevelSceneBuildIndex);
    }

    /// <summary>
    /// Belirtilen level index'i iÃ§in AllLevel sahnesini yÃ¼kler.
    /// IMPORTANT: levelIndex scene build index DEÄÄ°LDÄ°R!
    /// TÃ¼m level'lar aynÄ± sahneden (build index 1) yÃ¼klenir.
    /// </summary>
    /// <param name="levelIndex">YÃ¼klenecek level'Ä±n index'i (0, 1, 2, 3...)</param>
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
        Debug.Log($"[SceneManager] Loading AllLevel scene (build index {allLevelSceneBuildIndex}) for level {levelIndex}");
        LoadSceneByIndex(allLevelSceneBuildIndex);
    }

    /// <summary>
    /// Ana menÃ¼ sahnesini yÃ¼kler.
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
        
        Debug.Log($"Loading Main Menu. Build Index: {mainMenuBuildIndex}");
        LoadSceneByIndex(mainMenuBuildIndex);
    }

    /// <summary>
    /// Verilen build index'e gÃ¶re sahneyi yÃ¼kleyen temel metot.
    /// </summary>
    private void LoadSceneByIndex(int sceneBuildIndex)
    {
        // Build Settings'de bu index'te bir sahne olup olmadÄ±ÄŸÄ±nÄ± kontrol et
        if (sceneBuildIndex < 0 || sceneBuildIndex >= UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError($"Invalid Scene Build Index: {sceneBuildIndex}. Make sure the scene is added to Build Settings.");
            return;
        }

        // TODO: Asenkron yÃ¼kleme ve bir loading ekranÄ± gÃ¶sterme mantÄ±ÄŸÄ± buraya eklenebilir.
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneBuildIndex);
    }
}
