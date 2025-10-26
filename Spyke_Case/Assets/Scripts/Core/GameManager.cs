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
            DontDestroyOnLoad(gameObject);
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
        WagonManager.Instance.OnWagonRemoved -= HandleWagonRemoved;
    }

    private void HandleWagonRemoved(MetroWagon wagon, Transform transform)
    {
        if (CurrentState != GameState.Playing) return;

        // Check if there are any wagons left
        if (WagonManager.Instance.GetActiveWagons().Count == 0)
        {
            WinLevel();
        }
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

        // Increment level
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.SetLevelStartCount(stars);
            ResourceManager.Instance.IncrementLevel();
        }

        // TODO: Load next level or show win screen
        Debug.Log("Loading next level...");
    }

    private void HandleGameOver()
    {
        if (CurrentState != GameState.Playing) return;

        CurrentState = GameState.Lost;
        Debug.LogError("GAME OVER! You ran out of ubers.");

        // TODO: Show game over screen
    }
}
