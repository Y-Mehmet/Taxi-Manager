using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using GridSystem;

/// <summary>
/// Hızlı passenger yerleştirme tool'u
/// Klavye kısayolları ile grid üzerinde passenger'ları kolayca yerleştir
/// </summary>
public class QuickPassengerSpawnerEditor : EditorWindow
{
    private enum SpawnMode
    {
        Custom,      // Tek tek yerleştir
        Rectangle    // Dikdörtgen şeklinde yerleştir
    }

    // Mode
    private SpawnMode currentMode = SpawnMode.Custom;
    
    // Rectangle mode settings
    private int rectangleWidth = 3;
    private int rectangleHeight = 4;
    // Otomatik ortalama - kullanıcı sadece boyut girer
    
    // Current passenger settings
    private Vector2Int currentGridPos = Vector2Int.zero;
    private Vector2Int currentDirection = new Vector2Int(1, 0); // Sağa doğru
    private HyperCasualColor currentColor = HyperCasualColor.Blue;
    private int currentGroupSize = 4;
    
    // References
    private PassengerGrid passengerGrid;
    private GameObject passengerPrefab;
    private Transform passengerContainer;
    
    // UI
    private Vector2 scrollPosition;
    private List<string> spawnLog = new List<string>();
    private bool autoAdvance = true; // Otomatik ilerleme
    
    // Color shortcuts
    private Dictionary<KeyCode, HyperCasualColor> colorShortcuts = new Dictionary<KeyCode, HyperCasualColor>
    {
        { KeyCode.B, HyperCasualColor.Blue },
        { KeyCode.R, HyperCasualColor.Red },
        { KeyCode.G, HyperCasualColor.Green },
        { KeyCode.Y, HyperCasualColor.Yellow },
        { KeyCode.O, HyperCasualColor.Orange },
        { KeyCode.P, HyperCasualColor.Purple },
        { KeyCode.I, HyperCasualColor.Pink },     // I for pInk
        { KeyCode.C, HyperCasualColor.Cyan },
        { KeyCode.L, HyperCasualColor.Lime },
        { KeyCode.W, HyperCasualColor.White }
    };

    [MenuItem("Tools/Quick Passenger Spawner")]
    public static void ShowWindow()
    {
        var window = GetWindow<QuickPassengerSpawnerEditor>("Quick Passenger Spawner");
        window.minSize = new Vector2(450, 700);
        window.Show();
    }

    private void OnEnable()
    {
        // Sahneden referansları bul
        if (passengerGrid == null)
        {
            passengerGrid = FindObjectOfType<PassengerGrid>();
        }
        
        if (passengerContainer == null)
        {
            var container = GameObject.Find("PassengerContainer");
            if (container != null) passengerContainer = container.transform;
        }
        
        spawnLog.Clear();
        spawnLog.Add("🚀 Quick Passenger Spawner başlatıldı!");
        spawnLog.Add("📝 Mod seç ve passenger yerleştirmeye başla!");
    }

    private void OnGUI()
    {
        // Klavye event'lerini yakala
        Event e = Event.current;
        if (e.type == EventType.KeyDown)
        {
            HandleKeyPress(e.keyCode);
            Repaint();
        }

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // Header
        GUILayout.Space(10);
        EditorGUILayout.LabelField("⚡ Quick Passenger Spawner", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Klavye kısayolları ile hızlı passenger yerleştir!\n" +
                               "Ok tuşları: Yön değiştir | Renk tuşları: Y,B,R,G,O,P,I,C,L,W\n" +
                               "Space: Passenger spawn et | Enter: Bir sonraki pozisyona geç", MessageType.Info);
        GUILayout.Space(10);

        // References
        DrawReferencesSection();
        GUILayout.Space(10);

        // Mode Selection
        DrawModeSelection();
        GUILayout.Space(10);

        // Current Settings
        DrawCurrentSettings();
        GUILayout.Space(10);

        // Spawn Log
        DrawSpawnLog();
        GUILayout.Space(10);

        // Action Buttons
        DrawActionButtons();

        EditorGUILayout.EndScrollView();
    }

