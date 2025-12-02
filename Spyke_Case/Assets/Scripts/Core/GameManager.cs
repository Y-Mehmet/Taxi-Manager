using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Playing, Won, Lost }
    public GameState CurrentState { get; private set; }

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
        Debug.LogWarning("GameManager started.");
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

        // Add earnings to total and increment level
        if (ResourceManager.Instance != null)
        {
            int finalCoins = GameEconomy.Instance != null ? GameEconomy.Instance.GetCurrentCoins() : 0;
            ResourceManager.Instance.SetLevelStarCount(ResourceManager.Instance.CurrentLevel, stars);
            ResourceManager.Instance.IncrementLevel();
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
