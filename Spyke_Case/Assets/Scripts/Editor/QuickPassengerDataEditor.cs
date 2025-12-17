using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using GridSystem;

/// <summary>
/// Hızlı passenger data girişi tool'u
/// Klavye kısayolları ile passenger bilgilerini gir, sahneye spawn etme!
/// </summary>
public class QuickPassengerDataEditor : EditorWindow
{
    [System.Serializable]
    public class PassengerData
    {
        public Vector2Int gridPosition;
        public HyperCasualColor color;
        public Vector2Int direction;
        public int groupSize;
    }

    private enum InputMode
    {
        Custom,      // Tek tek gir
        Rectangle    // Dikdörtgen şeklinde gir
    }

    // Mode
    private InputMode currentMode = InputMode.Custom;
    
    // Rectangle mode settings
    private int rectangleWidth = 3;
    private int rectangleHeight = 3;
    private Vector2Int rectangleOffset = Vector2Int.zero; // Ortalama için offset (arka plan)
    
    // Current passenger settings (kullanıcı perspektifi: 0-based)
    private Vector2Int currentGridPos = Vector2Int.zero; // Kullanıcının gördüğü pozisyon (0,0'dan başlar)
    private Vector2Int currentDirection = new Vector2Int(1, 0);
    private HyperCasualColor currentColor = HyperCasualColor.Blue;
    private int currentGroupSize = 4;
    
    // Passenger data list
    private List<PassengerData> passengerList = new List<PassengerData>();
    
    // UI
    private Vector2 scrollPosition;
    private List<string> inputLog = new List<string>();
    private bool autoAdvance = true;
    
    // Color shortcuts
    private Dictionary<KeyCode, HyperCasualColor> colorShortcuts = new Dictionary<KeyCode, HyperCasualColor>
    {
        { KeyCode.B, HyperCasualColor.Blue },
        { KeyCode.R, HyperCasualColor.Red },
        { KeyCode.G, HyperCasualColor.Green },
        { KeyCode.Y, HyperCasualColor.Yellow },
        { KeyCode.O, HyperCasualColor.Orange },
        { KeyCode.P, HyperCasualColor.Purple },
        { KeyCode.I, HyperCasualColor.Pink },
        { KeyCode.C, HyperCasualColor.Cyan },
        { KeyCode.L, HyperCasualColor.Lime },
        { KeyCode.W, HyperCasualColor.White }
    };

    [MenuItem("Tools/Quick Passenger Data Input")]
    public static void ShowWindow()
    {
        var window = GetWindow<QuickPassengerDataEditor>("Passenger Data Input");
        window.minSize = new Vector2(500, 750);
        window.Show();
    }

    private void OnEnable()
    {
        inputLog.Clear();
        inputLog.Add("🚀 Quick Passenger Data Input başlatıldı!");
        inputLog.Add("📝 Passenger bilgilerini gir (sahneye spawn etmez, sadece data)");
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
        EditorGUILayout.LabelField("⚡ Quick Passenger Data Input", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Klavye kısayolları ile passenger bilgilerini gir!\n" +
                               "Ok tuşları: Yön | Renk tuşları: Y,B,R,G,O,P,I,C,L,W\n" +
                               "Space: Data ekle | Enter: Sonraki pozisyon", MessageType.Info);
        GUILayout.Space(10);

        // Mode Selection
        DrawModeSelection();
        GUILayout.Space(10);

        // Current Settings
        DrawCurrentSettings();
        GUILayout.Space(10);

        // Passenger List
        DrawPassengerList();
        GUILayout.Space(10);

        // Input Log
        DrawInputLog();
        GUILayout.Space(10);

        // Action Buttons
        DrawActionButtons();

        EditorGUILayout.EndScrollView();
    }

