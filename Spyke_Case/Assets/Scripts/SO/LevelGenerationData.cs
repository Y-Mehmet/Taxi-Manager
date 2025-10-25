
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PassengerGroupDefinition
{
    public Vector2Int position;
    public Vector2Int direction;
    public int capacity;
    public PassengerColor color;
}

[System.Serializable]
public class UnderpassDefinition
{
    public Vector2Int position;
    public Vector2Int direction;
    public List<PassengerColor> sequence;
}

[System.Serializable]
public class LevelDefinition
{
    public int levelNumber;
    public List<PassengerGroupDefinition> initialPassengerGroups;
    public List<UnderpassDefinition> underpasses;
    public List<PassengerColor> wagonTrain;
    public string gridDataPath; // Path to the GridData SO
}
