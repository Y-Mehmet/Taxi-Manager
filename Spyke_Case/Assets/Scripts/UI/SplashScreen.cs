using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SplashScreen : MonoBehaviour
{
    [Header("Logo References")]
    [SerializeField] private GameObject studioLogoPanel;  // Studio logo container
    [SerializeField] private GameObject gameLogoPanel;    // Game logo container
    
    [Header("Configuration")]
    [SerializeField] private float studioLogoDuration = 3f;
    [SerializeField] private float gameLogoDuration = 3f;
    
    [Header("Scene Configuration")]
    [SerializeField] private int mainMenuSceneIndex = 0;  // MainMenu scene build index
    
    private void Start()
    {
        // Hide both panels initially
        if (studioLogoPanel != null) studioLogoPanel.SetActive(false);
        if (gameLogoPanel != null) gameLogoPanel.SetActive(false);
        
        // Start splash sequence
        StartCoroutine(SplashSequence());
    }
    
    private IEnumerator SplashSequence()
    {
        // Phase 1: Show Studio Logo (3 seconds)
        if (studioLogoPanel != null)
        {
            studioLogoPanel.SetActive(true);
            yield return new WaitForSeconds(studioLogoDuration);
            studioLogoPanel.SetActive(false);
        }
        
        // Phase 2: Show Game Logo (3 seconds)
        if (gameLogoPanel != null)
        {
            gameLogoPanel.SetActive(true);
            yield return new WaitForSeconds(gameLogoDuration);
            // Do NOT hide the game logo panel; keep it visible while loading the next scene
        }
        
        // Load Main Menu scene
        UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuSceneIndex);
    }
}
