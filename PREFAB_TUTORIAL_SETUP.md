# Prefab-Based Tutorial System - Unity Setup

Bu sistem, 4 ayrı tutorial panel prefab'ı kullanır. AbilityTutorialManager hangi level'dan geldiğini bildiği için doğru prefab'ı spawn eder.

## 🎯 Sistem Özeti

```
Level 4 → SaveData.currentAbilityTutorial = "AddStop"
  ↓
AbilityTutorialManager.Start()
  ↓
SpawnTutorialPanel("AddStop")
  ↓
Instantiate(addStopTutorialPrefab, panelContainer)
  ↓
Tutorial oynatılır
```

## 📦 Prefab Yapısı

### 1.  (Prefab)

```
AddStopTutorialPanel (Prefab)
├── TutorialManager (Empty GameObject)
│   └── AddStopTutorial (Script)
├── Title (TextMeshPro)
├── DescriptionText (TextMeshPro)
│   └── TypewriterEffect (Script)
├── StopContainer (Empty GameObject)
│   ├── StopPanel_1
│   │   ├── InactiveIndicator (Image)
│   │   ├── ActiveIndicator (Image)
│   │   ├── StopIcon (Image)
│   │   └── StopText (TextMeshPro)
│   ├── StopPanel_2 (aynı yapı)
│   ├── StopPanel_3 (aynı yapı)
│   └── StopPanel_4 (aynı yapı)
├── CostText (TextMeshPro)
├── HandImage (Image)
└── AddStopButton (Button)
    └── AbilityTutorialButton (Script)
```

### 2. UniversalPathfindingTutorialPanel (Prefab)
- Aynı yapı, farklı içerik
- Kendi tutorial logic'i

### 3. FlasherTutorialPanel (Prefab)
- Aynı yapı, farklı içerik
- Kendi tutorial logic'i

### 4. ShuffleTutorialPanel (Prefab)
- Aynı yapı, farklı içerik
- Kendi tutorial logic'i

## 🏗️ Unity Setup Adımları

### Adım 1: Prefab Oluşturma

#### AddStopTutorialPanel Prefab:

```
1. Hierarchy'de Panel_AddStop'u oluştur (yukarıdaki yapıda)
2. Tüm script'leri ve referansları ayarla
3. Project'e sürükle → Prefab oluştur
4. Hierarchy'den sil (artık gerekli değil)
```

**Script Ayarları:**
```
AddStopButton (AbilityTutorialButton):
- Tutorial Behaviour: TutorialManager
- Typewriter Effect: DescriptionText/TypewriterEffect
- Enable Auto Click: ✅ True
- Auto Click Count: 3
- Tutorial Manager: ❌ BOŞ BIRAK! (Runtime'da set edilecek)
- Click Particle Effect Prefab: ClickEffect
- Hand Image: HandImage
- Button Click Sound: ButtonClick.wav

AddStopTutorial:
- Stop Container: StopContainer
- Cost Text: CostText
- Ability Type: AddNewStop
- Ability Name: "Add Stop"
- Description: "Add a new stop..."

TypewriterEffect:
- Characters Per Second: 30
- Skip On Tap: ✅ True
```

#### Diğer Prefab'lar:
- Aynı yapıyı kullanarak diğer 3 prefab'ı oluşturun
- Her birinin kendi tutorial logic'i olacak

### Adım 2: AbilityTutorial Scene Setup

```
AbilityTutorial Scene
├── Canvas
│   ├── PanelContainer (Empty GameObject) ← Prefab'lar buraya spawn edilecek
│   │   - RectTransform: Stretch (0,0,0,0)
│   ├── NextButton (Button)
│   ├── SkipButton (Button) - Opsiyonel
│   └── TimerText (TextMeshPro)
└── AbilityTutorialManager (Empty GameObject)
    └── AbilityTutorialManager (Script)
```

### Adım 3: AbilityTutorialManager Ayarları

```
Inspector:

Tutorial Panel Prefabs:
- Add Stop Tutorial Prefab: AddStopTutorialPanel
- Universal Pathfinding Tutorial Prefab: UniversalPathfindingTutorialPanel
- Flasher Tutorial Prefab: FlasherTutorialPanel
- Shuffle Tutorial Prefab: ShuffleTutorialPanel

Spawn Parent:
- Panel Container: Canvas/PanelContainer

Navigation Buttons:
- Next Button: NextButton
- Skip Button: SkipButton

Auto Skip:
- Auto Skip Delay: 5

UI Elements:
- Timer Text: TimerText
```

## 🔄 Runtime Akışı

### Level 4 İlk Kez:

