using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// LevelSpawner'dan aldığı veriyle vagonları oluşturur ve yönetir.
public class WagonManager : MonoBehaviour
{
    public static WagonManager Instance { get; private set; }

    // Vagonların oyun içindeki güncel listesi.
    private List<MetroWagon> runtimeWagons = new List<MetroWagon>();

    // Bir vagon kaldırıldığında tetiklenir. MetroManager bunu dinler.
    public event System.Action<MetroWagon, Transform> OnWagonRemoved;

    [Header("Spawn Ayarları")]
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

        // Önceki level'dan kalan vagonları temizle
        foreach (var wagon in runtimeWagons)
        {
            if (wagon != null) Destroy(wagon.gameObject);
        }
        runtimeWagons.Clear();

        // Vagonları başlangıç noktasına göre Z ekseninde sırala.
        for (int i = 0; i < spawnData.Count; i++)
        {
            var data = spawnData[i];
            Vector3 spawnPos = startSpawnPoint + new Vector3(0, 0, i * distanceBetweenWagons);
            Quaternion spawnRot = Quaternion.identity;
            
            MetroWagon newWagon = Instantiate(wagonPrefab, spawnPos, spawnRot, transform);
            
            // HATA DÜZELTMESİ: Rengi doğrudan atamak yerine public SetColor metodunu kullan.
            newWagon.SetColor(data.color);
            
            runtimeWagons.Add(newWagon);
        }
    }

    // MetroManager tarafından çağrılır.
    public void RegisterWagon(MetroWagon wagon)
    {
        if (!runtimeWagons.Contains(wagon))
        {
            runtimeWagons.Add(wagon);
        }
    }

    // UberManager tarafından çağrılır.
    public void DeregisterWagon(MetroWagon wagon)
    {
        if (runtimeWagons.Contains(wagon))
        {
            runtimeWagons.Remove(wagon);
        }
    }

    // UberManager tarafından çağrılır.
    public void TriggerWagonRemovalEvent(MetroWagon wagon, Transform transform)
    {
        OnWagonRemoved?.Invoke(wagon, transform);
    }

    // MetroWagon tarafından çağrılır.
    public void ReportWagonFilled(MetroWagon wagon)
    {
        // TODO: Bir vagon dolduğunda yapılacak oyun mantığını buraya ekle.
        Debug.Log($"Wagon {wagon.name} is full!", wagon.gameObject);
    }

    // MetroManager tarafından istenir.
    public List<MetroWagon> GetActiveWagons()
    {
        // Null referansları temizleyerek güncel listeyi döndür.
        runtimeWagons.RemoveAll(item => item == null);
        return runtimeWagons;
    }

    public MetroWagon FindWagon(HyperCasualColor color, int requiredCapacity = 1, int minCheckpointIndex = -1)
    {
        // Not: Bu metod artık sahnedeki değil, runtime'da oluşturulan vagonları kullanacak.
        // Initialize metodu doldurulduğunda bu liste de dolu olacak.
        foreach (var wagon in runtimeWagons)
        {
            if (wagon == null || wagon.IsFull) continue;

            // Bölge kontrolü (opsiyonel)
            if (minCheckpointIndex != -1 && wagon.GetCurrentCheckpointIndex() < minCheckpointIndex) continue;

            // Renk ve kapasite kontrolü
            if (wagon.wagonColor == color && (wagon.maxPassengerCount - wagon.passengerCount) >= requiredCapacity)
            {
                return wagon;
            }
        }
        return null;
    }

    // MetroManager tarafından renk karıştırma için kullanılır.
    public static List<HyperCasualColor> ShuffleColorGroups(List<HyperCasualColor> originalColors)
    {
        if (originalColors == null || originalColors.Count < 2) return originalColors;

        List<HyperCasualColor> newColors = new List<HyperCasualColor>(originalColors);
        System.Random rng = new System.Random();

        // Fisher-Yates shuffle algoritması
        int n = newColors.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            HyperCasualColor value = newColors[k];
            newColors[k] = newColors[n];
            newColors[n] = value;
        }

        // İsteğe bağlı: Hiçbir rengin kendi orijinal yerinde kalmamasını sağla (derangement)
        // Basit bir shuffle şimdilik yeterlidir.

        return newColors;
    }
}