using UnityEngine;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }
    
    private bool isPaused = false;
    public bool IsPaused => isPaused;
    
    // Pause panel reference (will be fetched from ObjectPool)
    private GameObject pausePanelInstance;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void TogglePause()
    {
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }
    
    public void PauseGame()
    {
        if (isPaused) return;
        
        isPaused = true;
        
        // Stop time for physics and animations
        Time.timeScale = 0f;
        
        // Disable input
        if (InputManager.Instance != null)
        {
            InputManager.Instance.DisableInput();
        }
        
        // Stop MetroWagon movements
        if (MetroManager.Instance != null)
        {
            MetroManager.Instance.PauseAllWagons();
        }
        
        // Stop Conveyor
        if (ConveyorBelt.Instance != null)
        {
            ConveyorBelt.Instance.Pause();
        }
        
        // Show pause panel from ObjectPool
        ShowPausePanel();
        
        Debug.Log("[PauseManager] Game Paused");
    }
    
    public void ResumeGame()
    {
        if (!isPaused) return;
        
        isPaused = false;
        
        // Resume time
        Time.timeScale = 1f;
        
        // Enable input
        if (InputManager.Instance != null)
        {
            InputManager.Instance.EnableInput();
        }
        
        // Resume MetroWagon movements
        if (MetroManager.Instance != null)
        {
            MetroManager.Instance.ResumeAllWagons();
        }
        
        // Resume Conveyor
        if (ConveyorBelt.Instance != null)
        {
            ConveyorBelt.Instance.Resume();
        }
        
        // Hide pause panel
        HidePausePanel();
        
        Debug.Log("[PauseManager] Game Resumed");
    }
    
    private void ShowPausePanel()
    {
        // TODO: Get from ObjectPool when implemented
        // For now, use PanelManager if available
        if (PanelManager.Instance != null)
        {
            PanelManager.Instance.ShowPanel(PanelID.PausePanel);
        }
    }
    
    private void HidePausePanel()
    {
        // TODO: Return to ObjectPool when implemented
        // For now, use PanelManager if available
        if (PanelManager.Instance != null)
        {
            PanelManager.Instance.HidePanelWithPanelID(PanelID.PausePanel);
        }
    }

}
