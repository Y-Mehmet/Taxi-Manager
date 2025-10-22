using UnityEngine;
using System;

/// <summary>
/// Oyuncunun coin gibi kaynaklarını yöneten merkezi sistem.
/// </summary>
public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    // Coin miktarı değiştiğinde tetiklenir. UI güncellemek için kullanılır.
    public static event Action<int> OnCoinsChanged;

    public int CurrentCoins { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Sahneler arası geçişte korunması için
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Veri yöneticisine bağlan
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.OnDataLoaded += LoadData;
            // Başlangıçta mevcut veriyi de yükle
            LoadData(GameDataManager.Instance.GetSaveData());
        }
    }

    private void OnDestroy()
    {
        // Olay aboneliğini kaldır
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.OnDataLoaded -= LoadData;
        }
    }

    /// <summary>
    /// Kayıtlı veriden coin miktarını yükler.
    /// </summary>
    private void LoadData(SaveGameData data)
    {
        if (data == null) return;
        CurrentCoins = data.coinCount;
        OnCoinsChanged?.Invoke(CurrentCoins);
    }

    /// <summary>
    /// Mevcut coin miktarını kaydetmek için veri nesnesini günceller.
    /// </summary>
    public void SaveData(SaveGameData data)
    {
        if (data == null) return;
        data.coinCount = CurrentCoins;
    }

    /// <summary>
    /// Oyuncuya belirtilen miktarda coin ekler.
    /// </summary>
    /// <param name="amount">Eklenecek coin miktarı.</param>
    public void AddCoins(int amount)
    {
        if (amount <= 0) return;

        CurrentCoins += amount;
        OnCoinsChanged?.Invoke(CurrentCoins);
        Debug.Log($"{amount} coins added. Total coins: {CurrentCoins}");
    }

    /// <summary>
    /// Oyuncudan belirtilen miktarda coin harcamayı dener.
    /// </summary>
    /// <param name="amount">Harcanacak coin miktarı.</param>
    /// <returns>Harcama başarılıysa true, yeterli coin yoksa false döner.</returns>
    public bool SpendCoins(int amount)
    {
        if (amount <= 0) return false;

        if (CurrentCoins >= amount)
        {
            CurrentCoins -= amount;
            OnCoinsChanged?.Invoke(CurrentCoins);
            Debug.Log($"{amount} coins spent. Remaining coins: {CurrentCoins}");
            return true;
        }
        else
        {
            Debug.LogWarning($"Not enough coins to spend {amount}. Current coins: {CurrentCoins}");
            return false;
        }
    }
}
