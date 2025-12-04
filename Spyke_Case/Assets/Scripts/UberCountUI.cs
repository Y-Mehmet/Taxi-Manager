using UnityEngine;
using TMPro;

/// <summary>
/// UberManager'dan gelen verilere gÃ¶re kalan Uber hakkÄ±nÄ± UI'da gÃ¶sterir.
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class UberCountUI : MonoBehaviour
{
    private TextMeshProUGUI countText;
    private int maxCount;

    void Awake()
    {
        countText = GetComponent<TextMeshProUGUI>();
    }

    void Start()
    {
        if (UberManager.Instance == null)
        {
            Debug.LogError("UberCountUI requires an UberManager in the scene.");
            this.enabled = false;
            return;
        }

        // BaÅŸlangÄ±Ã§ deÄŸerlerini al ve olaylara abone ol
        maxCount = UberManager.Instance.maxUberCount;
        UberManager.OnUberCountChanged += UpdateText;

        // BaÅŸlangÄ±Ã§ metnini ayarla
        UpdateText(UberManager.Instance.UberCount);
    }

    void OnDestroy()
    {
        // Bellek sÄ±zÄ±ntÄ±sÄ±nÄ± Ã¶nle
        if (UberManager.Instance != null)
        {
            UberManager.OnUberCountChanged -= UpdateText;
        }
    }

    /// <summary>
    /// SayaÃ§ her deÄŸiÅŸtiÄŸinde metni gÃ¼nceller.
    /// </summary>
    private void UpdateText(int currentCount)
    {
        int remaining = maxCount - currentCount;
        countText.text = $"{remaining}";
    }
}
