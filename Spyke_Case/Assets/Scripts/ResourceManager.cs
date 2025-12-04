using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Oyuncunun kaynaklarÄ±nÄ± ve temel ilerlemesini yÃ¶neten merkezi sistem.
/// </summary>
public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    // Olaylar
    public static event Action<int> OnCoinsChanged;

    // Genel Ã–zellikler
    public int CurrentCoins { get; private set; }
    [SerializeField]
    public int CurrentLevel;
    public int MaxOpenedLevel { get; private set; } // Highest level ever unlocked (never decreases)
    public List<int> LevelStars { get; private set; }
    public  int boardingStartIndex {get; private set; }

    public float musicVolume, soundFxVolume;

    
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
        // Veri yÃ¶neticisine baÄŸlan
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.OnDataLoaded += LoadData;
            // BaÅŸlangÄ±Ã§ta mevcut veriyi de yÃ¼kle
            LoadData(GameDataManager.Instance.GetSaveData());
        }
    }

    private void OnDestroy()
    {
        // Olay aboneliÄŸini kaldÄ±r
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.OnDataLoaded -= LoadData;
        }
    }

    /// <summary>
    /// KayÄ±tlÄ± veriden kaynaklarÄ± ve ilerlemeyi yÃ¼kler.
    /// </summary>
    private void LoadData(SaveGameData data)
    {
        if (data == null) return;
        
        CurrentCoins = data.coinCount;
        CurrentLevel = data.levelIndex;
        MaxOpenedLevel = data.maxOpenedLevel;
        LevelStars = data.levelStarsCount;
        soundFxVolume=data.soundFxVolume;
        musicVolume=data.musicVolume;
        
        OnCoinsChanged?.Invoke(CurrentCoins);
        
        Debug.Log($"[ResourceManager] Loaded: CurrentLevel={CurrentLevel}, MaxOpenedLevel={MaxOpenedLevel}");
    }

    /// <summary>
    /// Mevcut durumu kaydetmek iÃ§in veri nesnesini gÃ¼nceller.
    /// </summary>
    public void SaveData(SaveGameData data)
    {
        if (data == null) return;
        
        data.coinCount = CurrentCoins;
        data.levelIndex = CurrentLevel;
        data.maxOpenedLevel = MaxOpenedLevel;
        data.levelStarsCount = LevelStars;
        data.soundFxVolume=soundFxVolume;
        data.musicVolume=musicVolume;
    }

    // --- Coin MetodlarÄ± --- //

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
//             Debug.LogWarning($"<color=red>{amount} coins spent.</color> Remaining coins: {CurrentCoins}");
            return true;
        }
        else
        {
//             Debug.LogWarning($"<color=red>Not enough coins to spend {amount}.</color> Current coins: {CurrentCoins}");
            return false;
        }
    }

    /// <summary>
    /// Increments the current level and updates MaxOpenedLevel if needed
    /// </summary>
    public void IncrementLevel()
    {
        CurrentLevel++;
        
        // Always update MaxOpenedLevel to the highest value
        if (CurrentLevel > MaxOpenedLevel)
        {
            MaxOpenedLevel = CurrentLevel;
            Debug.Log($"[ResourceManager] New max opened level: {MaxOpenedLevel}");
        }
        
        Debug.Log($"[ResourceManager] Level incremented to {CurrentLevel}");
        
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.SaveGame();
        }
    }

    public void SetLevelStarCount(int levelIndex, int stars)
    {
        if (LevelStars == null)
        {
            LevelStars = new List<int>();
        }

        // Listeyi geniÅŸlet
        while (LevelStars.Count <= levelIndex)
        {
            LevelStars.Add(0);
        }

        // Sadece daha yÃ¼ksek yÄ±ldÄ±z sayÄ±sÄ± kaydedilir (asla azalmaz)
        int previousStars = LevelStars[levelIndex];
        
        if (stars > previousStars)
        {
            int starDifference = stars - previousStars;
            LevelStars[levelIndex] = stars;
            Debug.Log($"[ResourceManager] Level {levelIndex} stars improved: {previousStars} -> {stars} (+{starDifference})");
            
            // totalStarsEarned'e sadece FARKI ekle (tÃ¼m toplamÄ± yeniden hesaplama!)
            if (GameDataManager.Instance != null && GameDataManager.Instance.GetSaveData() != null)
            {
                var data = GameDataManager.Instance.GetSaveData();
                data.totalStarsEarned += starDifference;
                Debug.Log($"[ResourceManager] totalStarsEarned updated: +{starDifference} = {data.totalStarsEarned}");
                
                // Event'i tetikle
                if (JokerSystem.Instance != null)
                {
                    JokerSystem.Instance.NotifyStarsChanged(data.totalStarsEarned);
                }
                
                GameDataManager.Instance.SaveGame();
            }
        }
        else if (stars == previousStars)
        {
            Debug.Log($"[ResourceManager] Level {levelIndex} completed with same stars: {stars}");
        }
        else
        {
            Debug.Log($"[ResourceManager] Level {levelIndex} completed with lower stars ({stars}), keeping previous best: {previousStars}");
        }
    }
}
