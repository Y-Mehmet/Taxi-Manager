using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Semi-Auto Level Spawner için Unity Editor Window
/// Passenger bilgilerini gir, challenge color'ları seç, wagon'ları otomatik spawn et
/// </summary>
public class SemiAutoLevelSpawnerEditor : EditorWindow
{
    [System.Serializable]
    private class PassengerColorData
    {
        public HyperCasualColor color;
        public int count;
        public bool isChallenge;
    }

    // Passenger Data
    private List<PassengerColorData> passengerData = new List<PassengerColorData>();
    
    // Settings
    private int challengeWagonCount = 20;
    private float challengeRatio = 0.4f;
    private int startCheckpointIndex = 0;
    private float wagonSpacing = 1.5f;
    
    // References
    private GameObject wagonPrefab;
    private MetroCheckpointPath checkpointPath;
    private Transform wagonContainer;
    
    // UI State
    private Vector2 scrollPosition;
    private bool showAdvancedSettings = false;

    [MenuItem("Tools/Semi-Auto Level Spawner")]
    public static void ShowWindow()
    {
        var window = GetWindow<SemiAutoLevelSpawnerEditor>("Level Spawner");
        window.minSize = new Vector2(400, 600);
        window.Show();
    }

    private void OnEnable()
    {
        // Varsayılan passenger data
        if (passengerData.Count == 0)
        {
            passengerData.Add(new PassengerColorData { color = HyperCasualColor.Blue, count = 0, isChallenge = false });
            passengerData.Add(new PassengerColorData { color = HyperCasualColor.Red, count = 0, isChallenge = false });
            passengerData.Add(new PassengerColorData { color = HyperCasualColor.Green, count = 0, isChallenge = false });
            passengerData.Add(new PassengerColorData { color = HyperCasualColor.Yellow, count = 0, isChallenge = false });
            passengerData.Add(new PassengerColorData { color = HyperCasualColor.Orange, count = 0, isChallenge = false });
            passengerData.Add(new PassengerColorData { color = HyperCasualColor.Purple, count = 0, isChallenge = false });
            passengerData.Add(new PassengerColorData { color = HyperCasualColor.Pink, count = 0, isChallenge = false });
            passengerData.Add(new PassengerColorData { color = HyperCasualColor.Cyan, count = 0, isChallenge = false });
        }

        // Sahneden referansları bulmaya çalış
        if (checkpointPath == null)
        {
            checkpointPath = FindObjectOfType<MetroCheckpointPath>();
        }
        if (wagonContainer == null)
        {
            var container = GameObject.Find("WagonContainer");
            if (container != null) wagonContainer = container.transform;
        }
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // Header
        GUILayout.Space(10);
        EditorGUILayout.LabelField("Semi-Auto Level Spawner", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("1. Passenger sayılarını gir (manuel yerleştirdiğin passenger'ları say)\n" +
                               "2. Challenge color'ları işaretle (zorlanacak renkler)\n" +
                               "3. 'Spawn Wagons' butonuna bas!", MessageType.Info);
        GUILayout.Space(10);

        // References Section
        DrawReferencesSection();
        GUILayout.Space(10);

        // Passenger Data Section
        DrawPassengerDataSection();
        GUILayout.Space(10);

        // Settings Section
        DrawSettingsSection();
        GUILayout.Space(10);

        // Preview Section
        DrawPreviewSection();
        GUILayout.Space(10);

        // Action Buttons
        DrawActionButtons();

        EditorGUILayout.EndScrollView();
    }

    private void DrawReferencesSection()
    {
        EditorGUILayout.LabelField("Referanslar", EditorStyles.boldLabel);
        
        wagonPrefab = (GameObject)EditorGUILayout.ObjectField("Wagon Prefab", wagonPrefab, typeof(GameObject), false);
        checkpointPath = (MetroCheckpointPath)EditorGUILayout.ObjectField("Checkpoint Path", checkpointPath, typeof(MetroCheckpointPath), true);
        wagonContainer = (Transform)EditorGUILayout.ObjectField("Wagon Container", wagonContainer, typeof(Transform), true);

        if (wagonPrefab == null || checkpointPath == null || wagonContainer == null)
        {
            EditorGUILayout.HelpBox("Lütfen tüm referansları ata!", MessageType.Warning);
        }
    }

    private void DrawPassengerDataSection()
    {
        EditorGUILayout.LabelField("Passenger Bilgileri (Manuel Yerleştirdiğin Passenger'lar)", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginVertical("box");
        
        // Header
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Renk", GUILayout.Width(80));
        EditorGUILayout.LabelField("Yolcu Sayısı", GUILayout.Width(100));
        EditorGUILayout.LabelField("Zorla?", GUILayout.Width(60));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Passenger rows
        for (int i = 0; i < passengerData.Count; i++)
        {
            var data = passengerData[i];
            
            EditorGUILayout.BeginHorizontal();
            
            // Color label with color
            GUI.color = GetColorForEnum(data.color);
            EditorGUILayout.LabelField(data.color.ToString(), GUILayout.Width(80));
            GUI.color = Color.white;
            
            // Count field
            data.count = EditorGUILayout.IntField(data.count, GUILayout.Width(100));
            data.count = Mathf.Max(0, data.count);
            
            // Challenge toggle
            data.isChallenge = EditorGUILayout.Toggle(data.isChallenge, GUILayout.Width(60));
            
            // Challenge indicator
            if (data.isChallenge && data.count > 0)
            {
                GUI.color = Color.red;
                EditorGUILayout.LabelField("⚠ CHALLENGE", GUILayout.Width(100));
                GUI.color = Color.white;
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndVertical();

        // Summary
        int totalPassengers = passengerData.Sum(p => p.count);
        int challengeCount = passengerData.Count(p => p.isChallenge && p.count > 0);
        EditorGUILayout.LabelField($"Toplam: {totalPassengers} yolcu, {challengeCount} challenge renk", EditorStyles.miniLabel);
    }

    private void DrawSettingsSection()
    {
        showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "Gelişmiş Ayarlar", true);
        
        if (showAdvancedSettings)
        {
            EditorGUILayout.BeginVertical("box");
            
            challengeWagonCount = EditorGUILayout.IntSlider("İlk Kaç Wagon'da Challenge", challengeWagonCount, 10, 50);
            challengeRatio = EditorGUILayout.Slider("Challenge Wagon Oranı", challengeRatio, 0.2f, 0.8f);
            
            EditorGUILayout.Space(5);
            
            startCheckpointIndex = EditorGUILayout.IntField("Başlangıç Checkpoint", startCheckpointIndex);
            wagonSpacing = EditorGUILayout.FloatField("Wagon Arası Mesafe", wagonSpacing);
            
            EditorGUILayout.EndVertical();
        }
    }

    private void DrawPreviewSection()
    {
        EditorGUILayout.LabelField("Önizleme", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");

        var activePassengers = passengerData.Where(p => p.count > 0).ToList();
        if (activePassengers.Count == 0)
        {
            EditorGUILayout.HelpBox("Henüz passenger bilgisi girilmedi.", MessageType.Info);
        }
        else
        {
            foreach (var data in activePassengers)
            {
                int wagonCountForColor = data.count;
                int first20Wagons = data.isChallenge 
                    ? Mathf.Max(1, Mathf.RoundToInt(data.count * challengeRatio))
                    : Mathf.Min(data.count, Mathf.RoundToInt(data.count * 0.8f));

                GUI.color = GetColorForEnum(data.color);
                string challengeTag = data.isChallenge ? " [CHALLENGE]" : "";
                EditorGUILayout.LabelField($"{data.color}{challengeTag}: {data.count} yolcu → İlk {challengeWagonCount}'de {first20Wagons} wagon, Toplam {wagonCountForColor} wagon");
                GUI.color = Color.white;
            }

            EditorGUILayout.Space(5);
            int totalWagons = activePassengers.Sum(p => p.count);
            EditorGUILayout.LabelField($"Toplam Wagon: {totalWagons}", EditorStyles.boldLabel);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawActionButtons()
    {
        EditorGUILayout.BeginHorizontal();
        
        GUI.enabled = CanSpawn();
        if (GUILayout.Button("🚀 Spawn Wagons", GUILayout.Height(40)))
        {
            SpawnWagons();
        }
        GUI.enabled = true;

        if (GUILayout.Button("🗑️ Clear Wagons", GUILayout.Height(40)))
        {
            ClearWagons();
        }

        EditorGUILayout.EndHorizontal();

        if (!CanSpawn())
        {
            EditorGUILayout.HelpBox("Spawn etmek için:\n- Tüm referansları ata\n- En az 1 passenger bilgisi gir", MessageType.Warning);
        }
    }

    private bool CanSpawn()
    {
        return wagonPrefab != null && 
               checkpointPath != null && 
               wagonContainer != null && 
               passengerData.Any(p => p.count > 0);
    }

    private void SpawnWagons()
    {
        if (!CanSpawn()) return;

        ClearWagons();

        List<HyperCasualColor> wagonSequence = CalculateWagonSequence();
        PlaceWagonsOnPath(wagonSequence);

        Debug.Log($"<color=cyan>[SemiAutoLevelSpawner]</color> {wagonSequence.Count} wagon başarıyla spawn edildi!");
        PrintSummary(wagonSequence);
    }

    private void ClearWagons()
    {
        if (wagonContainer == null) return;

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
        for (int i = wagonContainer.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(wagonContainer.GetChild(i).gameObject);
        }

        Debug.Log("[SemiAutoLevelSpawner] Mevcut vagonlar temizlendi.");
    }

    private List<HyperCasualColor> CalculateWagonSequence()
    {
        Dictionary<HyperCasualColor, int> first20Counts = new Dictionary<HyperCasualColor, int>();
        Dictionary<HyperCasualColor, int> totalCounts = new Dictionary<HyperCasualColor, int>();

        foreach (var data in passengerData.Where(p => p.count > 0))
        {
            int first20Count = data.isChallenge
                ? Mathf.Max(1, Mathf.RoundToInt(data.count * challengeRatio))
                : Mathf.Min(data.count, Mathf.RoundToInt(data.count * 0.8f));

            first20Counts[data.color] = first20Count;
            totalCounts[data.color] = data.count;
        }

        // İlk 20 wagon (dağıtılmış)
        List<HyperCasualColor> first20 = CreateDistributedSequence(first20Counts, challengeWagonCount);

        // Kalan wagon'lar
        Dictionary<HyperCasualColor, int> remaining = new Dictionary<HyperCasualColor, int>();
        foreach (var kvp in totalCounts)
        {
            int rem = kvp.Value - first20Counts[kvp.Key];
            if (rem > 0) remaining[kvp.Key] = rem;
        }

        List<HyperCasualColor> remainingWagons = CreateDistributedSequence(remaining, 100);

        List<HyperCasualColor> final = new List<HyperCasualColor>();
        final.AddRange(first20);
        final.AddRange(remainingWagons);

        return final;
    }

    private List<HyperCasualColor> CreateDistributedSequence(Dictionary<HyperCasualColor, int> counts, int maxCount)
    {
        List<HyperCasualColor> sequence = new List<HyperCasualColor>();
        Dictionary<HyperCasualColor, int> remaining = new Dictionary<HyperCasualColor, int>(counts);

        int totalWagons = Mathf.Min(counts.Values.Sum(), maxCount);

        while (sequence.Count < totalWagons && remaining.Values.Any(v => v > 0))
        {
            var mostRemaining = remaining.Where(kvp => kvp.Value > 0)
                                        .OrderByDescending(kvp => kvp.Value)
                                        .FirstOrDefault();

            if (mostRemaining.Key == default(HyperCasualColor)) break;

            sequence.Add(mostRemaining.Key);
            remaining[mostRemaining.Key]--;

            var otherColors = remaining.Where(kvp => kvp.Value > 0 && kvp.Key != mostRemaining.Key)
                                      .OrderByDescending(kvp => kvp.Value)
                                      .ToList();

            if (otherColors.Any() && sequence.Count < totalWagons)
            {
                var otherColor = otherColors[Random.Range(0, Mathf.Min(2, otherColors.Count))];
                sequence.Add(otherColor.Key);
                remaining[otherColor.Key]--;
            }
        }

        return sequence;
    }

    private void PlaceWagonsOnPath(List<HyperCasualColor> wagonSequence)
    {
        int currentCheckpointIndex = startCheckpointIndex;
        float currentOffset = 0f;

        for (int i = 0; i < wagonSequence.Count; i++)
        {
            HyperCasualColor wagonColor = wagonSequence[i];

            Vector3 spawnPosition = GetPositionOnPath(currentCheckpointIndex, currentOffset);
            Quaternion spawnRotation = GetRotationOnPath(currentCheckpointIndex);

            GameObject wagonObj = (GameObject)PrefabUtility.InstantiatePrefab(wagonPrefab, wagonContainer);
            wagonObj.transform.position = spawnPosition;
            wagonObj.transform.rotation = spawnRotation;
            wagonObj.name = $"Wagon_{wagonColor}_{i}";

            MetroWagon wagon = wagonObj.GetComponent<MetroWagon>();
            if (wagon != null)
            {
                // SetColor metodu hem rengi set eder hem de materyali değiştirir
                wagon.SetColor(wagonColor);
                
                if (WagonManager.Instance != null)
                {
                    WagonManager.Instance.RegisterWagon(wagon);
                }
            }

            currentOffset += wagonSpacing;

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

    private void PrintSummary(List<HyperCasualColor> wagonSequence)
    {
        Debug.Log("═══════════════════════════════════════════");
        Debug.Log("<color=cyan><b>WAGON DISTRIBUTION SUMMARY</b></color>");
        Debug.Log("═══════════════════════════════════════════");

        var first20 = wagonSequence.Take(challengeWagonCount).ToList();
        var colorGroups = first20.GroupBy(c => c).OrderByDescending(g => g.Count());

        Debug.Log($"<color=yellow>İLK {challengeWagonCount} WAGON:</color>");
        foreach (var group in colorGroups)
        {
            var data = passengerData.Find(p => p.color == group.Key);
            string tag = data != null && data.isChallenge ? "<color=red>[CHALLENGE]</color>" : "";
            Debug.Log($"  {tag} {group.Key}: {group.Count()} wagon");
        }

        Debug.Log("───────────────────────────────────────────");

        var allColorGroups = wagonSequence.GroupBy(c => c).OrderByDescending(g => g.Count());
        Debug.Log($"<color=yellow>TOPLAM {wagonSequence.Count} WAGON:</color>");
        foreach (var group in allColorGroups)
        {
            var data = passengerData.Find(p => p.color == group.Key);
            int passengerCount = data != null ? data.count : 0;
            Debug.Log($"  {group.Key}: {group.Count()} wagon ({passengerCount} yolcu)");
        }

        Debug.Log("───────────────────────────────────────────");

        string sequencePreview = string.Join("-", wagonSequence.Take(30).Select(c => c.ToString().Substring(0, 1)));
        Debug.Log($"<color=yellow>İLK 30 WAGON SIRASI:</color>");
        Debug.Log($"  {sequencePreview}...");

        Debug.Log("═══════════════════════════════════════════");
    }

    private Color GetColorForEnum(HyperCasualColor color)
    {
        switch (color)
        {
            case HyperCasualColor.Blue: return Color.blue;
            case HyperCasualColor.Red: return Color.red;
            case HyperCasualColor.Green: return Color.green;
            case HyperCasualColor.Yellow: return Color.yellow;
            case HyperCasualColor.Orange: return new Color(1f, 0.5f, 0f);
            case HyperCasualColor.Purple: return new Color(0.5f, 0f, 0.5f);
            case HyperCasualColor.Pink: return new Color(1f, 0.4f, 0.7f);
            case HyperCasualColor.Cyan: return Color.cyan;
            default: return Color.white;
        }
    }
}
