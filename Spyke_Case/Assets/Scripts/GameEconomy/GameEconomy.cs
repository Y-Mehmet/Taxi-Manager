using UnityEngine;
using System;

/// <summary>
/// Yeni ekonomi sistemi: TempResource (level iÃ§i) ve MainResource (kalÄ±cÄ±) yÃ¶netimi
/// </summary>
public class GameEconomy : MonoBehaviour
{
    public static GameEconomy Instance { get; private set; }

    // Events
    public static event Action<int> OnTempCoinsChanged;
    public static event Action<int> OnMainCoinsChanged;

    // Temp Resource - Level iÃ§inde kazanÄ±lan para (gÃ¶rÃ¼nmez, sadece level sonunda hesaplanÄ±r)
    private int tempCoins = 0;

    // Main Resource - GerÃ§ek para (ResourceManager'dan alÄ±nÄ±r)
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
        // Level baÅŸladÄ±ÄŸÄ±nda temp coins sÄ±fÄ±rlanÄ±r
        ResetTempCoins();
    }

    /// <summary>
    /// Level baÅŸÄ±nda temp coins sÄ±fÄ±rla
    /// </summary>
    public void ResetTempCoins()
    {
        tempCoins = 0;
        OnTempCoinsChanged?.Invoke(tempCoins);
        Debug.Log("[GameEconomy] Temp coins reset to 0");
    }

    /// <summary>
    /// Temp Resource'a coin ekle (level iÃ§i kazanÃ§)
    /// </summary>
    public void AddTempCoins(int amount)
    {
        if (amount <= 0) return;

        tempCoins += amount;
        OnTempCoinsChanged?.Invoke(tempCoins);
        Debug.Log($"[GameEconomy] +{amount} temp coins. Total temp: {tempCoins}");
    }

    /// <summary>
    /// Temp Resource'tan coin Ã§Ä±kar (cezalar iÃ§in)
    /// </summary>
    public void DeductTempCoins(int amount)
    {
        if (amount <= 0) return;

        tempCoins -= amount;
        OnTempCoinsChanged?.Invoke(tempCoins);
        Debug.Log($"[GameEconomy] -{amount} temp coins. Total temp: {tempCoins}");
    }

    /// <summary>
    /// Main Resource'tan coin harca (ability kullanÄ±mÄ± iÃ§in)
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
    /// Negatif kazanÃ§ durumunda main resource'tan dÃ¼ÅŸer ama asla 0'Ä±n altÄ±na inmez
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
            // Pozitif kazanÃ§: Main resource'a ekle
            AddMainCoins(netEarnings);
            Debug.Log($"[GameEconomy] Added {netEarnings} coins to main resource. New balance: {MainCoins}");
        }
        else if (netEarnings < 0)
        {
            // Negatif kazanÃ§: Main resource'tan dÃ¼ÅŸ ama 0'Ä±n altÄ±na inme
            if (ResourceManager.Instance != null)
            {
                int currentCoins = ResourceManager.Instance.CurrentCoins;
                int deductAmount = Mathf.Abs(netEarnings); // Pozitif deÄŸer yap
                
                if (currentCoins >= deductAmount)
                {
                    // Yeterli para var, tam olarak dÃ¼ÅŸ
                    ResourceManager.Instance.SpendCoins(deductAmount);
//                     Debug.LogWarning($"[GameEconomy] Deducted {deductAmount} coins from main resource. New balance: {MainCoins}");
                }
                else if (currentCoins > 0)
                {
                    // Yeterli para yok, sadece mevcut parayÄ± sÄ±fÄ±rla
                    ResourceManager.Instance.SpendCoins(currentCoins);
//                     Debug.LogWarning($"[GameEconomy] Insufficient coins! Deducted only {currentCoins} coins (wanted {deductAmount}). Balance set to 0.");
                }
                else
                {
                    // Zaten 0 para var
//                     Debug.LogWarning($"[GameEconomy] Net earnings are {netEarnings} but main resource is already 0. No deduction.");
                }
                
                OnMainCoinsChanged?.Invoke(MainCoins);
            }
        }
        else
        {
            // Net kazanÃ§ 0
            Debug.Log("[GameEconomy] Net earnings are 0. No change to main resource.");
        }

        Debug.Log($"[GameEconomy] Level completed. Net earnings: {netEarnings}. Final main balance: {MainCoins}");
        
        // Temp coins sÄ±fÄ±rla
        ResetTempCoins();
    }

    public int GetTempCoins() => tempCoins;
    public int GetMainCoins() => MainCoins;

    // Eski sistemle uyumluluk iÃ§in
    public int GetCurrentCoins() => MainCoins;
    
    public void SpendCoins(int cost, Vector3 worldPosition)
    {
        SpendMainCoins(cost);
        // Animasyon iÃ§in CoinAnimationManager kullanÄ±labilir
        if (CoinAnimationManager.Instance != null)
        {
            CoinAnimationManager.Instance.ShowSpendingFeedback(cost, worldPosition);
        }
    }
}
