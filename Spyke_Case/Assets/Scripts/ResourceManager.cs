using UnityEngine;
using System;

/// <summary>
/// Oyuncunun kaynaklarını ve temel ilerlemesini yöneten merkezi sistem.
/// </summary>
public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    // Olaylar
    public static event Action<int> OnCoinsChanged;

    // Genel Özellikler
    public int CurrentCoins { get; private set; }
    public int CurrentLevel { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
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
    /// Kayıtlı veriden kaynakları ve ilerlemeyi yükler.
    /// </summary>
    private void LoadData(SaveGameData data)
    {
        if (data == null) return;
        
        CurrentCoins = data.coinCount;
        CurrentLevel = data.levelIndex; // Hata düzeltildi: levelIndex kullanılıyor
        
        OnCoinsChanged?.Invoke(CurrentCoins);
    }

    /// <summary>
    /// Mevcut durumu kaydetmek için veri nesnesini günceller.
    /// </summary>
    public void SaveData(SaveGameData data)
    {
        if (data == null) return;
        
        data.coinCount = CurrentCoins;
        data.levelIndex = CurrentLevel; // Hata düzeltildi: levelIndex kullanılıyor
    }

    // --- Coin Metodları --- //

    public void AddCoins(int amount)
    {
        if (amount <= 0) return;

        CurrentCoins += amount;
        OnCoinsChanged?.Invoke(CurrentCoins);
        Debug.Log($"{amount} coins added. Total coins: {CurrentCoins}");
    }

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
