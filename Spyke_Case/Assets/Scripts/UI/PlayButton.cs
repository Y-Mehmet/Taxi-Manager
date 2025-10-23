using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// "Play" butonuna eklenmek üzere tasarlanmış script.
/// Tıklandığında SceneManager aracılığıyla mevcut seviyeyi yükler.
/// </summary>
[RequireComponent(typeof(Button))]
public class PlayButton : MonoBehaviour
{
    private void Awake()
    {
        // Butonun OnClick olayına LoadCurrentLevel metodunu programatik olarak ekle.
        GetComponent<Button>().onClick.AddListener(LoadCurrentLevel);
    }

    /// <summary>
    /// SceneManager'ı çağırarak mevcut seviyeyi yükler.
    /// </summary>
    public void LoadCurrentLevel()
    {
        if (SceneManager.Instance != null)
        {
            SceneManager.Instance.LoadCurrentLevel();
        }
        else
        {
            Debug.LogError("SceneManager not found in the scene!");
        }
    }

    private void OnDestroy()
    {
        // Bellek sızıntısını önlemek için listener'ı kaldır
        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveListener(LoadCurrentLevel);
        }
    }
}
