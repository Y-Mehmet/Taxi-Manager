using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Mevcut LevelSpawnSO'ya wagon ekleyen basit editor
/// Passenger'lar zaten var, sadece wagon'ları otomatik oluştur
/// </summary>
public class WagonAdderEditor : EditorWindow
{
    [System.Serializable]
    private class ColorData
    {
        public HyperCasualColor color;
        public int passengerCount;
        public bool isChallenge;
    }

    // Selected LevelSpawnSO
    private LevelSpawnSO targetLevelData;
    
    // Color analysis
    private List<ColorData> colorDataList = new List<ColorData>();
    
    // Settings
    private int challengeWagonCount = 20;
    private float challengeRatio = 0.4f;
    
    // UI
    private Vector2 scrollPosition;

    [MenuItem("Tools/Wagon Adder (Add Wagons to Existing Level)")]
    public static void ShowWindow()
    {
        var window = GetWindow<WagonAdderEditor>("Wagon Adder");
        window.minSize = new Vector2(450, 650);
        window.Show();
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // Header
        GUILayout.Space(10);
        EditorGUILayout.LabelField("🚂 Wagon Adder", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Mevcut LevelSpawnSO'ya wagon ekle!\n" +
                               "1. LevelSpawnSO seç (passenger'lar zaten var)\n" +
                               "2. Passenger'ları analiz et\n" +
                               "3. Challenge color'ları işaretle\n" +
                               "4. Wagon'ları ekle!", MessageType.Info);
        GUILayout.Space(10);

        // Level Selection
        DrawLevelSelection();
        GUILayout.Space(10);

        // Passenger Summary
        DrawPassengerSummary();
        GUILayout.Space(10);

        // Color Analysis
        DrawColorAnalysis();
        GUILayout.Space(10);

        // Wagon Preview
        DrawWagonPreview();
        GUILayout.Space(10);

        // Action Buttons
        DrawActionButtons();

        EditorGUILayout.EndScrollView();
    }

