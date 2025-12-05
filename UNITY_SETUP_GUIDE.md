# Unity Setup Rehberi - Level-Based Ability Tutorial

Bu rehber, Unity'de hangi sahnelerde ne yapmanız gerektiğini adım adım açıklar.

## 📋 Gerekli Sahneler

### 1. MainMenu Scene (Build Index 0)
- ✅ Zaten var, değişiklik YOK

### 2. AllLevel Scene (Build Index 1)
- ✅ Zaten var, değişiklik YOK

### 3. AbilityTutorial Scene (Build Index 2)
- ⚠️ **YENİ SAHNE OLUŞTURULMALI**

## 🏗️ AbilityTutorial Scene Kurulumu

### Adım 1: Yeni Sahne Oluştur

```
1. File → New Scene
2. Save As: "AbilityTutorial"
3. Kaydet: Assets/Scenes/AbilityTutorial.unity
```

### Adım 2: Build Settings'e Ekle

```
1. File → Build Settings
2. "Add Open Scenes" tıkla
3. Sıralama:
   - 0: MainMenu
   - 1: AllLevel
   - 2: AbilityTutorial ← YENİ
```

### Adım 3: Sahne Hiyerarşisi Oluştur

```
AbilityTutorial
├── Canvas
│   ├── Panel_AddStop (GameObject)
│   │   ├── TutorialManager (Empty GameObject)
│   │   │   └── AbilityTutorialManager (Script)
│   │   ├── Title (TextMeshPro)
│   │   ├── DescriptionText (TextMeshPro)
│   │   │   └── TypewriterEffect (Script)
│   │   ├── StopContainer (Empty GameObject)
│   │   │   ├── StopPanel_1
│   │   │   │   ├── InactiveIndicator (Image)
│   │   │   │   ├── ActiveIndicator (Image)
│   │   │   │   ├── StopIcon (Image)
│   │   │   │   └── StopText (TextMeshPro)
│   │   │   ├── StopPanel_2 (aynı yapı)
│   │   │   ├── StopPanel_3 (aynı yapı)
│   │   │   └── StopPanel_4 (aynı yapı)
│   │   ├── CostText (TextMeshPro)
│   │   ├── HandImage (Image)
│   │   └── AddStopButton (Button)
│   │       └── AbilityTutorialButton (Script)
│   ├── NextButton (Button)
│   ├── SkipButton (Button) - Opsiyonel
│   └── TimerText (TextMeshPro)
```

## 🔧 Script Ayarları

### TutorialManager (AbilityTutorialManager)

```
Inspector Ayarları:
- Current Ability Type: AddNewStop (Inspector'dan AYARLAMA!)
  ⚠️ Bu field artık otomatik doldurulacak, boş bırakın!
  
- Tutorial Panel: Panel_AddStop
- Next Button: NextButton
- Skip Button: SkipButton (opsiyonel)
- Auto Skip Delay: 5
- Timer Text: TimerText
```

### DescriptionText (TypewriterEffect)

```
Inspector Ayarları:
- Characters Per Second: 30
- Skip On Tap: ✅ True
```

### AddStopButton (AbilityTutorialButton)

```
Inspector Ayarları:
Tutorial Reference:
- Tutorial Behaviour: TutorialManager

UI Elements:
- Button Text: (opsiyonel)
- Cost Text: (opsiyonel)
- Description Text: DescriptionText

Typewriter Effect:
- Typewriter Effect: DescriptionText/TypewriterEffect
- Enable Auto Click: ✅ True
- Auto Click Count: 3
- Auto Click Delay: 2
- Tutorial Manager: TutorialManager ← ÖNEMLİ!

Visual Effects:
- Click Particle Effect Prefab: ClickEffect (Prefab)
- Particle Display Duration: 1

Hand Animation:
- Hand Image: HandImage
- Hand Click Anim Duration: 0.3

Audio:
- Button Click Sound: ButtonClick.wav
```

### AddStopTutorial (Script)

