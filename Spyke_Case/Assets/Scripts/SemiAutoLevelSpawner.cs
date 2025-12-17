using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Passenger'lar manuel yerleştirilir, sistem sadece wagon dağılımını otomatik hesaplar.
/// Challenge color'lar için ilk 20 wagon'da daha az wagon yerleştirir ve dağıtır.
/// </summary>
public class SemiAutoLevelSpawner : MonoBehaviour
{
    [System.Serializable]
    public class PassengerColorInfo
    {
        public HyperCasualColor color;
        [Tooltip("Bu renkte kaç yolcu var (manuel yerleştirdiğin passenger'ları say)")]
        public int passengerCount;
    }

    [Header("Passenger Bilgisi (Manuel Yerleştirdiğin Passenger'lar)")]
    [SerializeField] private List<PassengerColorInfo> passengerColors = new List<PassengerColorInfo>();
    
    [Header("Zorluk Ayarları")]
    [Tooltip("Hangi renkler oyuncuyu zorlayacak? (Bu renklerde az wagon olacak)")]
    [SerializeField] private List<HyperCasualColor> challengeColors = new List<HyperCasualColor>();
    
    [Tooltip("İlk kaç wagon'da challenge uygulanacak")]
    [SerializeField] private int challengeWagonCount = 20;
    
    [Tooltip("Challenge color'lar için ilk 20'deki wagon oranı (örn: 0.4 = %40)")]
    [SerializeField] [Range(0.2f, 0.8f)] private float challengeRatioInFirst20 = 0.4f;

    [Header("Wagon Yerleştirme Ayarları")]
    [Tooltip("Vagonların yerleştirileceği başlangıç checkpoint indeksi")]
    [SerializeField] private int startCheckpointIndex = 0;
    
    [Tooltip("Vagonlar arası mesafe")]
    [SerializeField] private float wagonSpacing = 1.5f;

    [Header("Bağlantılar")]
    [SerializeField] private GameObject wagonPrefab;
    [SerializeField] private MetroCheckpointPath checkpointPath;
    [SerializeField] private Transform wagonContainer;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;

    private List<MetroWagon> spawnedWagons = new List<MetroWagon>();

    /// <summary>
    /// Inspector'dan çağrılabilir - Wagon'ları spawn eder
    /// </summary>
    [ContextMenu("Spawn Wagons")]
    public void SpawnWagons()
    {
        ClearExistingWagons();
        
        if (!ValidateSetup())
        {
            Debug.LogError("[SemiAutoLevelSpawner] Setup geçersiz! Lütfen tüm alanları doldurun.");
            return;
        }

        List<HyperCasualColor> wagonSequence = CalculateWagonSequence();
        PlaceWagonsOnPath(wagonSequence);
        
        if (showDebugInfo)
        {
            PrintWagonSummary(wagonSequence);
        }
    }

    /// <summary>
    /// Mevcut vagonları temizler
    /// </summary>
    [ContextMenu("Clear Wagons")]
    public void ClearExistingWagons()
    {
        // WagonManager'daki vagonları temizle
        if (WagonManager.Instance != null)
        {
            var activeWagons = WagonManager.Instance.GetActiveWagons();
            foreach (var wagon in activeWagons.ToList())
            {
                if (wagon != null)
                {
                    WagonManager.Instance.DeregisterWagon(wagon);
                    DestroyImmediate(wagon.gameObject);
                }
            }
        }

        // Container'daki tüm child objeleri temizle
        if (wagonContainer != null)
        {
            for (int i = wagonContainer.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(wagonContainer.GetChild(i).gameObject);
            }
        }

        spawnedWagons.Clear();
        Debug.Log("[SemiAutoLevelSpawner] Mevcut vagonlar temizlendi.");
    }

