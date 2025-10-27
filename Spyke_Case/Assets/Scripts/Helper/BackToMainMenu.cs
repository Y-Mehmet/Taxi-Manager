using UnityEngine;
using UnityEngine.UI;

 [RequireComponent(typeof(Button))]
public class BackToMainMenu : MonoBehaviour
{
  

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(LoadLevel);
    }

    /// <summary>
    /// Bu butona tıklandığında çağrılır.
    /// </summary>
    public void LoadLevel()
    {
        // Butonun interactable değilse işlem yapma (zaten tıklanamaz ama garanti olsun)
        if (!button.interactable) return;
        ResourceManager.Instance.SaveData(GameDataManager.Instance.GetSaveData());


      SceneManager.Instance.LoadMainMenu();
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