```
Inspector Ayarları:
Stop Container:
- Stop Container: StopContainer

Cost Display:
- Cost Text: CostText

Settings:
- Ability Type: AddNewStop
- Ability Name: "Add Stop"
- Description: "Add a new stop to the map..."
```

## 📊 SceneManager Ayarları (Core Scene)

```
Inspector'da:
- Ability Tutorial Build Index: 2 ← ÖNEMLİ!
```

## 🎯 Diğer Ability Panelleri (Gelecek)

Aynı yapıyı kullanarak diğer ability'ler için paneller oluşturun:

### Panel_UniversalPathfinding
- TutorialManager → currentAbilityType: UniversalPathfinding
- Kendi tutorial logic'i

### Panel_Flasher
- TutorialManager → currentAbilityType: RemoveWagons
- Kendi tutorial logic'i

### Panel_Shuffle
- TutorialManager → currentAbilityType: ShuffleWagonColors
- Kendi tutorial logic'i

## ✅ Test Checklist

### Test 1: SaveGameData Kontrolü
```
1. Play mode'a gir
2. Console'da kontrol et:
   ✅ "hasSeenAddStopTutorial: false"
   ✅ "currentAbilityTutorial: """
```

### Test 2: Level 4 İlk Kez
```
1. Level 3'ü tamamla
2. Level 4 aç
3. Play button'a tıkla
4. Console'da kontrol et:
   ✅ "[SceneManager] Level 4 - Loading Add Stop Tutorial"
   ✅ "currentAbilityTutorial: AddStop"
5. AbilityTutorial sahnesi yüklenmeli
6. Console'da kontrol et:
   ✅ "[AbilityTutorialManager] Loaded tutorial type: AddStop → AddNewStop"
```

### Test 3: Tutorial Tamamlama
```
1. Tutorial oynatılsın (3 tıklama)
2. 5 saniye bekle VEYA Next button'a tıkla
3. Console'da kontrol et:
   ✅ "[AbilityTutorialManager] Saved AddNewStop tutorial as seen"
   ✅ "hasSeenAddStopTutorial: true"
4. Level 4 yüklenmeli
```

### Test 4: Level 4 Tekrar
```
1. Level 4'ü tekrar seç
2. Play button'a tıkla
3. Console'da kontrol et:
   ✅ "[SceneManager] Loading Level 4 directly (no tutorial)"
4. Tutorial atlanmalı, direkt level yüklenmeli
```

## 🐛 Sorun Giderme

### "currentAbilityTutorial" boş
- SceneManager'da SaveGame() çağrılıyor mu?
- SaveGameData.currentAbilityTutorial set ediliyor mu?

### Tutorial yanlış ability gösteriyor
- AbilityTutorialManager.Start() doğru çalışıyor mu?
- Switch-case doğru mu?
- Console log'larını kontrol et

### Tutorial tekrar gösteriliyor
- hasSeenXXXTutorial flag'i true oluyor mu?
- SaveGame() çağrılıyor mu?

## 📝 Önemli Notlar

### 1. Inspector'da Current Ability Type AYARLAMA!
```
❌ YANLIŞ: Inspector'da manuel ayarla
✅ DOĞRU: SaveGameData'dan otomatik oku
```

### 2. SaveGameData Kullan
```
❌ YANLIŞ: PlayerPrefs.SetString("CurrentAbilityTutorial", "AddStop")
✅ DOĞRU: saveData.currentAbilityTutorial = "AddStop"
```

### 3. Build Index Kontrol Et
```
Build Settings:
0: MainMenu
1: AllLevel
2: AbilityTutorial ← Mutlaka 2 olmalı!
```

## 🚀 Hızlı Başlangıç

1. ✅ AbilityTutorial sahnesi oluştur
2. ✅ Build Settings'e ekle (index 2)
3. ✅ Panel_AddStop hiyerarşisini oluştur
4. ✅ Script'leri ekle ve ayarla
5. ✅ Test et (Level 4)

Sistem hazır! 🎉
