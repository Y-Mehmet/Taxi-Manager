using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Quick Passenger Data'dan passenger listesini alır ve LevelSpawnData oluşturur
/// </summary>
public class SemiAutoLevelDataCreator : EditorWindow
{
    [System.Serializable]
    private class ColorData
    {
        public HyperCasualColor color;
        public int passengerCount;
        public bool isChallenge;
    }

    // Passenger data from Quick Input
    private List<QuickPassengerDataEditor.PassengerData> receivedPassengerData = new List<QuickPassengerDataEditor.PassengerData>();
    
    // Color analysis
    private List<ColorData> colorDataList = new List<ColorData>();
    
    // Settings
    private int challengeWagonCount = 20;
    private float challengeRatio = 0.4f;
    
    // Level data
    private string levelName = "NewLevel";
    private int levelIndex = 1;
    
    // UI
    private Vector2 scrollPosition;

    [MenuItem("Tools/Semi-Auto Level Data Creator")]
    public static void ShowWindow()
    {
        var window = GetWindow<SemiAutoLevelDataCreator>("Level Data Creator");
        window.minSize = new Vector2(450, 700);
        window.Show();
    }

    public void SetPassengerData(List<QuickPassengerDataEditor.PassengerData> passengerData)
    {
        receivedPassengerData = new List<QuickPassengerDataEditor.PassengerData>(passengerData);
        AnalyzePassengerData();
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // Header
        GUILayout.Space(10);
        EditorGUILayout.LabelField("🎯 Semi-Auto Level Data Creator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("1. Quick Passenger Data Input'tan passenger listesini al\n" +
                               "2. Renk dağılımını analiz et\n" +
                               "3. Challenge color'ları işaretle\n" +
                               "4. LevelSpawnData.asset oluştur!", MessageType.Info);
        GUILayout.Space(10);

        // Passenger Data Summary
        DrawPassengerSummary();
        GUILayout.Space(10);

        // Color Analysis
        DrawColorAnalysis();
        GUILayout.Space(10);

        // Settings
        DrawSettings();
        GUILayout.Space(10);

        // Wagon Preview
        DrawWagonPreview();
        GUILayout.Space(10);

        // Action Buttons
        DrawActionButtons();

        EditorGUILayout.EndScrollView();
    }

