
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Unity'nin Color sınıfını serileştirmek için kullanılan basit bir yapı.
/// JsonUtility Color'ı doğrudan serileştiremez.
/// </summary>
[System.Serializable]
public struct SerializableColor
{
    public float r, g, b, a;

    public SerializableColor(Color color)
    {
        r = color.r;
        g = color.g;
        b = color.b;
        a = color.a;
    }

    public Color ToUnityColor()
    {
        return new Color(r, g, b, a);
    }
}

/// <summary>
/// Oyundaki tüm kaydedilebilir verileri içeren ana veri yapısı.
/// </summary>
[System.Serializable]
public class SaveGameData
{
    // ResourceManager
    public int coinCount;

    // AbilityManager
    public List<string> abilityInventory;

    // MetroManager
    public int levelIndex;
    public List<SerializableColor> wagonColors;

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

        abilityInventory = new List<string>();
        wagonColors = new List<SerializableColor>();
    }
}
