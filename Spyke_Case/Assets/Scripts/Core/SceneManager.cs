using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Oyun içindeki sahne geçişlerini yöneten merkezi sistem.
/// Tüm level'lar aynı sahneden (AllLevel - Build Index 1) yüklenir.
/// </summary>
public class SceneManager : Singleton<SceneManager>
{
    [Header("Scene Build Indices")]
    [Tooltip("Ana Menü sahnesinin Build Settings'deki index'i")]
    [SerializeField] private int mainMenuBuildIndex = 0;

    [Tooltip("Tüm levellerin yüklendiği sahne (AllLevel scene)")]
    [SerializeField] private int allLevelSceneBuildIndex = 1;

    /// <summary>
    /// ResourceManager'dan alınan mevcut seviyeyi yükler.
    /// Tüm level'lar aynı sahneden (AllLevel) yüklenir.
    /// </summary>
    public void LoadLevelSceene()
    {
        // Always load the AllLevel scene (build index 1)
        // The actual level data is loaded based on ResourceManager.CurrentLevel
        LoadSceneByIndex(allLevelSceneBuildIndex);
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
        Debug.Log($"[SceneManager] Loading AllLevel scene (build index {allLevelSceneBuildIndex}) for level {levelIndex}");
        LoadSceneByIndex(allLevelSceneBuildIndex);
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
        
        Debug.Log($"Loading Main Menu. Build Index: {mainMenuBuildIndex}");
        LoadSceneByIndex(mainMenuBuildIndex);
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
}