    private void DrawModeSelection()
    {
        EditorGUILayout.LabelField("Mod Seçimi", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        
        currentMode = (InputMode)EditorGUILayout.EnumPopup("Input Mode", currentMode);
        
        if (currentMode == InputMode.Rectangle)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Dikdörtgen Ayarları (Otomatik Ortalanır)", EditorStyles.miniLabel);
            
            rectangleWidth = EditorGUILayout.IntField("Genişlik", rectangleWidth);
            rectangleHeight = EditorGUILayout.IntField("Yükseklik", rectangleHeight);
            
            // Otomatik ortalama hesapla ve göster
            Vector2Int autoStartPos = CalculateCenteredStart(rectangleWidth, rectangleHeight);
            Vector2Int autoEndPos = new Vector2Int(autoStartPos.x + rectangleWidth - 1, autoStartPos.y + rectangleHeight - 1);
            
            EditorGUILayout.HelpBox($"Otomatik Ortalanmış Dikdörtgen:\nBaşlangıç: ({autoStartPos.x},{autoStartPos.y})\nBitiş: ({autoEndPos.x},{autoEndPos.y})\n\nÖrnek: 3x3 → 9 passenger", MessageType.Info);
            
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
        EditorGUILayout.LabelField("Matris Pozisyonu:", GUILayout.Width(120));
        GUI.color = Color.cyan;
        EditorGUILayout.LabelField($"({currentGridPos.x}, {currentGridPos.y})", EditorStyles.boldLabel);
        GUI.color = Color.white;
        if (currentMode == InputMode.Rectangle)
        {
            Vector2Int realPos = currentGridPos + rectangleOffset;
            GUI.color = Color.gray;
            EditorGUILayout.LabelField($"→ Grid: ({realPos.x},{realPos.y})", EditorStyles.miniLabel, GUILayout.Width(100));
            GUI.color = Color.white;
        }
        EditorGUILayout.EndHorizontal();
        
        // Direction
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Yön:", GUILayout.Width(120));
        GUI.color = Color.yellow;
        EditorGUILayout.LabelField(GetDirectionText(currentDirection), EditorStyles.boldLabel);
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
        
        autoAdvance = EditorGUILayout.Toggle("Otomatik İlerleme", autoAdvance);
        
        EditorGUILayout.EndVertical();
        
        // Keyboard shortcuts
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("⌨️ Klavye Kısayolları", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("← → ↑ ↓: Yön | Space: Data ekle | Enter: Sonraki pozisyon", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("Y:Yellow B:Blue R:Red G:Green O:Orange P:Purple I:Pink C:Cyan L:Lime W:White", EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();
    }

    private void DrawPassengerList()
    {
        EditorGUILayout.LabelField($"📋 Passenger Listesi ({passengerList.Count} adet)", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box", GUILayout.Height(200));
        
        Vector2 listScroll = EditorGUILayout.BeginScrollView(Vector2.zero, GUILayout.Height(190));
        
        for (int i = 0; i < passengerList.Count; i++)
        {
            var p = passengerList[i];
            GUI.color = GetColorForEnum(p.color);
            EditorGUILayout.LabelField($"{i+1}. ({p.gridPosition.x},{p.gridPosition.y}) {p.color} {GetDirectionText(p.direction)} Size:{p.groupSize}", EditorStyles.miniLabel);
            GUI.color = Color.white;
        }
        
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawInputLog()
    {
        EditorGUILayout.LabelField("📝 Input Log", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box", GUILayout.Height(120));
        
        Vector2 logScroll = EditorGUILayout.BeginScrollView(Vector2.zero, GUILayout.Height(110));
        
        for (int i = Mathf.Max(0, inputLog.Count - 15); i < inputLog.Count; i++)
        {
            EditorGUILayout.LabelField(inputLog[i], EditorStyles.miniLabel);
        }
        
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawActionButtons()
    {
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("🎯 Data Ekle (Space)", GUILayout.Height(40)))
        {
            AddPassengerData();
        }
        
        if (GUILayout.Button("➡️ Sonraki Pozisyon (Enter)", GUILayout.Height(40)))
        {
            AdvancePosition();
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("📤 Listeyi Semi-Auto'ya Gönder", GUILayout.Height(40)))
        {
            SendToSemiAuto();
        }
        
        if (GUILayout.Button("🗑️ Listeyi Temizle", GUILayout.Height(30)))
        {
            passengerList.Clear();
            AddLog("🧹 Passenger listesi temizlendi");
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
        // Add data
        else if (keyCode == KeyCode.Space)
        {
            AddPassengerData();
        }
        // Advance
        else if (keyCode == KeyCode.Return || keyCode == KeyCode.KeypadEnter)
        {
            AdvancePosition();
        }
    }

    private void StartRectangleMode()
    {
        // Kullanıcı 0,0'dan başlar (matris gibi)
        currentGridPos = Vector2Int.zero;
        currentDirection = new Vector2Int(1, 0);
        
        // Arka planda ortalama için offset hesapla
        rectangleOffset = CalculateCenteredStart(rectangleWidth, rectangleHeight);
        
        Vector2Int realStart = rectangleOffset;
        Vector2Int realEnd = new Vector2Int(realStart.x + rectangleWidth - 1, realStart.y + rectangleHeight - 1);
        
        AddLog($"📐 Dikdörtgen mod başlatıldı: {rectangleWidth}x{rectangleHeight}");
        AddLog($"📍 Matris: (0,0) → ({rectangleWidth-1},{rectangleHeight-1})");
        AddLog($"📍 Grid (arka plan): ({realStart.x},{realStart.y}) → ({realEnd.x},{realEnd.y})");
        AddLog($"💡 Şimdi (0,0)'dan başlayarak passenger gir!");
    }

    /// <summary>
    /// Dikdörtgeni 7x11 grid'de ortalar
    /// Kullanılabilir alan: X: 1-5 (5 genişlik), Y: 1-9 (9 yükseklik)
    /// Örnek: 5 genişlik → X: 1,2,3,4,5 (1'den başla, tam sığar)
    /// Örnek: 6 yükseklik → Y: 2,3,4,5,6,7 (ortada)
    /// </summary>
    private Vector2Int CalculateCenteredStart(int width, int height)
    {
        // Grid boyutu 7x11 (GridData'dan)
        // Kullanılabilir alan: X: 1-5 (5 genişlik), Y: 1-9 (9 yükseklik)
        const int USABLE_WIDTH = 5;   // X: 1,2,3,4,5
        const int USABLE_HEIGHT = 9;  // Y: 1,2,3,4,5,6,7,8,9
        const int START_X = 1;        // X her zaman 1'den başlar
        const int START_Y = 1;        // Y 1'den başlar
        
        // X için: Her zaman 1'den başla (maksimum 5 genişlik)
        int startX = START_X;
        
        // Y için: Ortalama (9 yükseklik içinde)
        // Örnek: 6 yükseklik → (9 - 6) / 2 = 1.5 ≈ 1 → 1 + 1 = 2'den başla
        int startY = START_Y + (USABLE_HEIGHT - height) / 2;
        
        return new Vector2Int(startX, startY);
    }

    private void AddPassengerData()
    {
        // Gerçek grid pozisyonunu hesapla (kullanıcı pozisyonu + offset)
        Vector2Int realGridPos = currentMode == InputMode.Rectangle 
            ? currentGridPos + rectangleOffset 
            : currentGridPos;
        
        var data = new PassengerData
        {
            gridPosition = realGridPos, // Gerçek grid pozisyonu
            color = currentColor,
            direction = currentDirection,
            groupSize = currentGroupSize
        };
        
        passengerList.Add(data);
        
        if (currentMode == InputMode.Rectangle)
        {
            AddLog($"✅ Eklendi: Matris({currentGridPos.x},{currentGridPos.y}) → Grid({realGridPos.x},{realGridPos.y}) {currentColor} {GetDirectionText(currentDirection)}");
        }
        else
        {
            AddLog($"✅ Eklendi: ({currentGridPos.x},{currentGridPos.y}) {currentColor} {GetDirectionText(currentDirection)} Size:{currentGroupSize}");
        }
        
        if (autoAdvance)
        {
            AdvancePosition();
        }
    }

    private void AdvancePosition()
    {
        if (currentMode == InputMode.Rectangle)
        {
            // Dikdörtgen modda: Sadece X yönünde ilerle (sağa doğru)
            // Direction vektörünü kullanma, her zaman sağa git
            currentGridPos.x++;
            
            // Satır sonu kontrolü (matris sınırları: 0 → width-1)
            if (currentGridPos.x >= rectangleWidth)
            {
                // Satır sonu, bir alt satıra geç
                currentGridPos.x = 0;
                currentGridPos.y++;
                AddLog($"📍 Yeni satır: Matris({currentGridPos.x},{currentGridPos.y})");
            }
            
            // Dikdörtgen tamamlandı mı?
            if (currentGridPos.y >= rectangleHeight)
            {
                AddLog("🏁 Dikdörtgen tamamlandı!");
                currentGridPos = Vector2Int.zero; // 0,0'a dön
            }
        }
        else
        {
            // Custom modda: Direction vektörüne göre ilerle
            currentGridPos += currentDirection;
            AddLog($"📍 Pozisyon: ({currentGridPos.x},{currentGridPos.y})");
        }
    }

    private void SendToSemiAuto()
    {
        if (passengerList.Count == 0)
        {
            EditorUtility.DisplayDialog("Hata", "Passenger listesi boş! Önce passenger data'sı ekle.", "Tamam");
            return;
        }
        
        // Semi-Auto Level Spawner'ı aç ve listeyi gönder
        var semiAutoWindow = GetWindow<SemiAutoLevelDataCreator>("Level Data Creator");
        semiAutoWindow.SetPassengerData(passengerList);
        semiAutoWindow.Show();
        
        AddLog($"📤 {passengerList.Count} passenger Semi-Auto'ya gönderildi!");
    }

    private void AddLog(string message)
    {
        inputLog.Add(message);
        if (inputLog.Count > 100) inputLog.RemoveAt(0);
    }

    private string GetDirectionText(Vector2Int dir)
    {
        if (dir == new Vector2Int(1, 0)) return "→";
        if (dir == new Vector2Int(-1, 0)) return "←";
        if (dir == new Vector2Int(0, 1)) return "↑";
        if (dir == new Vector2Int(0, -1)) return "↓";
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
    
    // Public method for Semi-Auto to access passenger data
    public List<PassengerData> GetPassengerData()
    {
        return new List<PassengerData>(passengerList);
    }
}
