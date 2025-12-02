using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Seviye seçim ekranındaki her bir butona eklenmek üzere tasarlanmış script.
/// Butonun hiyerarşideki sırasını (sibling index) alarak ilgili seviyeyi yükler.
/// </summary>
[RequireComponent(typeof(Button))]
public class LevelButton : MonoBehaviour
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(LoadLevel);
    }

    /// <summary>
    /// Bu butona tıklandığında çağrılır.
    /// GetSiblingIndex kullanarak hangi level'ı yükleyeceğini belirler.
    /// </summary>
    public void LoadLevel()
    {
        // Butonun interactable değilse işlem yapma (zaten tıklanamaz ama garanti olsun)
        if (!button.interactable) return;

        // Hiyerarşideki sırayı al (bu bizim level index'imiz olacak)
        int levelIndex = transform.GetSiblingIndex();

        Debug.Log($"[LevelButton] Loading level {levelIndex} from button sibling index");

        // ResourceManager'da CurrentLevel'ı geçici olarak değiştir
        // Bu sayede GameDataManager'dan yüklenen CurrentLevel bypass edilir
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.CurrentLevel = levelIndex;
            Debug.Log($"[LevelButton] Set ResourceManager.CurrentLevel to {levelIndex}");
        }

        // SceneManager üzerinden ilgili seviyeyi yükle
        if (SceneManager.Instance != null)
        {
            SceneManager.Instance.LoadSpecificLevel(levelIndex);
        }
        else
        {
            Debug.LogError("SceneManager not found in the scene!");
        }
    }

    private void OnDestroy()
    {
        // Bellek sızıntısını önlemek için listener'ı kaldır
        if (button != null)
        {
            button.onClick.RemoveListener(LoadLevel);
        }
    }
}
