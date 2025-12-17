using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class LoadingPanel : MonoBehaviour
{
    public static LoadingPanel Instance { get; private set; }
    
    [Header("UI References")]
    [SerializeField] private GameObject panelContainer; // The visible panel
    [SerializeField] private TextMeshProUGUI tipText;
    [SerializeField] private TextMeshProUGUI loadingText;
    
    [Header("Tips Configuration")]
    [SerializeField] private List<string> gameTips = new List<string>()
    {
        "Tip: You lose 100 coins for each passenger lost to VIP vehicles!",
        "Tip: You lose 500 coins for each crash! Make sure the road ahead is clear.",
        "Tip: Running low on coins? Replay earlier levels to earn more!",
        "Tip: Use Perks to avoid taxes or heavy repair costs!",
        "Tip: Tunnel vehicle colors are random - plan your moves carefully!",
        "Tip: Game too slow? Try playing at 2x speed!"
    };

    
    [Header("Loading Configuration")]
    [SerializeField] private float loadingDuration = 5f;
    
    private void Awake()
    {
        // Singleton pattern with DontDestroyOnLoad
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // Hide panel initially
        if (panelContainer != null)
        {
            panelContainer.SetActive(false);
        }
    }
    
    private void OnEnable()
    {
        // Subscribe to scene loaded event
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    private void OnDisable()
    {
        // Unsubscribe from scene loaded event
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    private bool isFirstLoad = true;
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Skip the first scene load (game opening)
        if (isFirstLoad)
        {
            isFirstLoad = false;
            return;
        }
        
        // Only show for AllLevel scene (build index 2) or ability tutorial scene (build index 3)
        if (scene.buildIndex == 2 || scene.buildIndex == 3)
        {
            ShowLoadingScreen();
        }

    }
    
    /// <summary>
    /// Shows the loading screen with a random tip
    /// </summary>
    public void ShowLoadingScreen()
    {
        // Show random tip
        if (tipText != null && gameTips.Count > 0)
        {
            int randomIndex = Random.Range(0, gameTips.Count);
            tipText.text = gameTips[randomIndex];
        }
        
        // Show panel
        if (panelContainer != null)
        {
            panelContainer.SetActive(true);
        }
        
        // Reset tap state
        waitingForTap = false;
        
        // Pause the game while loading
        Time.timeScale = 0f;
        if (InputManager.Instance != null)
        {
            InputManager.Instance.DisableInput();
        }
        
        // Start countdown then wait for tap
        StartCoroutine(LoadingSequence());
    }
    
    private bool waitingForTap = false;
    
    private IEnumerator LoadingSequence()
    {
        float elapsed = 0f;
        
        // Phase 1: Loading countdown (5 seconds) - using unscaled time since timeScale is 0
        while (elapsed < loadingDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            
            // Update loading text with animation
            if (loadingText != null)
            {
                int dotCount = Mathf.FloorToInt(elapsed * 2f) % 4;
                loadingText.text = "Loading" + new string('.', dotCount);
            }
            
            yield return null;
        }
        
        // Phase 2: Hide loading panel and show floating text
        if (panelContainer != null)
        {
            panelContainer.SetActive(false);
        }
        
        // Show "Tap to Screen" as floating text in level (at screen center)
        if (UIManager.Instance != null)
        {
            Vector3 screenCenter = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 10f));
            UIManager.Instance.ShowFloatingText("Tap to Screen", screenCenter);
        }
        
        waitingForTap = true;
        
        // Wait until player taps
        while (waitingForTap)
        {
            yield return null;
        }
        
        // Resume the game
        Time.timeScale = 1f;
        if (InputManager.Instance != null)
        {
            InputManager.Instance.EnableInput();
        }
    }

    
    private void Update()
    {
        // Check for tap input when waiting
        if (waitingForTap)
        {
            // Touch input
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                waitingForTap = false;
            }
            
            // Mouse input (for editor testing)
            #if UNITY_EDITOR || UNITY_STANDALONE
            if (Input.GetMouseButtonDown(0))
            {
                waitingForTap = false;
            }
            #endif
        }
    }
}
