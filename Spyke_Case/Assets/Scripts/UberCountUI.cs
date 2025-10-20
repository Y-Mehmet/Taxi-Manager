using UnityEngine;
using TMPro;

/// <summary>
/// UberManager'dan gelen verilere göre kalan Uber hakkını UI'da gösterir.
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

        // Başlangıç değerlerini al ve olaylara abone ol
        maxCount = UberManager.Instance.maxUberCount;
        UberManager.OnUberCountChanged += UpdateText;

        // Başlangıç metnini ayarla
        UpdateText(UberManager.Instance.UberCount);
    }

    void OnDestroy()
    {
        // Bellek sızıntısını önle
        if (UberManager.Instance != null)
        {
            UberManager.OnUberCountChanged -= UpdateText;
        }
    }

    /// <summary>
    /// Sayaç her değiştiğinde metni günceller.
    /// </summary>
    private void UpdateText(int currentCount)
    {
        int remaining = maxCount - currentCount;
        countText.text = $"{remaining}";
    }
}