    private void DrawReferencesSection()
    {
        EditorGUILayout.LabelField("Referanslar", EditorStyles.boldLabel);
        
        passengerGrid = (PassengerGrid)EditorGUILayout.ObjectField("Passenger Grid", passengerGrid, typeof(PassengerGrid), true);
        passengerPrefab = (GameObject)EditorGUILayout.ObjectField("Passenger Prefab", passengerPrefab, typeof(GameObject), false);
        passengerContainer = (Transform)EditorGUILayout.ObjectField("Passenger Container", passengerContainer, typeof(Transform), true);

        if (passengerGrid == null || passengerPrefab == null || passengerContainer == null)
        {
            EditorGUILayout.HelpBox("Lütfen tüm referansları ata!", MessageType.Warning);
        }
    }

    private void DrawModeSelection()
    {
        EditorGUILayout.LabelField("Mod Seçimi", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        
        currentMode = (SpawnMode)EditorGUILayout.EnumPopup("Spawn Mode", currentMode);
        
        if (currentMode == SpawnMode.Rectangle)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Dikdörtgen Ayarları (Otomatik Ortalanır)", EditorStyles.miniLabel);
            
            rectangleWidth = EditorGUILayout.IntField("Genişlik", rectangleWidth);
            rectangleHeight = EditorGUILayout.IntField("Yükseklik", rectangleHeight);
            
            // Otomatik ortalama hesapla ve göster
            Vector2Int autoStartPos = CalculateCenteredStart(rectangleWidth, rectangleHeight);
            Vector2Int autoEndPos = new Vector2Int(autoStartPos.x + rectangleWidth - 1, autoStartPos.y + rectangleHeight - 1);
            
            EditorGUILayout.HelpBox($"Otomatik Ortalanmış Dikdörtgen:\nBaşlangıç: ({autoStartPos.x},{autoStartPos.y})\nBitiş: ({autoEndPos.x},{autoEndPos.y})\n\nÖrnek: 3x4 → X:[2,3,4] Y:[3,4,5,6]", MessageType.Info);
            
            if (GUILayout.Button("Dikdörtgen Modunu Başlat"))
            {
                StartRectangleMode();
            }
        }
        
        EditorGUILayout.EndVertical();
    }

