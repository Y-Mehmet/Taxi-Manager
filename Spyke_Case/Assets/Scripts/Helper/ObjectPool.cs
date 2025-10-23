using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// ObjectPool class implements a basic object pooling system using the Singleton pattern.
/// </summary>
public class ObjectPool : Singleton<ObjectPool>
{
    /// <summary>
    /// List of prefab objects that can be instantiated when needed.
    /// </summary>
    public List<GameObject> PrefabsForPool;

    /// <summary>
    /// List of currently pooled (inactive) objects.
    /// </summary>
    List<GameObject> _pooledObjects = new List<GameObject>();

    /// <summary>
    /// Retrieves an object with the specified name from the pool.
    /// </summary>
    /// <param name="objectName">The name of the object to retrieve.</param>
    /// <returns>An active GameObject instance, or null if not found.</returns>
    private void Start()
    {
        foreach (var prefab in PrefabsForPool)
        {
            // Instantiate each prefab and add it to the pool
            var instance = Instantiate(prefab, Vector3.zero, Quaternion.identity, transform);
            instance.transform.localPosition = Vector3.zero;
            instance.name = prefab.name; // Set the name to match the prefab
            instance.SetActive(false); // Initially set to inactive
            _pooledObjects.Add(instance);
        }
    }
    public GameObject GetObjectFromPool(string objectName)
    {
        

      
        var instance = _pooledObjects.FirstOrDefault(o => o.name == objectName);

        if (instance != null)
        {
            // Bulunan objeyi havuzdan ��kar, aktif et ve d�nd�r
            _pooledObjects.Remove(instance);
            instance.SetActive(true);
            return instance;
        }

        // Havuzda yoksa, prefab listesinden yeni bir tane olu�turmay� dene
        var prefab = PrefabsForPool.FirstOrDefault(o => o.name == objectName);

        if (prefab != null)
        {
            // Yeni bir obje olu�tur ve d�nd�r
            var newInstace = Instantiate(prefab, Vector3.zero, Quaternion.identity, transform);
            newInstace.transform.localPosition = Vector3.zero;
            newInstace.name = objectName;
            return newInstace;
        }

        // E�le�en bir prefab bulunamazsa uyar� ver
        Debug.LogWarning($"{objectName} prefab not found in the prefab list.");
        return null;
    }

    /// <summary>
    /// Adds the specified object back into the pool for future reuse.
    /// </summary>
    /// <param name="gameObject">The GameObject to pool.</param>
    public void PoolObject(GameObject gameObject)
    {
        gameObject.SetActive(false);
        _pooledObjects.Add(gameObject);
    }
}
