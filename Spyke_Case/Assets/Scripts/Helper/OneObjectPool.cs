using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

public class OneObjectPool : MonoBehaviour
{
    public static OneObjectPool Instance { get; private set; }
    public List<GameObject> gameObjectsPrefebs = new List<GameObject>();
    public Dictionary<string, GameObject> objectPool = new Dictionary<string, GameObject>();
    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }
    private void Start()
    {
        if(objectPool.Count == 0)
        {
            foreach (var item in gameObjectsPrefebs)
            {
                GameObject obj = Instantiate(item);
                obj.SetActive(false);
                objectPool.Add(item.name, obj);
            }
        }
    }
    public GameObject GetObjectWhitName(ObjectName name)
    {
        if(objectPool.Count == 0)
        {
           // Debug.LogWarning("Object pool is empty. Please add objects to the pool before requesting.");
            return null;
        }
        foreach (var item in objectPool)
        {
            if (item.Key == name.ToString())
            {
                if (!item.Value.activeInHierarchy)
                {
                    item.Value.SetActive(true);
                    return item.Value;
                }
                else
                {
                   // Debug.LogWarning(" Object with name  " + name + " is already active. Please deactivate it before requesting again.");
                    return null;
                }
            }
            

        }
        return null;
    }
    public ObjectName StringToCastOnjectName(string objName)
    {
        switch(objName)
        {
            case "Hammer":
                return ObjectName.Hammer;
            case "Missile":
                return ObjectName.Missile;
            case "BrokenSlot":
                return ObjectName.BrokenSlot;
            case "FreezePariclePrefab":
                return ObjectName.FreezePariclePrefab;
            case "Flow":
                return ObjectName.Flow;
            default:
                Debug.LogWarning("Object name not recognized: " + objName);
                return ObjectName.Hammer; // Default to Hammer or handle as needed
        
        }
    }
    

}
public enum ObjectName
{
    Hammer,
    Missile,
    BrokenSlot,
    FreezePariclePrefab,
    Flow,
}