    private void DrawCurrentSettings()
    {
        EditorGUILayout.LabelField("Mevcut Ayarlar", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        
        // Grid Position
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Grid Pozisyonu:", GUILayout.Width(120));
        GUI.color = Color.cyan;
        EditorGUILayout.LabelField($"({currentGridPos.x}, {currentGridPos.y})", EditorStyles.boldLabel);
        GUI.color = Color.white;
        EditorGUILayout.EndHorizontal();
        
        // Direction
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Yön:", GUILayout.Width(120));
        GUI.color = Color.yellow;
        string directionText = GetDirectionText(currentDirection);
        EditorGUILayout.LabelField(directionText, EditorStyles.boldLabel);
        GUI.color = Color.white;
        EditorGUILayout.EndHorizontal();
        
        // Color
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Renk:", GUILayout.Width(120));
        GUI.color = GetColorForEnum(currentColor);
        EditorGUILayout.LabelField(currentColor.ToString(), EditorStyles.boldLabel);
        GUI.color = Color.white;
        EditorGUILayout.EndHorizontal();
        
        // Group Size
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Grup Boyutu:", GUILayout.Width(120));
        currentGroupSize = EditorGUILayout.IntSlider(currentGroupSize, 1, 10);
        EditorGUILayout.EndHorizontal();
        
        // Auto Advance
        autoAdvance = EditorGUILayout.Toggle("Otomatik İlerleme", autoAdvance);
        
        EditorGUILayout.EndVertical();
        
        // Keyboard shortcuts reference
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("⌨️ Klavye Kısayolları", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("← → ↑ ↓: Yön değiştir", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("Space: Passenger spawn et", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("Enter: Bir sonraki pozisyona geç", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("Y:Yellow B:Blue R:Red G:Green O:Orange P:Purple I:Pink C:Cyan L:Lime W:White", EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();
    }

    private void DrawSpawnLog()
    {
        EditorGUILayout.LabelField("📋 Spawn Log", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box", GUILayout.Height(150));
        
        Vector2 logScroll = EditorGUILayout.BeginScrollView(Vector2.zero, GUILayout.Height(140));
        
        for (int i = Mathf.Max(0, spawnLog.Count - 20); i < spawnLog.Count; i++)
        {
            EditorGUILayout.LabelField(spawnLog[i], EditorStyles.miniLabel);
        }
        
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawActionButtons()
    {
        EditorGUILayout.BeginHorizontal();
        
        GUI.enabled = CanSpawn();
        if (GUILayout.Button("🎯 Spawn Passenger (Space)", GUILayout.Height(40)))
        {
            SpawnPassenger();
        }
        GUI.enabled = true;
        
        if (GUILayout.Button("➡️ Sonraki Pozisyon (Enter)", GUILayout.Height(40)))
        {
            AdvancePosition();
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("🔄 Pozisyonu Sıfırla", GUILayout.Height(30)))
        {
            currentGridPos = Vector2Int.zero;
            AddLog("📍 Pozisyon sıfırlandı: (0,0)");
        }
        
        if (GUILayout.Button("🗑️ Log'u Temizle", GUILayout.Height(30)))
        {
            spawnLog.Clear();
            AddLog("🧹 Log temizlendi");
        }
        
        EditorGUILayout.EndHorizontal();
    }

    private void HandleKeyPress(KeyCode keyCode)
    {
        // Direction keys
        if (keyCode == KeyCode.LeftArrow)
        {
            currentDirection = new Vector2Int(-1, 0);
            AddLog("⬅️ Yön: Sol");
        }
        else if (keyCode == KeyCode.RightArrow)
        {
            currentDirection = new Vector2Int(1, 0);
            AddLog("➡️ Yön: Sağ");
        }
        else if (keyCode == KeyCode.UpArrow)
        {
            currentDirection = new Vector2Int(0, 1);
            AddLog("⬆️ Yön: Yukarı");
        }
        else if (keyCode == KeyCode.DownArrow)
        {
            currentDirection = new Vector2Int(0, -1);
            AddLog("⬇️ Yön: Aşağı");
        }
        // Color shortcuts
        else if (colorShortcuts.ContainsKey(keyCode))
        {
            currentColor = colorShortcuts[keyCode];
            AddLog($"🎨 Renk: {currentColor}");
        }
        // Spawn
        else if (keyCode == KeyCode.Space)
        {
            SpawnPassenger();
        }
        // Advance
        else if (keyCode == KeyCode.Return || keyCode == KeyCode.KeypadEnter)
        {
            AdvancePosition();
        }
    }

    private void StartRectangleMode()
    {
        // Otomatik ortalanmış başlangıç pozisyonu hesapla
        Vector2Int centeredStart = CalculateCenteredStart(rectangleWidth, rectangleHeight);
        currentGridPos = centeredStart;
        currentDirection = new Vector2Int(1, 0); // Sağa doğru başla
        
        Vector2Int endPos = new Vector2Int(centeredStart.x + rectangleWidth - 1, centeredStart.y + rectangleHeight - 1);
        AddLog($"📐 Dikdörtgen mod başlatıldı: {rectangleWidth}x{rectangleHeight}");
        AddLog($"📍 Otomatik ortalanmış alan: ({centeredStart.x},{centeredStart.y}) → ({endPos.x},{endPos.y})");
        AddLog($"📍 Başlangıç pozisyonu: ({currentGridPos.x},{currentGridPos.y})");
    }
    
    /// <summary>
    /// Dikdörtgeni grid'de ortalar
    /// Örnek: 3x4 dikdörtgen için X: 2,3,4 ve Y: 3,4,5,6 kullanır
    /// </summary>
    private Vector2Int CalculateCenteredStart(int width, int height)
    {
        // Grid boyutunu varsayalım (genelde 10x10 veya benzeri)
        // Ortalama için grid merkezinden başlayıp yarı genişlik/yükseklik çıkarıyoruz
        int gridCenterX = 5; // 10x10 grid için merkez
        int gridCenterY = 5;
        
        int startX = gridCenterX - (width / 2);
        int startY = gridCenterY - (height / 2);
        
        return new Vector2Int(startX, startY);
    }

    private void SpawnPassenger()
    {
        if (!CanSpawn())
        {
            AddLog("❌ Spawn edilemedi: Referansları kontrol et!");
            return;
        }

        // Grid pozisyonunu world pozisyonuna çevir
        Vector3 worldPos = passengerGrid.GetWorldPosition(currentGridPos);
        
        // Passenger spawn et
        GameObject passengerObj = (GameObject)PrefabUtility.InstantiatePrefab(passengerPrefab, passengerContainer);
        passengerObj.transform.position = worldPos;
        passengerObj.name = $"Passenger_{currentColor}_{currentGridPos.x}_{currentGridPos.y}";
        
        PassengerGroup passenger = passengerObj.GetComponent<PassengerGroup>();
        if (passenger != null)
        {
            passenger.groupColor = currentColor;
            passenger.GroupSize = currentGroupSize;
            // direction ve gridPosition PassengerGroup'ta public property değil
            // Bu bilgiler PassengerGroup içinde otomatik ayarlanacak
        }
        
        AddLog($"✅ Spawn: {currentColor} @ ({currentGridPos.x},{currentGridPos.y}) Dir:{GetDirectionText(currentDirection)} Size:{currentGroupSize}");
        
        // Otomatik ilerleme
        if (autoAdvance)
        {
            AdvancePosition();
        }
    }

    private void AdvancePosition()
    {
        if (currentMode == SpawnMode.Rectangle)
        {
            // Dikdörtgen modunda otomatik ilerleme
            currentGridPos.x += currentDirection.x;
            currentGridPos.y += currentDirection.y;
            
            // Otomatik ortalanmış dikdörtgen için sınırları hesapla
            Vector2Int centeredStart = CalculateCenteredStart(rectangleWidth, rectangleHeight);
            
            // Sınırları kontrol et
            if (currentGridPos.x >= centeredStart.x + rectangleWidth)
            {
                // Satır sonu, bir alt satıra geç
                currentGridPos.x = centeredStart.x;
                currentGridPos.y++;
                AddLog($"📍 Yeni satır: ({currentGridPos.x},{currentGridPos.y})");
            }
            else if (currentGridPos.x < centeredStart.x)
            {
                currentGridPos.x = centeredStart.x + rectangleWidth - 1;
                currentGridPos.y++;
                AddLog($"📍 Yeni satır: ({currentGridPos.x},{currentGridPos.y})");
            }
            
            if (currentGridPos.y >= centeredStart.y + rectangleHeight)
            {
                AddLog("🏁 Dikdörtgen tamamlandı!");
                currentGridPos = centeredStart;
            }
        }
        else
        {
            // Custom modda yön vektörüne göre ilerle
            currentGridPos += currentDirection;
            AddLog($"📍 Pozisyon: ({currentGridPos.x},{currentGridPos.y})");
        }
    }

    private bool CanSpawn()
    {
        return passengerGrid != null && passengerPrefab != null && passengerContainer != null;
    }

    private void AddLog(string message)
    {
        spawnLog.Add(message);
        if (spawnLog.Count > 100) spawnLog.RemoveAt(0);
    }

    private string GetDirectionText(Vector2Int dir)
    {
        if (dir == new Vector2Int(1, 0)) return "→ Sağ";
        if (dir == new Vector2Int(-1, 0)) return "← Sol";
        if (dir == new Vector2Int(0, 1)) return "↑ Yukarı";
        if (dir == new Vector2Int(0, -1)) return "↓ Aşağı";
        return $"({dir.x},{dir.y})";
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
