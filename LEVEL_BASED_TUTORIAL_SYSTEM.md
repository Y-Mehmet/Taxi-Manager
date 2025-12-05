# Level-Based Ability Tutorial System

Bu sistem, belirli levellerde ability'leri unlock ederken tutorial gösterir.

## 🎯 Ability Unlock Levels

| Level | Ability | Tutorial |
|-------|---------|----------|
| 4 | Add Stop | ✅ İlk kez gösterilir |
| 8 | Universal Pathfinding | ✅ İlk kez gösterilir |
| 16 | Flasher (Remove Wagons) | ✅ İlk kez gösterilir |
| 32 | Shuffle Colors | ✅ İlk kez gösterilir |

## 🔄 Sistem Akışı

### İlk Kez (Level 4):
```
Level 3 Tamamlandı
  ↓
Level 4 Açıldı
  ↓
SceneManager.LoadLevelSceene()
  ↓
Check: hasSeenAddStopTutorial? → FALSE
  ↓
PlayerPrefs.SetString("CurrentAbilityTutorial", "AddStop")
  ↓
Load AbilityTutorial Scene (Build Index 2)
  ↓
Tutorial Oynatılır (Typewriter + Auto-click + Hand)
  ↓
3 Tıklama Sonrası → Auto-skip başlar (5 saniye)
  ↓
Next Button VEYA 5 Saniye Geçince
  ↓
hasSeenAddStopTutorial = TRUE → Save
  ↓
Load Level 4
```

### İkinci Kez (Level 4 Tekrar):
```
Level 4 Seçildi
  ↓
SceneManager.LoadLevelSceene()
  ↓
Check: hasSeenAddStopTutorial? → TRUE ✅
  ↓
Skip Tutorial
  ↓
Load Level 4 Directly
```

## 📊 SaveGameData Yapısı

```csharp
public class SaveGameData
{
    // Ability Tutorial Flags
    public bool hasSeenAddStopTutorial;         // Level 4
    public bool hasSeenUniversalPathfindingTutorial; // Level 8
    public bool hasSeenFlasherTutorial;         // Level 16
    public bool hasSeenShuffleTutorial;         // Level 32
}
```

## 🎬 AbilityTutorialManager

### Özellikler:
- **Current Ability Type**: Hangi ability tutorial'ı (Inspector'dan ayarla)
- **Auto-Skip**: 5 saniye sonra otomatik geçiş
- **Next Button**: Manuel geçiş
- **Skip Button**: Atla (opsiyonel)
- **Timer Text**: Geri sayım göstergesi

### Timeline:
```
0s:  Tutorial başlar
     → Typewriter effect
     → Hand animation
     → Auto-click sequence (3x)
     
6s:  3 tıklama tamamlandı
     → OnTutorialCompleted() çağrılır
     → Auto-skip countdown başlar
     
7s:  Timer: "Devam ediliyor... 5"
8s:  Timer: "Devam ediliyor... 4"
9s:  Timer: "Devam ediliyor... 3"
10s: Timer: "Devam ediliyor... 2"
11s: Timer: "Devam ediliyor... 1"
12s: Auto-skip → Level yüklenir

VEYA

7s:  Kullanıcı Next Button'a tıklar
     → Auto-skip iptal
     → Hemen level yüklenir
```

## 📋 Unity Setup

### 1. AbilityTutorial Scene (Build Index 2)

#### Hierarchy:
```
AbilityTutorial
├── Canvas
│   ├── Panel_AddStop
│   │   ├── TutorialManager + AbilityTutorialManager
│   │   │   - Current Ability Type: AddNewStop
│   │   │   - Auto Skip Delay: 5
│   │   │   - Next Button: NextButton
│   │   │   - Timer Text: TimerText
│   │   ├── DescriptionText + TypewriterEffect
│   │   ├── StopContainer
│   │   ├── CostText
│   │   ├── HandImage
│   │   └── AddStopButton + AbilityTutorialButton
│   │       - Tutorial Manager: TutorialManager
│   ├── Panel_UniversalPathfinding
│   │   └── ... (aynı yapı)
│   ├── Panel_Flasher
│   │   └── ... (aynı yapı)
│   ├── Panel_Shuffle
│   │   └── ... (aynı yapı)
│   ├── NextButton (Button)
│   └── TimerText (TextMeshPro)
```

### 2. SceneManager Ayarları

