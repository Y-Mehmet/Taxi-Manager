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

    /// <summary>
    /// Coin ekler ve animasyon gösterir (dünya pozisyonundan).
    /// </summary>
    public void AddCoins(int amount, Vector3? worldPosition = null)
    {
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.AddCoins(amount);
            Debug.Log($"+{amount} coins! (via GameEconomy)");

            // Animasyon göster (dünya pozisyonundan)
            if (CoinAnimationManager.Instance != null && worldPosition.HasValue)
            {
                CoinAnimationManager.Instance.ShowCoinGain(amount, worldPosition.Value);
            }
        }
    }

    /// <summary>
    /// Coin harcatır ve animasyon gösterir (UI pozisyonundan).
    /// </summary>
    public void SpendCoins(int amount, Vector3? uiPosition = null)
    {
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.SpendCoins(amount);
            Debug.Log($"-{amount} coins! (via GameEconomy)");

            // Animasyon göster (UI pozisyonundan)
            if (CoinAnimationManager.Instance != null && uiPosition.HasValue)
            {
                CoinAnimationManager.Instance.ShowCoinSpend(amount, uiPosition.Value);
            }
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