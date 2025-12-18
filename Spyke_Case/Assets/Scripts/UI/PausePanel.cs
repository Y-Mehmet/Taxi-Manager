using UnityEngine;
using UnityEngine.UI;

public class PausePanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button continueButton; // Continue playing
    [SerializeField] private Button restartButton;  // Restart level
    
    private void OnEnable()
    {
        // Setup button listeners
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueClicked);
        }
        
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartClicked);
        }
    }
    
    private void OnDisable()
    {
        // Remove listeners
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(OnContinueClicked);
        }
        
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(OnRestartClicked);
        }
    }
    
    private void OnContinueClicked()
    {
        // Resume game and close panel
        if (PauseManager.Instance != null)
        {
            PauseManager.Instance.ResumeGame();
        }
    }
    
    private void OnRestartClicked()
    {
        // Refund booster cost if used (player is restarting mid-game)
        if (GameManager.Instance != null && GameManager.Instance.CurrentInvoice != null)
        {
            int boosterCost = GameManager.Instance.CurrentInvoice.boosterCost;
            if (boosterCost > 0 && GameEconomy.Instance != null)
            {
                GameEconomy.Instance.AddMainCoins(boosterCost);
                /* Debug.Log($"[PausePanel] Refunded {boosterCost} coins for booster usage (Restart)."); */
            }
        }
        
        // First resume time scale so scene can load properly
        Time.timeScale = 1f;
        
        // Reload current scene
        if (SceneManager.Instance != null)
        {
            SceneManager.Instance.LoadCurrentScene();
        }
        else
        {
            // Fallback: use Unity's SceneManager directly
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
            );
        }
    }
}
