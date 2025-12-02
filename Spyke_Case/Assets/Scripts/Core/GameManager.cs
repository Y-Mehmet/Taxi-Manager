using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Playing, Won, Lost }
    public GameState CurrentState { get; private set; }

    // Level Invoice System
    public LevelInvoiceData CurrentInvoice { get; private set; }


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
          //  DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        CurrentState = GameState.Playing;
        UberManager.OnGameOver += HandleGameOver;
        WagonManager.Instance.OnWagonRemoved += HandleWagonRemoved;
        
        // Initialize invoice for this level
        InitializeInvoice();
        
        Debug.LogWarning("GameManager started.");
    }

    private void InitializeInvoice()
    {
        CurrentInvoice = new LevelInvoiceData();
        
        // Notify joker system that game started (decrements counters)
        if (JokerSystem.Instance != null)
        {
            JokerSystem.Instance.OnGameStarted();
            Debug.Log($"[GameManager] Invoice initialized with active jokers");
        }
        
        // Reset ability usage tracker
        if (AbilityUsageTracker.Instance != null)
        {
            AbilityUsageTracker.Instance.ResetUsageCounts();
        }
        
        // Reset temp coins
        if (GameEconomy.Instance != null)
        {
            GameEconomy.Instance.ResetTempCoins();
        }
    }


    private void OnDestroy()
    {
        UberManager.OnGameOver -= HandleGameOver;
        if (WagonManager.Instance != null)
        {
            WagonManager.Instance.OnWagonRemoved -= HandleWagonRemoved;
        }
    }

    private void HandleWagonRemoved(MetroWagon wagon, Transform transform)
    {
        if (CurrentState != GameState.Playing) return;

        CheckWinCondition(); // Call the new method
    }

    private void WinLevel()
    {
        CurrentState = GameState.Won;

        int remainingUbers = UberManager.Instance.maxUberCount - UberManager.Instance.UberCount;
        int stars = 0;

        if (remainingUbers >= 9)
        {
            stars = 3;
        }
        else if (remainingUbers >= 5)
        {
            stars = 2;
        }
        else if (remainingUbers >= 0) // 0-5
        {
            stars = 1;
        }

        Debug.LogWarning($"<color=green>LEVEL WON!</color> You earned {stars} stars.");

        // Print invoice (but DON'T transfer coins yet - wait for Continue/Retry button)
        if (CurrentInvoice != null)
        {
            CurrentInvoice.PrintInvoice();
            
            // DON'T transfer coins here - let LevelUpPanel do it when player clicks Continue/Retry
            // if (GameEconomy.Instance != null)
            // {
            //     GameEconomy.Instance.TransferTempToMain(CurrentInvoice);
            // }
        }

        // Save stars (only if higher than previous)
        if (ResourceManager.Instance != null)
        {
            int currentLevel = ResourceManager.Instance.CurrentLevel;
            
            // Get previous star count for this level
            int previousStars = 0;
            if (ResourceManager.Instance.LevelStars != null && 
                currentLevel < ResourceManager.Instance.LevelStars.Count)
            {
                previousStars = ResourceManager.Instance.LevelStars[currentLevel];
            }
            
            // Only save if new stars are higher
            if (stars > previousStars)
            {
                ResourceManager.Instance.SetLevelStarCount(currentLevel, stars);
                Debug.Log($"[GameManager] New star record! {previousStars} -> {stars}");
            }
            else
            {
                Debug.Log($"[GameManager] Stars not saved. Previous: {previousStars}, Current: {stars}");
            }
            
            // IMPORTANT: Only increment level if we're playing the CURRENT highest unlocked level
            // This prevents re-locking levels when replaying old levels
            int justCompletedLevel = currentLevel;
            int highestUnlockedLevel = ResourceManager.Instance.MaxOpenedLevel;
            
            if (justCompletedLevel >= highestUnlockedLevel)
            {
                // We completed the highest unlocked level or beyond, unlock the next one
                ResourceManager.Instance.IncrementLevel();
                Debug.Log($"[GameManager] Level progression: {highestUnlockedLevel} -> {highestUnlockedLevel + 1}");
            }
            else
            {
                Debug.Log($"[GameManager] Replaying old level {justCompletedLevel}. Highest unlocked: {highestUnlockedLevel}. No progression.");
            }
        }

        StartCoroutine(ShowLevelUpPanelRoutine(stars));
    }


    private System.Collections.IEnumerator ShowLevelUpPanelRoutine(int stars)
    {
        yield return new WaitForSeconds(2f);

        // TODO: Load next level or show win screen
        PanelManager.Instance.ShowPanel(PanelID.LevelUpPanel);
        var panelInstanceModel = PanelManager.Instance.GetLastPanel();
        if (panelInstanceModel != null)
        {
            LevelUpPanel levelUpPanel = panelInstanceModel.PanelInstance.GetComponent<LevelUpPanel>();
            if (levelUpPanel != null)
            {
                int finalCoins = GameEconomy.Instance != null ? GameEconomy.Instance.GetCurrentCoins() : 0;
                levelUpPanel.Show(stars, finalCoins);
            }
        }
        Debug.Log("Loading next level...");
    }

    private void HandleGameOver()
    {
        if (CurrentState != GameState.Playing) return;

        CurrentState = GameState.Lost;
        Debug.LogError("GAME OVER! You ran out of ubers.");

        // TODO: Show game over screen
        PanelManager.Instance.ShowPanel(PanelID.TryAgainPanel);
    }

    public void CheckWinCondition()
    {
        if (CurrentState != GameState.Playing) return;

        bool noWagonsLeft = WagonManager.Instance.GetActiveWagons().Count == 0;
        //bool noPassengersAtStops = StopManager.Instance.GetOccupiedStops().Count == 0;
        //bool noUnderpassPassengers = UnderpassManager.Instance.AreAllQueuesEmpty();
        // TODO: Add check for conveyor passengers if needed. For now, assume they are handled by other means.

        if (noWagonsLeft)
        {
            WinLevel();
        }
    }
}