```
1. SceneManager.LoadLevelSceene()
   → saveData.currentAbilityTutorial = "AddStop"
   → SaveGame()
   → Load AbilityTutorial Scene

2. AbilityTutorialManager.Start()
   → Read: currentAbilityTutorial = "AddStop"
   → SpawnTutorialPanel("AddStop")
   → Instantiate(addStopTutorialPrefab, panelContainer)
   → spawnedPanel.GetComponentInChildren<AbilityTutorialButton>()
   → tutorialButton.SetTutorialManager(this)

3. Tutorial oynatılır
   → Typewriter effect
   → Auto-click (3x)
   → Hand animation

4. OnTutorialCompleted()
   → Auto-skip countdown (5s)
   → Next button VEYA 5 saniye
   → hasSeenAddStopTutorial = true
   → currentAbilityTutorial = ""
   → SaveGame()
   → Load Level 4
```

## ✨ Avantajlar

### 1. Modular
```
✅ Her ability'nin kendi prefab'ı
✅ Bağımsız olarak düzenlenebilir
✅ Kolayca test edilebilir
```

### 2. Clean Scene
```
✅ AbilityTutorial sahnesi minimal
✅ Sadece manager ve container
✅ Prefab'lar runtime'da spawn edilir
```

### 3. Flexible
```
✅ Yeni ability eklemek kolay
✅ Prefab'ı oluştur, manager'a ekle
✅ Kod değişikliği minimal
```

### 4. No Manual Setup
```
✅ Tutorial Manager referansı otomatik
✅ Runtime'da ConnectTutorialButton()
✅ Inspector'da boş bırakılabilir
```

## 🎯 Prefab Oluşturma Checklist

### AddStopTutorialPanel:
- ✅ Hierarchy'de oluştur
- ✅ Script'leri ekle
- ✅ Referansları ayarla
- ✅ Tutorial Manager: BOŞ BIRAK
- ✅ Project'e sürükle → Prefab
- ✅ Hierarchy'den sil

### UniversalPathfindingTutorialPanel:
- ✅ Aynı yapı
- ✅ Farklı içerik
- ✅ Kendi tutorial logic'i

### FlasherTutorialPanel:
- ✅ Aynı yapı
- ✅ Farklı içerik
- ✅ Kendi tutorial logic'i

### ShuffleTutorialPanel:
- ✅ Aynı yapı
- ✅ Farklı içerik
- ✅ Kendi tutorial logic'i

## 🧪 Test

### Test 1: Prefab Spawn
```
1. Level 4 aç
2. Console'da kontrol et:
   ✅ "[AbilityTutorialManager] Current tutorial type: AddStop"
   ✅ "[AbilityTutorialManager] Spawning Add Stop Tutorial"
   ✅ "[AbilityTutorialManager] Spawned panel: AddStopTutorialPanel(Clone)"
   ✅ "[AbilityTutorialManager] Connected tutorial button to manager"
```

### Test 2: Manager Connection
```
1. Tutorial oynatılsın
2. 3 tıklama tamamlansın
3. Console'da kontrol et:
   ✅ "[AbilityTutorialButton] Tutorial manager set"
   ✅ "[AbilityTutorialButton] Notifying manager: tutorial completed"
   ✅ "[AbilityTutorialManager] Tutorial completed, starting auto-skip timer"
```

### Test 3: Auto-Skip
```
1. 5 saniye bekle
2. Console'da kontrol et:
   ✅ "Devam ediliyor... 5"
   ✅ "Devam ediliyor... 4"
   ✅ "Devam ediliyor... 3"
   ✅ "Devam ediliyor... 2"
   ✅ "Devam ediliyor... 1"
   ✅ "[AbilityTutorialManager] Auto-skip triggered"
   ✅ "hasSeenAddStopTutorial: true"
```

## 🐛 Sorun Giderme

### Prefab spawn olmuyor
- Prefab field'ı dolu mu?
- PanelContainer null değil mi?
- Console'da error var mı?

### Tutorial Manager bağlanmıyor
- AbilityTutorialButton prefab içinde mi?
- GetComponentInChildren çalışıyor mu?
- SetTutorialManager() çağrılıyor mu?

### Yanlış prefab spawn oluyor
- currentAbilityTutorial doğru mu?
- Switch-case doğru mu?
- SaveGameData güncel mi?

## 📝 Önemli Notlar

### 1. Tutorial Manager Referansı
```
❌ YANLIŞ: Inspector'da manuel ayarla
✅ DOĞRU: Runtime'da SetTutorialManager() ile set et
```

### 2. Prefab Hierarchy
```
✅ AbilityTutorialButton prefab içinde olmalı
✅ GetComponentInChildren ile bulunabilir olmalı
```

### 3. Panel Container
```
✅ RectTransform: Stretch (0,0,0,0)
✅ Canvas'ın child'ı olmalı
✅ Boş GameObject
```

Sistem hazır! 🎉
