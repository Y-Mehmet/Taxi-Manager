using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Bir stop panel'inin UI elementlerini tutan data class.
/// Her stop için: image, text ve durum göstergesi (ışık)
/// </summary>
[System.Serializable]
public class StopPanelData
{
    [Header("Panel")]
    public GameObject panelObject; // Ana panel GameObject
    
    [Header("UI Elements")]
    public Image stopImage; // Stop ikonu
    public TextMeshProUGUI stopText; // Stop metni (örn: "Stop 1")
    
    [Header("State Indicators")]
    public GameObject inactiveIndicator; // Kapalı durum göstergesi (ilk child)
    public GameObject activeIndicator; // Açık durum göstergesi (ikinci child)
    
    /// <summary>
    /// Panel'i aktif/pasif yapar
    /// </summary>
    public void SetActive(bool active)
    {
        if (panelObject != null)
            panelObject.SetActive(active);
    }
    
    /// <summary>
    /// Stop'un durumunu gösterir (aktif/inaktif)
    /// </summary>
    public void SetStopActive(bool active)
    {
        if (inactiveIndicator != null)
            inactiveIndicator.SetActive(!active);
            
        if (activeIndicator != null)
            activeIndicator.SetActive(active);
    }
    
    /// <summary>
    /// Stop metnini ayarlar
    /// </summary>
    public void SetText(string text)
    {
        if (stopText != null)
            stopText.text = text;
    }
}
