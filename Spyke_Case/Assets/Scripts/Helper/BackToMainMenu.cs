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
        
        // Refund booster cost if used (player is leaving mid-game)
        if (GameManager.Instance != null && GameManager.Instance.CurrentInvoice != null)
        {
            int boosterCost = GameManager.Instance.CurrentInvoice.boosterCost;
            if (boosterCost > 0 && GameEconomy.Instance != null)
            {
                GameEconomy.Instance.AddMainCoins(boosterCost);
                /* Debug.Log($"[BackToMainMenu] Refunded {boosterCost} coins for booster usage (Back to Main Menu)."); */
            }
        }
        
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
