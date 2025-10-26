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
    public int levelIndex;
    public int levelStartsCount;


    // Diğer potansiyel veriler
    public int unlockedWagonCount;
    public int activeStopCount;
    public int passengerCapacityLevel;
    public int passengerSpawnRateLevel;
    public int offlineEarningsLevel;
    
    /// <summary>
    /// Yeni bir oyun başladığında veya hiç kayıt dosyası bulunmadığında
    /// kullanılacak başlangıç verilerini oluşturur.
    /// </summary>
    public SaveGameData()
    {
        // Başlangıç değerleri
        coinCount = 100;
        levelIndex = 0;
        unlockedWagonCount = 1;
        activeStopCount = 1;
        passengerCapacityLevel = 1;
        passengerSpawnRateLevel = 1;
        offlineEarningsLevel = 1;
    }
}