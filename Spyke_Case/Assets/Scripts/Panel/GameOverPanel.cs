using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameOverPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button retryButton;

    private void Awake()
    {
        if (retryButton != null)
        {
            retryButton.onClick.AddListener(OnRetryButtonClicked);
        }
    }

    private void OnRetryButtonClicked()
    {
        // Reload the current level
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        Debug.Log("Retrying current level from Game Over panel...");
    }
}
