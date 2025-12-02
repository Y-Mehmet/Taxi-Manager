using UnityEngine;
using System;

/// <summary>
/// Yeni ekonomi sistemi: TempResource (level içi) ve MainResource (kalıcı) yönetimi
/// </summary>
public class GameEconomy : MonoBehaviour
{
    public static GameEconomy Instance { get; private set; }

    // Events
    public static event Action<int> OnTempCoinsChanged;
    public static event Action<int> OnMainCoinsChanged;

    // Temp Resource - Level içinde kazanılan para (görünmez, sadece level sonunda hesaplanır)
    private int tempCoins = 0;

    // Main Resource - Gerçek para (ResourceManager'dan alınır)
    public int MainCoins => ResourceManager.Instance != null ? ResourceManager.Instance.CurrentCoins : 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // Level başladığında temp coins sıfırlanır
        ResetTempCoins();
    }

    /// <summary>
    /// Level başında temp coins sıfırla
    /// </summary>
    public void ResetTempCoins()
    {
        tempCoins = 0;
        OnTempCoinsChanged?.Invoke(tempCoins);
        Debug.Log("[GameEconomy] Temp coins reset to 0");
    }

    /// <summary>
    /// Temp Resource'a coin ekle (level içi kazanç)
    /// </summary>
    public void AddTempCoins(int amount)
    {
        if (amount <= 0) return;

        tempCoins += amount;
        OnTempCoinsChanged?.Invoke(tempCoins);
        Debug.Log($"[GameEconomy] +{amount} temp coins. Total temp: {tempCoins}");
    }

    /// <summary>
    /// Temp Resource'tan coin çıkar (cezalar için)
    /// </summary>
    public void DeductTempCoins(int amount)
    {
        if (amount <= 0) return;

        tempCoins -= amount;
        OnTempCoinsChanged?.Invoke(tempCoins);
        Debug.Log($"[GameEconomy] -{amount} temp coins. Total temp: {tempCoins}");
    }

    /// <summary>
    /// Main Resource'tan coin harca (ability kullanımı için)
    /// </summary>
    public bool SpendMainCoins(int amount)
    {
        if (amount <= 0) return false;

        if (ResourceManager.Instance != null)
        {
            bool success = ResourceManager.Instance.SpendCoins(amount);
            if (success)
            {
                OnMainCoinsChanged?.Invoke(MainCoins);
                Debug.Log($"[GameEconomy] Spent {amount} main coins. Remaining: {MainCoins}");
            }
            return success;
        }

        return false;
    }

    /// <summary>
    /// Main Resource'a coin ekle
    /// </summary>
    public void AddMainCoins(int amount)
    {
        if (amount <= 0) return;

        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.AddCoins(amount);
            OnMainCoinsChanged?.Invoke(MainCoins);
            Debug.Log($"[GameEconomy] Added {amount} main coins. Total: {MainCoins}");
        }
    }

    /// <summary>
    /// Level sonu: Temp coins'i fatura ile birlikte Main Resource'a aktar
    /// </summary>
    public void TransferTempToMain(LevelInvoiceData invoice)
    {
        if (invoice == null)
        {
            Debug.LogError("[GameEconomy] Invoice is null!");
            return;
        }

        int netEarnings = invoice.CalculateNetEarnings();
        
        if (netEarnings > 0)
        {
            AddMainCoins(netEarnings);
        }
        else if (netEarnings < 0)
        {
            // Negatif kazanç durumunda main resource'tan düşebilir (opsiyonel)
            Debug.LogWarning($"[GameEconomy] Net earnings are negative: {netEarnings}. Not deducting from main resource.");
        }

        Debug.Log($"[GameEconomy] Level completed. Net earnings: {netEarnings}. New main balance: {MainCoins}");
        
        // Temp coins sıfırla
        ResetTempCoins();
    }

    public int GetTempCoins() => tempCoins;
    public int GetMainCoins() => MainCoins;

    // Eski sistemle uyumluluk için
    public int GetCurrentCoins() => MainCoins;
    
    public void SpendCoins(int cost, Vector3 worldPosition)
    {
        SpendMainCoins(cost);
        // Animasyon için CoinAnimationManager kullanılabilir
        if (CoinAnimationManager.Instance != null)
        {
            CoinAnimationManager.Instance.ShowSpendingFeedback(cost, worldPosition);
        }
    }
}
