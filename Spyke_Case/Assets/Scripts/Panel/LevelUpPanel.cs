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
        // As requested, reload the current scene. A manager script should handle loading the correct level data upon scene start.
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        Debug.Log("Continue button clicked. Reloading scene to start next level.");
    }

    private void OnRetryButtonClicked()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        Debug.Log("Retrying current level...");
    }
}