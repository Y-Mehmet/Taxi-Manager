using UnityEngine;

public class GameEconomy : MonoBehaviour
{
    public static GameEconomy Instance { get; private set; }

    [Header("Economy Settings")]
    public int successfulBoardingReward = 20;
    public int uberPenalty = 100;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public void AddCoins(int amount)
    {
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.AddCoins(amount);
            Debug.Log($"+{amount} coins! (via GameEconomy)");
        }
    }

    public void SpendCoins(int amount)
    {
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.SpendCoins(amount);
            Debug.Log($"-{amount} coins! (via GameEconomy)");
        }
    }

    public int GetCurrentCoins()
    {
        if (ResourceManager.Instance != null)
        {
            return ResourceManager.Instance.CurrentCoins;
        }
        return 0;
    }
}