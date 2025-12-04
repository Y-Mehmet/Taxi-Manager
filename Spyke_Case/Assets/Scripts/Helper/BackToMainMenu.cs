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
    /// Bu butona tÄ±klandÄ±ÄŸÄ±nda Ã§aÄŸrÄ±lÄ±r.
    /// </summary>
    public void LoadLevel()
    {
        // Butonun interactable deÄŸilse iÅŸlem yapma (zaten tÄ±klanamaz ama garanti olsun)
        if (!button.interactable) return;
        ResourceManager.Instance.SaveData(GameDataManager.Instance.GetSaveData());


      SceneManager.Instance.LoadMainMenu();
    }

    private void OnDestroy()
    {
        // Bellek sÄ±zÄ±ntÄ±sÄ±nÄ± Ã¶nlemek iÃ§in listener'Ä± kaldÄ±r
        if (button != null)
        {
            button.onClick.RemoveListener(LoadLevel);
        }
    }
}
