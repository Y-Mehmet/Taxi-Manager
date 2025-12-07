using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Oyundaki tÃ¼m kaydedilebilir verileri iÃ§eren ana veri yapÄ±sÄ±.
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


    // DiÄŸer potansiyel veriler
    public int unlockedWagonCount;
    public int activeStopCount;
    public int passengerCapacityLevel;
    public int passengerSpawnRateLevel;
    public int offlineEarningsLevel;
    public float soundFxVolume;
    public float musicVolume;
    public bool isTutorialShown;
    
    // Ability Tutorial Completion Flags (per unlock level)
    public bool hasSeenAddStopTutorial; // Level 4
    public bool hasSeenUniversalPathfindingTutorial; // Level 8
    public bool hasSeenFlasherTutorial; // Level 16
    public bool hasSeenShuffleTutorial; // Level 32
    
    // Ability Unlock Levels (which level unlocks each ability)
    public int abilityAddNewStopUnlockLevel = 2;
    public int abilityUniversalPathfindingUnlockLevel = 3;
    public int abilityRemoveWagonsUnlockLevel = 4;
    public int abilityShuffleWagonColorsUnlockLevel = 5;
    
    // Current ability tutorial to show (set by SceneManager, read by AbilityTutorialManager)
    public string currentAbilityTutorial; // "AddStop", "UniversalPathfinding", "Flasher", "Shuffle"
    
    public bool isPushNotificationEnabled; // Push notification settings

    // JokerSystem (Category-Based System)
    public Dictionary<int, int> jokerRemainingGames; // JokerType (int) -> Remaining Games
    public int activeTaxJoker; // Currently active tax joker (JokerType as int, 0 = None)
    public int activeRepairJoker; // Currently active repair joker (JokerType as int, 0 = None)


    
    /// <summary>
    /// Yeni bir oyun baÅŸladÄ±ÄŸÄ±nda veya hiÃ§ kayÄ±t dosyasÄ± bulunmadÄ±ÄŸÄ±nda
    /// kullanÄ±lacak baÅŸlangÄ±Ã§ verilerini oluÅŸturur.
    /// </summary>
    public SaveGameData()
    {
        // BaÅŸlangÄ±Ã§ deÄŸerleri
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
        
        // Initialize ability tutorial flags
        hasSeenAddStopTutorial = false;
        hasSeenUniversalPathfindingTutorial = false;
        hasSeenFlasherTutorial = false;
        hasSeenShuffleTutorial = false;
        currentAbilityTutorial = "";
        
        isPushNotificationEnabled = true; // Default: enabled
        
        // Initialize joker data
        jokerRemainingGames = new Dictionary<int, int>();
        activeTaxJoker = 0; // JokerType.None
        activeRepairJoker = 0; // JokerType.None
    }
}
