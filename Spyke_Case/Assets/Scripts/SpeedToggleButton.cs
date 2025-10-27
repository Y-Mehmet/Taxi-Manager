
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class SpeedToggleButton : MonoBehaviour
{
    private Button speedButton;
    private TMP_Text buttonText;

    void Awake()
    {
        speedButton = GetComponent<Button>();
        buttonText = GetComponentInChildren<TMP_Text>();

        if (buttonText == null)
        {
            Debug.LogError("[SpeedToggleButton] Button'un altında bir TMP_Text componenti bulunamadı!");
            return;
        }
    }

    void OnEnable()
    {
        MetroManager.OnSpeedMultiplierChanged += UpdateButtonText;
        speedButton.onClick.AddListener(OnButtonClicked);
    }

    void OnDisable()
    {
        MetroManager.OnSpeedMultiplierChanged -= UpdateButtonText;
        speedButton.onClick.RemoveListener(OnButtonClicked);
    }

    private void OnButtonClicked()
    {
        if (MetroManager.Instance != null)
        {
            MetroManager.Instance.ToggleSpeed();
        }
        else
        {
            Debug.LogError("[SpeedToggleButton] MetroManager.Instance bulunamadı!");
        }
    }

    private void UpdateButtonText(string newText)
    {
        if (buttonText != null)
        {
            buttonText.text = newText;
        }
    }
}
