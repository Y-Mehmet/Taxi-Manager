using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class LevelUpPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI earningsText;
    [SerializeField] private List<Image> starImages;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button retryButton;

    [Header("Star Sprites")]
    [SerializeField] private Sprite brightStar;
    [SerializeField] private Sprite greyStar;

    private void Awake()
    {
        continueButton.onClick.AddListener(OnContinueButtonClicked);
        retryButton.onClick.AddListener(OnRetryButtonClicked);
    }

    public void Show(int stars, int earnings)
    {
        if (earningsText != null)
        {
            earningsText.text = $"EARNED: ${earnings}";
        }

        for (int i = 0; i < starImages.Count; i++)
        {
            if (starImages[i] != null)
            {
                starImages[i].sprite = (i < stars) ? brightStar : greyStar;
            }
        }

        if (retryButton != null)
        {
            retryButton.gameObject.SetActive(stars < 3);
        }
    }

    private void OnContinueButtonClicked()
    {
        // ResourceManager already incremented the level in GameManager.WinLevel
        // So we just load the current level index.
        // Note: This assumes scene names are "Level1", "Level2", etc.
        // and build settings are aligned.
        int levelToLoad = ResourceManager.Instance.CurrentLevel;
        UnityEngine.SceneManagement.SceneManager.LoadScene("Level" + levelToLoad);
        Debug.Log($"Loading next level: {levelToLoad}");
    }

    private void OnRetryButtonClicked()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        Debug.Log("Retrying current level...");
    }
}