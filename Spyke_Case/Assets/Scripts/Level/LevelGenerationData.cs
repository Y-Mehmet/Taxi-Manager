using System.Collections.Generic;
using UnityEngine;

// This file contains the data structures for defining a generated level before it becomes a ScriptableObject.

[System.Serializable]
public class LevelDefinition
{
    public int levelNumber;
    public string levelName;

    // While we use one GridData, it's good practice to have a reference in the definition.
    // We will assign this from the editor later.
    // public GridData gridData;

    [Header("Spawn Definitions")]
    public List<PassengerSpawnData> initialPassengerGroups;
    public List<UnderpassSpawnData> underpasses;
    public List<WagonSpawnData> wagons;
    public List<PassengerSpawnData> conveyorPassengers; // ADDED

    public LevelDefinition()
    {
        initialPassengerGroups = new List<PassengerSpawnData>();
        underpasses = new List<UnderpassSpawnData>();
        wagons = new List<WagonSpawnData>();
        conveyorPassengers = new List<PassengerSpawnData>(); // ADDED
    }

    public LevelDefinition(int levelNum)
    {
        this.levelNumber = levelNum;
        this.levelName = $"Level_{levelNum}";
        initialPassengerGroups = new List<PassengerSpawnData>();
        underpasses = new List<UnderpassSpawnData>();
        wagons = new List<WagonSpawnData>();
        conveyorPassengers = new List<PassengerSpawnData>(); // ADDED
    }
}