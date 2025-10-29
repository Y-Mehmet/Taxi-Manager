using UnityEngine;
using System;
using System.Collections.Generic;

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
    public List<int> LevelStars { get; private set; }
    public  int boardingStartIndex {get; private set; }


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
        boardingStartIndex = 13;
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
        LevelStars = data.levelStarsCount;
        
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
        data.levelStarsCount = LevelStars;
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

    public void IncrementLevel()
    {
        CurrentLevel++;
        Debug.Log($"Level incremented to {CurrentLevel}");
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.SaveGame();
        }
    }
    public void SetLevelStarCount(int levelIndex, int stars)
    {
        // Ensure the list is large enough
        while (LevelStars.Count <= levelIndex)
        {
            LevelStars.Add(0); // Add levels with 0 stars if they don't exist yet
        }
        LevelStars[levelIndex] = stars;
        Debug.Log($"Level {levelIndex} star count set to {stars}");
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.SaveGame();
        }
    } 


}
