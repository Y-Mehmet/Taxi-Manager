using UnityEngine;
using System.Collections.Generic;
using GridSystem.Data;

[CreateAssetMenu(fileName = "NewLevelSpawnData", menuName = "Spyke/Level Spawn Data")]
public class LevelSpawnSO : ScriptableObject
{
    [Header("Grid & Level Base")]
    public GridData gridData;

    [Header("Spawn Definitions")]
    public List<PassengerSpawnData> initialPassengerGroups;
    public List<UnderpassSpawnData> underpasses;
    public List<WagonSpawnData> wagons;
    public List<PassengerSpawnData> conveyorPassengers; // CORRECTED TYPE
}