    private void DrawLevelSelection()
    {
        EditorGUILayout.LabelField("📁 Level Seçimi", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        
        LevelSpawnSO newSelection = (LevelSpawnSO)EditorGUILayout.ObjectField(
            "LevelSpawnSO", 
            targetLevelData, 
            typeof(LevelSpawnSO), 
            false
        );
        
        if (newSelection != targetLevelData)
        {
            targetLevelData = newSelection;
            if (targetLevelData != null)
            {
                AnalyzePassengers();
            }
        }
        
        if (targetLevelData == null)
        {
            EditorGUILayout.HelpBox("Lütfen bir LevelSpawnSO seç!", MessageType.Warning);
        }
        else
        {
            EditorGUILayout.LabelField($"Seçili: {targetLevelData.name}", EditorStyles.miniLabel);
        }
        
        EditorGUILayout.EndVertical();
    }

    private void DrawPassengerSummary()
    {
        EditorGUILayout.LabelField("📊 Araba Özeti (PassengerGroup)", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        
        if (targetLevelData == null)
        {
            EditorGUILayout.HelpBox("LevelSpawnSO seçilmedi.", MessageType.Info);
        }
        else if (targetLevelData.initialPassengerGroups == null || targetLevelData.initialPassengerGroups.Count == 0)
        {
            EditorGUILayout.HelpBox("Bu level'da araba (PassengerGroup) yok!", MessageType.Warning);
        }
        else
        {
            const int PASSENGERS_PER_CAR = 4;
            int totalCars = targetLevelData.initialPassengerGroups.Count;
            int totalWagons = totalCars * PASSENGERS_PER_CAR;
            
            EditorGUILayout.LabelField($"Toplam: {totalCars} araba → {totalWagons} wagon gerekli", EditorStyles.miniLabel);
            
            var colorGroups = targetLevelData.initialPassengerGroups.GroupBy(p => p.color).OrderByDescending(g => g.Count());
            foreach (var group in colorGroups)
            {
                int carCount = group.Count();
                int wagonCount = carCount * PASSENGERS_PER_CAR;
                
                GUI.color = GetColorForEnum(group.Key);
                EditorGUILayout.LabelField($"  {group.Key}: {carCount} araba → {wagonCount} wagon", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }
        }
        
        EditorGUILayout.EndVertical();
    }

    private void DrawColorAnalysis()
    {
        EditorGUILayout.LabelField("🎨 Renk Dağılımı", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        
        if (colorDataList.Count == 0)
        {
            EditorGUILayout.HelpBox("Araba'lar analiz edilmedi.", MessageType.Info);
        }
        else
        {
            // Header
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Renk", GUILayout.Width(100));
            EditorGUILayout.LabelField("Wagon Sayısı", GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // Color rows
            foreach (var colorData in colorDataList)
            {
                EditorGUILayout.BeginHorizontal();
                
                GUI.color = GetColorForEnum(colorData.color);
                EditorGUILayout.LabelField(colorData.color.ToString(), GUILayout.Width(100));
                GUI.color = Color.white;
                
                EditorGUILayout.LabelField(colorData.passengerCount.ToString(), GUILayout.Width(100));
                
                EditorGUILayout.EndHorizontal();
            }
        }
        
        EditorGUILayout.EndVertical();
    }

    private void DrawSettings()
    {
        EditorGUILayout.LabelField("⚙️ Wagon Ayarları", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        
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
            EditorGUILayout.HelpBox("Önizleme için araba analizi gerekli.", MessageType.Info);
        }
        else
        {
            foreach (var colorData in colorDataList)
            {
                // Basit: Her renk için passengerCount kadar wagon
                int wagonCount = colorData.passengerCount;
                
                GUI.color = GetColorForEnum(colorData.color);
                EditorGUILayout.LabelField($"{colorData.color}: {wagonCount} wagon");
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
        
        GUI.enabled = targetLevelData != null;
        if (GUILayout.Button("🔍 Passenger'ları Analiz Et", GUILayout.Height(40)))
        {
            AnalyzePassengers();
        }
        GUI.enabled = true;
        
        GUI.enabled = colorDataList.Count > 0;
        if (GUILayout.Button("🚂 Wagon'ları Ekle", GUILayout.Height(40)))
        {
            AddWagons();
        }
        GUI.enabled = true;
        
        EditorGUILayout.EndHorizontal();
    }

    private void AnalyzePassengers()
    {
        if (targetLevelData == null)
        {
            EditorUtility.DisplayDialog("Hata", "LevelSpawnSO seçilmedi!", "Tamam");
            return;
        }
        
        if (targetLevelData.initialPassengerGroups == null || targetLevelData.initialPassengerGroups.Count == 0)
        {
            EditorUtility.DisplayDialog("Hata", "Bu level'da passenger yok!", "Tamam");
            return;
        }
        
        colorDataList.Clear();
        
        // PassengerGroup = Araba (car)
        // Her araba 4 yolcu taşır
        // Wagon = Yolcu
        // Yani: Her PassengerGroup için 4 Wagon gerekli
        const int PASSENGERS_PER_CAR = 4;
        
        var colorGroups = targetLevelData.initialPassengerGroups.GroupBy(p => p.color);
        foreach (var group in colorGroups)
        {
            int carCount = group.Count(); // Araba sayısı
            int wagonCount = carCount * PASSENGERS_PER_CAR; // Wagon (yolcu) sayısı
            
            colorDataList.Add(new ColorData
            {
                color = group.Key,
                passengerCount = wagonCount, // Wagon sayısı (yolcu sayısı)
                isChallenge = false
            });
        }
        
        colorDataList = colorDataList.OrderByDescending(c => c.passengerCount).ToList();
        
        int totalCars = targetLevelData.initialPassengerGroups.Count;
        int totalWagons = colorDataList.Sum(c => c.passengerCount);
        
        Debug.Log($"✅ {totalCars} araba (PassengerGroup) analiz edildi → {totalWagons} wagon (yolcu) gerekli, {colorDataList.Count} farklı renk bulundu.");
    }

    private void AddWagons()
    {
        if (targetLevelData == null)
        {
            EditorUtility.DisplayDialog("Hata", "LevelSpawnSO seçilmedi!", "Tamam");
            return;
        }
        
        if (colorDataList.Count == 0)
        {
            EditorUtility.DisplayDialog("Hata", "Önce araba'ları analiz et!", "Tamam");
            return;
        }
        
        // Wagon sequence hesapla (basit - challenge ratio yok)
        List<HyperCasualColor> wagonSequence = CalculateWagonSequence();
        
        // Mevcut wagon'ları temizle ve yenilerini ekle
        targetLevelData.wagons = new List<WagonSpawnData>();
        foreach (var color in wagonSequence)
        {
            // Capacity = 1 (her wagon 1 yolcu)
            targetLevelData.wagons.Add(new WagonSpawnData(color, 1));
        }
        
        // Asset'i kaydet
        EditorUtility.SetDirty(targetLevelData);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"✅ {wagonSequence.Count} wagon eklendi: {targetLevelData.name}");
        
        EditorUtility.DisplayDialog("Başarılı!", 
            $"Wagon'lar eklendi!\n\n{targetLevelData.name}\n\n{targetLevelData.initialPassengerGroups.Count} araba\n{wagonSequence.Count} wagon", 
            "Tamam");
    }

    private List<HyperCasualColor> CalculateWagonSequence()
    {
        // Basit hesaplama: Her renk için passengerCount kadar wagon
        // Challenge ratio yok, dağıtılmış sıralama var
        
        Dictionary<HyperCasualColor, int> wagonCounts = new Dictionary<HyperCasualColor, int>();
        
        foreach (var colorData in colorDataList)
        {
            wagonCounts[colorData.color] = colorData.passengerCount;
            Debug.Log($"📊 {colorData.color}: {colorData.passengerCount} wagon gerekli");
        }
        
        int totalExpected = wagonCounts.Values.Sum();
        Debug.Log($"📊 Toplam beklenen wagon: {totalExpected}");
        
        // Dağıtılmış sıralama ile wagon listesi oluştur
        List<HyperCasualColor> sequence = CreateDistributedSequence(wagonCounts, int.MaxValue);
        
        Debug.Log($"📊 Oluşturulan wagon: {sequence.Count}");
        
        return sequence;
    }

    private List<HyperCasualColor> CreateDistributedSequence(Dictionary<HyperCasualColor, int> counts, int maxCount)
    {
        List<HyperCasualColor> sequence = new List<HyperCasualColor>();
        Dictionary<HyperCasualColor, int> remaining = new Dictionary<HyperCasualColor, int>(counts);
        
        int totalWagons = Mathf.Min(counts.Values.Sum(), maxCount);
        
        while (sequence.Count < totalWagons && remaining.Values.Any(v => v > 0))
        {
            // En fazla kalan wagon'a sahip rengi bul
            var mostRemaining = remaining.Where(kvp => kvp.Value > 0)
                                        .OrderByDescending(kvp => kvp.Value)
                                        .FirstOrDefault();
            
            // Eğer hiç kalan wagon yoksa dur
            if (mostRemaining.Value == 0) break;
            
            // Bir wagon ekle
            sequence.Add(mostRemaining.Key);
            remaining[mostRemaining.Key]--;
            
            // Diğer renklerden de bir tane ekle (dağıtılmış sıralama için)
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
