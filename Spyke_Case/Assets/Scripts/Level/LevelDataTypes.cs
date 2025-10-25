
using UnityEngine;
using System.Collections.Generic;

// Bu dosya, LevelSpawnSO içinde kullanılacak olan veri yapılarını barındırır.

[System.Serializable]
public struct PassengerSpawnData
{
    public Vector2Int position;
    public Vector2Int direction;
    public HyperCasualColor color;
}

[System.Serializable]
public struct UnderpassSpawnData
{
    [Tooltip("Alt geçit prefabının yerleştirileceği grid hücresi.")]
    public Vector2Int position;
    [Tooltip("Aktif yolcunun alt geçide göre duracağı yön. Örn: (-1, 0) -> sol tarafı.")]
    public Vector2Int direction;
    [Tooltip("Bu alt geçidin kullanacağı yolcu renk sırası.")]
    public List<HyperCasualColor> passengerSequence;
}

[System.Serializable]
public struct WagonSpawnData
{
    public HyperCasualColor color;
    public int capacity;

    public WagonSpawnData(HyperCasualColor color, int capacity = 4)
    {
        this.color = color;
        this.capacity = capacity;
    }
}
