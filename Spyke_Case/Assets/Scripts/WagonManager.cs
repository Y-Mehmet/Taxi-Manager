using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// LevelSpawner'dan aldÄ±ÄŸÄ± veriyle vagonlarÄ± oluÅŸturur ve yÃ¶netir.
public class WagonManager : MonoBehaviour
{
    public static WagonManager Instance { get; private set; }

    // VagonlarÄ±n oyun iÃ§indeki gÃ¼ncel listesi.
    private List<MetroWagon> runtimeWagons = new List<MetroWagon>();

    // Bir vagon kaldÄ±rÄ±ldÄ±ÄŸÄ±nda tetiklenir. MetroManager bunu dinler.
    public event System.Action<MetroWagon, Transform> OnWagonRemoved;

    [Header("Spawn AyarlarÄ±")]
    public Vector3 startSpawnPoint = new Vector3(-3f, 0, 11f);
    public float distanceBetweenWagons = 1f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Initialize(List<WagonSpawnData> spawnData, MetroWagon wagonPrefab)
    {
        if (wagonPrefab == null)
        {
            Debug.LogError("WagonManager Initialize failed: Prefab not provided!");
            return;
        }

        // Ã–nceki level'dan kalan vagonlarÄ± temizle
        foreach (var wagon in runtimeWagons)
        {
            if (wagon != null) Destroy(wagon.gameObject);
        }
        runtimeWagons.Clear();

        // VagonlarÄ± baÅŸlangÄ±Ã§ noktasÄ±na gÃ¶re Z ekseninde sÄ±rala.
        for (int i = 0; i < spawnData.Count; i++)
        {
            var data = spawnData[i];
            Vector3 spawnPos = startSpawnPoint + new Vector3(0, 0, i * distanceBetweenWagons);
            Quaternion spawnRot = Quaternion.identity;
            
            MetroWagon newWagon = Instantiate(wagonPrefab, spawnPos, spawnRot, transform);
            
            // HATA DÃœZELTMESÄ°: Rengi doÄŸrudan atamak yerine public SetColor metodunu kullan.
            newWagon.SetColor(data.color);
            
            runtimeWagons.Add(newWagon);
        }
    }

    // MetroManager tarafÄ±ndan Ã§aÄŸrÄ±lÄ±r.
    public void RegisterWagon(MetroWagon wagon)
    {
        if (!runtimeWagons.Contains(wagon))
        {
            runtimeWagons.Add(wagon);
        }
    }

    // UberManager tarafÄ±ndan Ã§aÄŸrÄ±lÄ±r.
    public void DeregisterWagon(MetroWagon wagon)
    {
        if (runtimeWagons.Contains(wagon))
        {
            runtimeWagons.Remove(wagon);
        }
    }

    // UberManager tarafÄ±ndan Ã§aÄŸrÄ±lÄ±r.
    public void TriggerWagonRemovalEvent(MetroWagon wagon, Transform transform)
    {
        OnWagonRemoved?.Invoke(wagon, transform);
    }

    // MetroWagon tarafÄ±ndan Ã§aÄŸrÄ±lÄ±r.
    public void ReportWagonFilled(MetroWagon wagon)
    {
        // TODO: Bir vagon dolduÄŸunda yapÄ±lacak oyun mantÄ±ÄŸÄ±nÄ± buraya ekle.
        /* Debug.Log($"Wagon {wagon.name} is full!", wagon.gameObject); */
    }

    // MetroManager tarafÄ±ndan istenir.
    public List<MetroWagon> GetActiveWagons()
    {
        // Null referanslarÄ± temizleyerek gÃ¼ncel listeyi dÃ¶ndÃ¼r.
        runtimeWagons.RemoveAll(item => item == null);
        return runtimeWagons;
    }

    public MetroWagon FindWagon(HyperCasualColor color, int requiredCapacity = 1, int minCheckpointIndex = -1)
    {
        // Not: Bu metod artÄ±k sahnedeki deÄŸil, runtime'da oluÅŸturulan vagonlarÄ± kullanacak.
        // Initialize metodu doldurulduÄŸunda bu liste de dolu olacak.
        foreach (var wagon in runtimeWagons)
        {
            if (wagon == null || wagon.IsFull) continue;

            // BÃ¶lge kontrolÃ¼ (opsiyonel)
            if (minCheckpointIndex != -1 && wagon.GetCurrentCheckpointIndex() < minCheckpointIndex) continue;

            // Renk ve kapasite kontrolÃ¼
            if (wagon.wagonColor == color && (wagon.maxPassengerCount - wagon.passengerCount) >= requiredCapacity)
            {
                return wagon;
            }
        }
        return null;
    }

    // MetroManager tarafÄ±ndan renk karÄ±ÅŸtÄ±rma iÃ§in kullanÄ±lÄ±r.
    public static List<HyperCasualColor> ShuffleColorGroups(List<HyperCasualColor> originalColors)
    {
        if (originalColors == null || originalColors.Count < 2) return originalColors;

        List<HyperCasualColor> newColors = new List<HyperCasualColor>(originalColors);
        System.Random rng = new System.Random();

        // Fisher-Yates shuffle algoritmasÄ±
        int n = newColors.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            HyperCasualColor value = newColors[k];
            newColors[k] = newColors[n];
            newColors[n] = value;
        }

        // Ä°steÄŸe baÄŸlÄ±: HiÃ§bir rengin kendi orijinal yerinde kalmamasÄ±nÄ± saÄŸla (derangement)
        // Basit bir shuffle ÅŸimdilik yeterlidir.

        return newColors;
    }
}