    private void DrawPassengerSummary()
    {
        EditorGUILayout.LabelField($"📊 Passenger Özeti ({receivedPassengerData.Count} adet)", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        
        if (receivedPassengerData.Count == 0)
        {
            EditorGUILayout.HelpBox("Henüz passenger data'sı yok!\nQuick Passenger Data Input'tan listeyi gönder.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.LabelField($"Toplam Passenger: {receivedPassengerData.Count}", EditorStyles.miniLabel);
            
            var colorGroups = receivedPassengerData.GroupBy(p => p.color).OrderByDescending(g => g.Count());
            foreach (var group in colorGroups)
            {
                GUI.color = GetColorForEnum(group.Key);
                EditorGUILayout.LabelField($"  {group.Key}: {group.Count()} passenger", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }
        }
        
        EditorGUILayout.EndVertical();
    }

    private void DrawColorAnalysis()
    {
        EditorGUILayout.LabelField("🎨 Renk Analizi ve Challenge Seçimi", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        
        if (colorDataList.Count == 0)
        {
            EditorGUILayout.HelpBox("Passenger data'sı analiz edilmedi.", MessageType.Info);
        }
        else
        {
            // Header
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Renk", GUILayout.Width(80));
            EditorGUILayout.LabelField("Passenger", GUILayout.Width(80));
            EditorGUILayout.LabelField("Challenge?", GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // Color rows
            foreach (var colorData in colorDataList)
            {
                EditorGUILayout.BeginHorizontal();
                
                GUI.color = GetColorForEnum(colorData.color);
                EditorGUILayout.LabelField(colorData.color.ToString(), GUILayout.Width(80));
                GUI.color = Color.white;
                
                EditorGUILayout.LabelField(colorData.passengerCount.ToString(), GUILayout.Width(80));
                
                colorData.isChallenge = EditorGUILayout.Toggle(colorData.isChallenge, GUILayout.Width(80));
                
                if (colorData.isChallenge)
                {
                    GUI.color = Color.red;
                    EditorGUILayout.LabelField("⚠ CHALLENGE", GUILayout.Width(100));
                    GUI.color = Color.white;
                }
                
                EditorGUILayout.EndHorizontal();
            }
        }
        
        EditorGUILayout.EndVertical();
    }

    private void DrawSettings()
    {
        EditorGUILayout.LabelField("⚙️ Ayarlar", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        
        levelName = EditorGUILayout.TextField("Level Adı", levelName);
        levelIndex = EditorGUILayout.IntField("Level Index", levelIndex);
        
        EditorGUILayout.Space(5);
        
        challengeWagonCount = EditorGUILayout.IntSlider("İlk Kaç Wagon'da Challenge", challengeWagonCount, 10, 50);
        challengeRatio = EditorGUILayout.Slider("Challenge Wagon Oranı", challengeRatio, 0.2f, 0.8f);
        
        EditorGUILayout.EndVertical();
    }

    private void DrawWagonPreview()
    {
        EditorGUILayout.LabelField("🚂 Wagon Önizleme", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        
        if (colorDataList.Count == 0)
        {
            EditorGUILayout.HelpBox("Önizleme için passenger data'sı gerekli.", MessageType.Info);
        }
        else
        {
            foreach (var colorData in colorDataList)
            {
                int wagonCountForColor = colorData.passengerCount;
                int first20Wagons = colorData.isChallenge
                    ? Mathf.Max(1, Mathf.RoundToInt(colorData.passengerCount * challengeRatio))
                    : Mathf.Min(colorData.passengerCount, Mathf.RoundToInt(colorData.passengerCount * 0.8f));
                
                GUI.color = GetColorForEnum(colorData.color);
                string challengeTag = colorData.isChallenge ? " [CHALLENGE]" : "";
                EditorGUILayout.LabelField($"{colorData.color}{challengeTag}: {colorData.passengerCount} passenger → İlk {challengeWagonCount}'de {first20Wagons} wagon, Toplam {wagonCountForColor} wagon");
                GUI.color = Color.white;
            }
            
            EditorGUILayout.Space(5);
            int totalWagons = colorDataList.Sum(c => c.passengerCount);
            EditorGUILayout.LabelField($"Toplam Wagon: {totalWagons}", EditorStyles.boldLabel);
        }
        
        EditorGUILayout.EndVertical();
    }

    private void DrawActionButtons()
    {
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("🔍 Passenger Data'yı Analiz Et", GUILayout.Height(40)))
        {
            AnalyzePassengerData();
        }
        
        GUI.enabled = colorDataList.Count > 0;
        if (GUILayout.Button("💾 LevelSpawnData Oluştur", GUILayout.Height(40)))
        {
            CreateLevelSpawnData();
        }
        GUI.enabled = true;
        
        EditorGUILayout.EndHorizontal();
    }

    private void AnalyzePassengerData()
    {
        if (receivedPassengerData.Count == 0)
        {
            EditorUtility.DisplayDialog("Hata", "Passenger data'sı yok! Quick Passenger Data Input'tan listeyi gönder.", "Tamam");
            return;
        }
        
        colorDataList.Clear();
        
        var colorGroups = receivedPassengerData.GroupBy(p => p.color);
        foreach (var group in colorGroups)
        {
            colorDataList.Add(new ColorData
            {
                color = group.Key,
                passengerCount = group.Count(),
                isChallenge = false
            });
        }
        
        colorDataList = colorDataList.OrderByDescending(c => c.passengerCount).ToList();
        
        Debug.Log($"✅ {receivedPassengerData.Count} passenger analiz edildi, {colorDataList.Count} farklı renk bulundu.");
    }

    private void CreateLevelSpawnData()
    {
        if (colorDataList.Count == 0)
        {
            EditorUtility.DisplayDialog("Hata", "Önce passenger data'sını analiz et!", "Tamam");
            return;
        }
        
        // Wagon sequence hesapla
        List<HyperCasualColor> wagonSequence = CalculateWagonSequence();
        
        // LevelSpawnSO oluştur
        LevelSpawnSO levelData = ScriptableObject.CreateInstance<LevelSpawnSO>();
        
        // Passenger data'sını ekle
        levelData.initialPassengerGroups = new List<PassengerSpawnData>();
        foreach (var p in receivedPassengerData)
        {
            levelData.initialPassengerGroups.Add(new PassengerSpawnData
            {
                position = p.gridPosition,
                color = p.color,
                direction = p.direction
            });
        }
        
        // Wagon data'sını ekle
        levelData.wagons = new List<WagonSpawnData>();
        foreach (var color in wagonSequence)
        {
            levelData.wagons.Add(new WagonSpawnData(color, 4));
        }
        
        // Asset olarak kaydet
        string path = $"Assets/Resources/Levels/{levelName}_Level{levelIndex}.asset";
        
        // Klasör yoksa oluştur
        if (!System.IO.Directory.Exists("Assets/Resources/Levels"))
        {
            System.IO.Directory.CreateDirectory("Assets/Resources/Levels");
        }
        
        AssetDatabase.CreateAsset(levelData, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = levelData;
        
        Debug.Log($"✅ LevelSpawnSO oluşturuldu: {path}");
        Debug.Log($"📊 {receivedPassengerData.Count} passenger, {wagonSequence.Count} wagon");
        
        EditorUtility.DisplayDialog("Başarılı!", $"LevelSpawnSO oluşturuldu!\n\n{path}\n\n{receivedPassengerData.Count} passenger\n{wagonSequence.Count} wagon", "Tamam");
    }

    private List<HyperCasualColor> CalculateWagonSequence()
    {
        Dictionary<HyperCasualColor, int> first20Counts = new Dictionary<HyperCasualColor, int>();
        Dictionary<HyperCasualColor, int> totalCounts = new Dictionary<HyperCasualColor, int>();
        
        foreach (var colorData in colorDataList)
        {
            int first20Count = colorData.isChallenge
                ? Mathf.Max(1, Mathf.RoundToInt(colorData.passengerCount * challengeRatio))
                : Mathf.Min(colorData.passengerCount, Mathf.RoundToInt(colorData.passengerCount * 0.8f));
            
            first20Counts[colorData.color] = first20Count;
            totalCounts[colorData.color] = colorData.passengerCount;
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
            case HyperCasualColor.Lime: return new Color(0.5f, 1f, 0f);
            case HyperCasualColor.White: return Color.white;
            default: return Color.white;
        }
    }
}
