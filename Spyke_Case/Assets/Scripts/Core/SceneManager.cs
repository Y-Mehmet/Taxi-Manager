using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Oyun içindeki sahne geçişlerini yöneten merkezi sistem.
/// Sahneleri Build Settings'deki index'lerine göre yükler.
/// </summary>
public class SceneManager : Singleton<SceneManager>
{
    [Header("Scene Build Indices")]
    [Tooltip("Ana Menü sahnesinin Build Settings'deki index'i")]
    [SerializeField] private int mainMenuBuildIndex = 0;

    [Tooltip("Seviye sahnelerinin başlangıç index'i. Örneğin, 0:MainMenu, 1:Level_1 ise bu değer 1 olmalı.")]
    [SerializeField] private int levelSceneBuildIndexOffset = 1;

    /// <summary>
    /// ResourceManager'dan alınan mevcut seviyeyi yükler.
    /// </summary>
    public void LoadCurrentLevel()
    {
        if (ResourceManager.Instance == null)
        {
            Debug.LogError("ResourceManager not found! Cannot determine current level.");
            return;
        }

        int currentLevelIndex = ResourceManager.Instance.CurrentLevel;
        int sceneToLoadIndex = levelSceneBuildIndexOffset + currentLevelIndex;
        
        Debug.Log($"Loading current level. Build Index: {sceneToLoadIndex}");
        LoadSceneByIndex(sceneToLoadIndex);
    }

    /// <summary>
    /// Belirtilen index'e sahip seviyeyi yükler.
    /// </summary>
    /// <param name="levelIndex">Yüklenecek seviyenin 0 tabanlı index'i.</param>
    public void LoadSpecificLevel(int levelIndex)
    {
        int sceneToLoadIndex = levelSceneBuildIndexOffset + levelIndex;

        Debug.Log($"Loading specific level. Build Index: {sceneToLoadIndex}");
        LoadSceneByIndex(sceneToLoadIndex);
    }

    /// <summary>
    /// Ana menü sahnesini yükler.
    /// </summary>
    public void LoadMainMenu()
    {
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