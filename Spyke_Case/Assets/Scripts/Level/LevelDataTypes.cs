
using UnityEngine;
using System.Collections.Generic;

// Bu dosya, LevelSpawnSO iÃ§inde kullanÄ±lacak olan veri yapÄ±larÄ±nÄ± barÄ±ndÄ±rÄ±r.

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
    [Tooltip("Alt geÃ§it prefabÄ±nÄ±n yerleÅŸtirileceÄŸi grid hÃ¼cresi.")]
    public Vector2Int position;
    [Tooltip("Aktif yolcunun alt geÃ§ide gÃ¶re duracaÄŸÄ± yÃ¶n. Ã–rn: (-1, 0) -> sol tarafÄ±.")]
    public Vector2Int direction;
    [Tooltip("Bu alt geÃ§idin kullanacaÄŸÄ± yolcu renk sÄ±rasÄ±.")]
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