    private bool ValidateSetup()
    {
        if (passengerColors == null || passengerColors.Count == 0)
        {
            Debug.LogError("Passenger colors boş! Lütfen manuel yerleştirdiğin passenger'ları say ve ekle.");
            return false;
        }

        if (wagonPrefab == null)
        {
            Debug.LogError("Wagon prefab atanmamış!");
            return false;
        }

        if (checkpointPath == null || checkpointPath.checkpoints == null || checkpointPath.checkpoints.Count == 0)
        {
            Debug.LogError("Checkpoint path geçersiz!");
            return false;
        }

        if (wagonContainer == null)
        {
            Debug.LogError("Wagon container atanmamış!");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Passenger sayılarına ve challenge color'lara göre wagon sırasını hesaplar
    /// </summary>
    private List<HyperCasualColor> CalculateWagonSequence()
    {
        // 1. Her renk için wagon sayısını hesapla
        Dictionary<HyperCasualColor, int> wagonCounts = new Dictionary<HyperCasualColor, int>();
        Dictionary<HyperCasualColor, int> first20Counts = new Dictionary<HyperCasualColor, int>();
        
        int totalPassengers = passengerColors.Sum(p => p.passengerCount);
        int totalWagonsNeeded = Mathf.Max(challengeWagonCount, totalPassengers); // En az passenger sayısı kadar wagon

        foreach (var passengerInfo in passengerColors)
        {
            bool isChallenge = challengeColors.Contains(passengerInfo.color);
            
            // Toplam wagon sayısı (passenger sayısına eşit veya biraz fazla)
            int totalForColor = passengerInfo.passengerCount;
            
            // İlk 20'deki wagon sayısı
            int first20Count;
            if (isChallenge)
            {
                // Challenge color: İlk 20'de daha az
                first20Count = Mathf.Max(1, Mathf.RoundToInt(passengerInfo.passengerCount * challengeRatioInFirst20));
            }
            else
            {
                // Normal color: İlk 20'de daha fazla
                first20Count = Mathf.Min(passengerInfo.passengerCount, 
                    Mathf.RoundToInt(passengerInfo.passengerCount * 0.8f)); // %80'i ilk 20'de
            }

            wagonCounts[passengerInfo.color] = totalForColor;
            first20Counts[passengerInfo.color] = first20Count;

            if (showDebugInfo)
            {
                string tag = isChallenge ? "<color=red>[CHALLENGE]</color>" : "<color=green>[NORMAL]</color>";
                Debug.Log($"{tag} {passengerInfo.color}: {passengerInfo.passengerCount} yolcu → İlk 20'de {first20Count} wagon, Toplam {totalForColor} wagon");
            }
        }

        // 2. İlk 20 wagon'u oluştur (dağıtılmış şekilde)
        List<HyperCasualColor> first20Wagons = CreateDistributedSequence(first20Counts, challengeWagonCount);

        // 3. Kalan wagon'ları oluştur
        Dictionary<HyperCasualColor, int> remainingCounts = new Dictionary<HyperCasualColor, int>();
        foreach (var kvp in wagonCounts)
        {
            int remaining = kvp.Value - first20Counts[kvp.Key];
            if (remaining > 0)
            {
                remainingCounts[kvp.Key] = remaining;
            }
        }

        List<HyperCasualColor> remainingWagons = CreateDistributedSequence(remainingCounts, 100); // Kalan wagon'lar

        // 4. Birleştir
        List<HyperCasualColor> finalSequence = new List<HyperCasualColor>();
        finalSequence.AddRange(first20Wagons);
        finalSequence.AddRange(remainingWagons);

        return finalSequence;
    }

    /// <summary>
    /// Wagon'ları dağıtılmış şekilde sıralar (bbbbbb değil, r-bb-r-bb-g-bb gibi)
    /// </summary>
    private List<HyperCasualColor> CreateDistributedSequence(Dictionary<HyperCasualColor, int> counts, int maxCount)
    {
        List<HyperCasualColor> sequence = new List<HyperCasualColor>();
        
        // Her renkten kaç tane kaldığını takip et
        Dictionary<HyperCasualColor, int> remaining = new Dictionary<HyperCasualColor, int>(counts);
        
        int totalWagons = counts.Values.Sum();
        totalWagons = Mathf.Min(totalWagons, maxCount);

        // Dağıtılmış sıralama algoritması
        while (sequence.Count < totalWagons && remaining.Values.Any(v => v > 0))
        {
            // En çok kalan rengi bul
            var mostRemaining = remaining.Where(kvp => kvp.Value > 0)
                                        .OrderByDescending(kvp => kvp.Value)
                                        .FirstOrDefault();

            if (mostRemaining.Key == default(HyperCasualColor)) break;

            // Bu rengi ekle
            sequence.Add(mostRemaining.Key);
            remaining[mostRemaining.Key]--;

            // Aynı rengin arka arkaya gelmemesi için, diğer renklerden birini ekle
            var otherColors = remaining.Where(kvp => kvp.Value > 0 && kvp.Key != mostRemaining.Key)
                                      .OrderByDescending(kvp => kvp.Value)
                                      .ToList();

            if (otherColors.Any() && sequence.Count < totalWagons)
            {
                var otherColor = otherColors[Random.Range(0, Mathf.Min(2, otherColors.Count))]; // İlk 2'den birini seç
                sequence.Add(otherColor.Key);
                remaining[otherColor.Key]--;
            }
        }

        return sequence;
    }

    /// <summary>
    /// Wagon'ları path üzerine yerleştirir
    /// </summary>
    private void PlaceWagonsOnPath(List<HyperCasualColor> wagonSequence)
    {
        int currentCheckpointIndex = startCheckpointIndex;
        float currentOffset = 0f;

        for (int i = 0; i < wagonSequence.Count; i++)
        {
            HyperCasualColor wagonColor = wagonSequence[i];
            
            // Checkpoint pozisyonunu al
            Vector3 spawnPosition = GetPositionOnPath(currentCheckpointIndex, currentOffset);
            Quaternion spawnRotation = GetRotationOnPath(currentCheckpointIndex);

            // Wagon'u spawn et
            GameObject wagonObj = Instantiate(wagonPrefab, spawnPosition, spawnRotation, wagonContainer);
            wagonObj.name = $"Wagon_{wagonColor}_{i}";

            MetroWagon wagon = wagonObj.GetComponent<MetroWagon>();
            if (wagon != null)
            {
                // SetColor metodu hem rengi set eder hem de materyali değiştirir
                wagon.SetColor(wagonColor);
                
                // WagonManager'a kaydet
                if (WagonManager.Instance != null)
                {
                    WagonManager.Instance.RegisterWagon(wagon);
                }

                spawnedWagons.Add(wagon);
            }

            // Bir sonraki pozisyonu hesapla
            currentOffset += wagonSpacing;
            
            // Eğer offset checkpoint arası mesafeden büyükse, bir sonraki checkpoint'e geç
            if (currentCheckpointIndex < checkpointPath.checkpoints.Count - 1)
            {
                float segmentLength = Vector3.Distance(
                    checkpointPath.checkpoints[currentCheckpointIndex].position,
                    checkpointPath.checkpoints[currentCheckpointIndex + 1].position
                );

                if (currentOffset >= segmentLength)
                {
                    currentOffset -= segmentLength;
                    currentCheckpointIndex++;
                }
            }
        }

        Debug.Log($"<color=cyan>[SemiAutoLevelSpawner]</color> {wagonSequence.Count} wagon başarıyla yerleştirildi!");
    }

    private Vector3 GetPositionOnPath(int checkpointIndex, float offset)
    {
        if (checkpointIndex >= checkpointPath.checkpoints.Count - 1)
        {
            return checkpointPath.checkpoints[checkpointPath.checkpoints.Count - 1].position;
        }

        Vector3 start = checkpointPath.checkpoints[checkpointIndex].position;
        Vector3 end = checkpointPath.checkpoints[checkpointIndex + 1].position;
        Vector3 direction = (end - start).normalized;

        return start + direction * offset;
    }

    private Quaternion GetRotationOnPath(int checkpointIndex)
    {
        if (checkpointIndex >= checkpointPath.checkpoints.Count - 1)
        {
            return checkpointPath.checkpoints[checkpointPath.checkpoints.Count - 1].rotation;
        }

        Vector3 start = checkpointPath.checkpoints[checkpointIndex].position;
        Vector3 end = checkpointPath.checkpoints[checkpointIndex + 1].position;
        Vector3 direction = (end - start).normalized;

        if (direction != Vector3.zero)
        {
            return Quaternion.LookRotation(direction);
        }

        return checkpointPath.checkpoints[checkpointIndex].rotation;
    }

    private void PrintWagonSummary(List<HyperCasualColor> wagonSequence)
    {
        Debug.Log("═══════════════════════════════════════════");
        Debug.Log("<color=cyan><b>WAGON DISTRIBUTION SUMMARY</b></color>");
        Debug.Log("═══════════════════════════════════════════");

        // İlk 20 wagon analizi
        var first20 = wagonSequence.Take(challengeWagonCount).ToList();
        var colorGroups = first20.GroupBy(c => c).OrderByDescending(g => g.Count());

        Debug.Log($"<color=yellow>İLK {challengeWagonCount} WAGON:</color>");
        foreach (var group in colorGroups)
        {
            bool isChallenge = challengeColors.Contains(group.Key);
            string tag = isChallenge ? "<color=red>[CHALLENGE]</color>" : "";
            Debug.Log($"  {tag} {group.Key}: {group.Count()} wagon");
        }

        Debug.Log("───────────────────────────────────────────");

        // Toplam analiz
        var allColorGroups = wagonSequence.GroupBy(c => c).OrderByDescending(g => g.Count());
        Debug.Log($"<color=yellow>TOPLAM {wagonSequence.Count} WAGON:</color>");
        foreach (var group in allColorGroups)
        {
            var passengerInfo = passengerColors.Find(p => p.color == group.Key);
            int passengerCount = passengerInfo != null ? passengerInfo.passengerCount : 0;
            Debug.Log($"  {group.Key}: {group.Count()} wagon ({passengerCount} yolcu)");
        }

        Debug.Log("───────────────────────────────────────────");
        
        // İlk 30 wagon sırası
        string sequencePreview = string.Join("-", wagonSequence.Take(30).Select(c => c.ToString().Substring(0, 1)));
        Debug.Log($"<color=yellow>İLK 30 WAGON SIRASI:</color>");
        Debug.Log($"  {sequencePreview}...");
        
        Debug.Log("═══════════════════════════════════════════");
    }

    void OnDrawGizmosSelected()
    {
        if (checkpointPath == null || checkpointPath.checkpoints == null || checkpointPath.checkpoints.Count == 0)
            return;

        // Spawn bölgesini göster
        Gizmos.color = Color.green;
        if (startCheckpointIndex < checkpointPath.checkpoints.Count)
        {
            Vector3 startPos = checkpointPath.checkpoints[startCheckpointIndex].position;
            Gizmos.DrawWireSphere(startPos, 0.5f);
            
            // İlk 5 checkpoint'i göster
            for (int i = startCheckpointIndex; i < Mathf.Min(startCheckpointIndex + 5, checkpointPath.checkpoints.Count); i++)
            {
                Gizmos.DrawWireSphere(checkpointPath.checkpoints[i].position, 0.3f);
            }
        }
    }
}