```csharp
[SerializeField] private int abilityTutorialBuildIndex = 2;
```

## 🎯 Kullanım Örnekleri

### Örnek 1: Level 4 İlk Kez
```
Player Level 3'ü tamamladı
  ↓
MaxOpenedLevel = 4
  ↓
Play Button → LoadLevelSceene()
  ↓
currentLevel = 4
hasSeenAddStopTutorial = false
  ↓
Tutorial gösterilir
  ↓
Tutorial tamamlanır
  ↓
hasSeenAddStopTutorial = true
  ↓
Level 4 yüklenir
```

### Örnek 2: Level 4 Tekrar
```
Player Level 4'ü seçti
  ↓
Play Button → LoadLevelSceene()
  ↓
currentLevel = 4
hasSeenAddStopTutorial = true ✅
  ↓
Tutorial atlanır
  ↓
Level 4 direkt yüklenir
```

### Örnek 3: Level 8 İlk Kez
```
Player Level 7'yi tamamladı
  ↓
MaxOpenedLevel = 8
  ↓
Play Button → LoadLevelSceene()
  ↓
currentLevel = 8
hasSeenUniversalPathfindingTutorial = false
  ↓
Universal Pathfinding Tutorial gösterilir
  ↓
Tutorial tamamlanır
  ↓
hasSeenUniversalPathfindingTutorial = true
  ↓
Level 8 yüklenir
```

## 🔧 PlayerPrefs Kullanımı

Tutorial sahnesinde hangi ability'nin gösterileceğini belirlemek için:

```csharp
// SceneManager'da set et
PlayerPrefs.SetString("CurrentAbilityTutorial", "AddStop");

// AbilityTutorial sahnesinde oku
string currentTutorial = PlayerPrefs.GetString("CurrentAbilityTutorial", "");

// İlgili paneli göster
if (currentTutorial == "AddStop")
    ShowPanel(0);
else if (currentTutorial == "UniversalPathfinding")
    ShowPanel(1);
// ...
```

## ✨ Özellikler

### 1. One-Time Show
- ✅ Her tutorial sadece bir kez gösterilir
- ✅ SaveGameData'da kalıcı olarak saklanır
- ✅ Tekrar gösterilmez

### 2. Auto-Skip
- ✅ 5 saniye sonra otomatik geçiş
- ✅ Geri sayım göstergesi
- ✅ Next button ile iptal edilebilir

### 3. Level-Based
- ✅ Belirli levellerde unlock
- ✅ Level 4, 8, 16, 32
- ✅ Otomatik kontrol

### 4. Persistent
- ✅ SaveGameData'da saklanır
- ✅ Oyun kapansa bile hatırlanır
- ✅ JSON dosyasında görülebilir

## 🐛 Sorun Giderme

### Tutorial tekrar tekrar gösteriliyor
- SaveGameData doğru kaydediliyor mu?
- hasSeenXXXTutorial flag'i true oluyor mu?
- GameDataManager.SaveGame() çağrılıyor mu?

### Tutorial hiç gösterilmiyor
- CurrentLevel doğru mu?
- SceneManager.LoadLevelSceene() çağrılıyor mu?
- abilityTutorialBuildIndex = 2 mi?

### Yanlış tutorial gösteriliyor
- PlayerPrefs.SetString doğru mu?
- AbilityTutorialManager.currentAbilityType doğru mu?

## 📊 Test Senaryoları

### Test 1: İlk Kez Level 4
1. Yeni oyun başlat
2. Level 1, 2, 3'ü tamamla
3. Level 4'ü aç
4. Play button'a tıkla
5. ✅ Add Stop Tutorial gösterilmeli
6. Tutorial tamamlan
7. ✅ Level 4 yüklenmeli

### Test 2: Level 4 Tekrar
1. Level 4'ü tekrar seç
2. Play button'a tıkla
3. ✅ Tutorial atlanmalı
4. ✅ Level 4 direkt yüklenmeli

### Test 3: Auto-Skip
1. Level 4 aç (ilk kez)
2. Tutorial başlasın
3. 3 tıklama tamamlansın
4. 5 saniye bekle
5. ✅ Otomatik level yüklenmeli

### Test 4: Next Button
1. Level 4 aç (ilk kez)
2. Tutorial başlasın
3. 3 tıklama tamamlansın
4. Next button'a tıkla
5. ✅ Hemen level yüklenmeli

Sistem hazır! 🎉
