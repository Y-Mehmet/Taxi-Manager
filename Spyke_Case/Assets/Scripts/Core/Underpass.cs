
using UnityEngine;
using TMPro;
using System.Collections.Generic;

[System.Serializable]
public class Underpass
{
    public string Name; // Inspector'da tanımak için
    public Vector2Int StartCell;
    public Vector2Int EndCell;
    public TMP_Text QueueCounterText; // Kuyruk sayacını gösterecek text
    
    [HideInInspector]
    public Queue<PassengerGroup> PassengerQueue = new Queue<PassengerGroup>();
}
