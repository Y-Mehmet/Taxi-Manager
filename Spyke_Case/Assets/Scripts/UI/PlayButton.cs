using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// "Play" butonuna eklenmek üzere tasarlanmış script.
/// Tıklandığında SceneManager aracılığıyla EN YÜKSEK AÇILAN SEVİYEYİ yükler.
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
    /// SceneManager'ı çağırarak EN YÜKSEK AÇILAN SEVİYEYİ yükler.
    /// Play button her zaman oyuncunun ulaştığı en yüksek seviyeyi oynatır.
    /// </summary>
    public void LoadCurrentLevel()
    {
        if (SceneManager.Instance != null)
        {
            // Play button her zaman en yüksek açılan seviyeyi oynatır
            if (ResourceManager.Instance != null)
            {
                int maxLevel = ResourceManager.Instance.MaxOpenedLevel;
                ResourceManager.Instance.CurrentLevel = maxLevel;
                Debug.Log($"<color=cyan>[PlayButton] Loading HIGHEST unlocked level: {maxLevel}</color>");
            }
            
            SceneManager.Instance.LoadLevelSceene();
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
