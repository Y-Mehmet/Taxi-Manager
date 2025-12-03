using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Oyundaki tüm kaydedilebilir verileri içeren ana veri yapısı.
/// </summary>
[System.Serializable]
public class SaveGameData
{
    // ResourceManager
    public int coinCount;

    // AbilityManager
    public int abilityUniversalPathfindingCount;
    public int abilityRemoveWagonsCount;
    public int abilityAddNewStopCount;
    public int abilityShuffleWagonColorsCount;

    // MetroManager
    public int levelIndex; // Current level being played
    public int maxOpenedLevel; // Highest level ever unlocked (never decreases)
    public List<int> levelStarsCount; // Stars earned per level (only highest count, never decreases)
    public int totalStarsEarned; // Total stars earned (sum of levelStarsCount, decreases when spent on jokers)


    // Diğer potansiyel veriler
    public int unlockedWagonCount;
    public int activeStopCount;
    public int passengerCapacityLevel;
    public int passengerSpawnRateLevel;
    public int offlineEarningsLevel;
    public float soundFxVolume;
    public float musicVolume;
    public bool isTutorialShown;
    public bool isPushNotificationEnabled; // Push notification settings

    // JokerSystem (Category-Based System)
    public Dictionary<int, int> jokerRemainingGames; // JokerType (int) -> Remaining Games
    public int activeTaxJoker; // Currently active tax joker (JokerType as int, 0 = None)
    public int activeRepairJoker; // Currently active repair joker (JokerType as int, 0 = None)


    
    /// <summary>
    /// Yeni bir oyun başladığında veya hiç kayıt dosyası bulunmadığında
    /// kullanılacak başlangıç verilerini oluşturur.
    /// </summary>
    public SaveGameData()
    {
        // Başlangıç değerleri
        coinCount = 500;
        levelIndex = 0;
        maxOpenedLevel = 0; // Start with only level 0 unlocked
        levelStarsCount = new List<int>();
        totalStarsEarned = 0; // No stars earned yet
        unlockedWagonCount = 1;
        activeStopCount = 1;
        passengerCapacityLevel = 1;
        passengerSpawnRateLevel = 1;
        offlineEarningsLevel = 1;
        soundFxVolume=0.5f;
        musicVolume=0.5f;
        isPushNotificationEnabled = true; // Default: enabled
        
        // Initialize joker data
        jokerRemainingGames = new Dictionary<int, int>();
        activeTaxJoker = 0; // JokerType.None
        activeRepairJoker = 0; // JokerType.None
    }
}