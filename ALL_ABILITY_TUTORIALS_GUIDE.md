# Tüm Ability Tutorial'ları - Kullanım Rehberi

Her ability için `IAbilityTutorial` interface'ini implement eden ayrı bir script var.

## 📋 Ability Tutorial Scriptleri

| Ability | Script | Level | Container Field |
|---------|--------|-------|-----------------|
| Add Stop | `AddStopTutorial.cs` | 4 | `stopContainer` |
| Universal Pathfinding | `UniversalPathfindingTutorial.cs` | 8 | `passengerContainer` |
| Flasher (Remove Wagons) | `RemoveWagonsTutorial.cs` | 16 | `wagonContainer` |
| Shuffle Colors | `ShuffleColorsTutorial.cs` | 32 | `colorContainer` |

## 🎯 Ortak Özellikler

Tüm tutorial'lar aynı pattern'i kullanır:

### 1. Auto-Fill System
```csharp
// Container'dan otomatik child bulma
AutoFillXXXPanels()
  → container.GetChild(i)
  → Otomatik array doldurma
```

### 2. Dynamic Cost
```csharp
public int GetCost()
{
    int usageCount = currentIndex;
    int baseCost = 100;
    return baseCost * (int)Mathf.Pow(2, usageCount);
}
// 100 → 200 → 400 → 800
```

### 3. IAbilityTutorial Implementation
```csharp
void OnAbilityUsed();
void ResetTutorial();
bool IsCompleted { get; }
int GetCost();
string GetAbilityName();
string GetDescription();
```

## 📦 Prefab Yapıları

### 1. AddStopTutorialPanel

```
AddStopTutorialPanel (Prefab)
├── TutorialManager (Empty)
│   └── AddStopTutorial (Script)
│       - Stop Container: StopContainer
│       - Cost Text: CostText
├── DescriptionText + TypewriterEffect
├── StopContainer (Empty) ← 4 StopPanel child
│   ├── StopPanel_1
│   ├── StopPanel_2
│   ├── StopPanel_3
│   └── StopPanel_4
├── CostText (TextMeshPro)
├── HandImage (Image)
└── AddStopButton + AbilityTutorialButton
    - Tutorial Behaviour: TutorialManager
```

### 2. UniversalPathfindingTutorialPanel

```
UniversalPathfindingTutorialPanel (Prefab)
├── TutorialManager (Empty)
│   └── UniversalPathfindingTutorial (Script)
│       - Passenger Container: PassengerContainer
│       - Cost Text: CostText
├── DescriptionText + TypewriterEffect
├── PassengerContainer (Empty) ← 2 PassengerPanel child
│   ├── PassengerPanel_1 (Image - Normal color)
│   └── PassengerPanel_2 (Image - Normal color)
├── CostText (TextMeshPro)
├── HandImage (Image)
└── UniversalPathButton + AbilityTutorialButton
    - Tutorial Behaviour: TutorialManager
```

### 3. FlasherTutorialPanel (RemoveWagons)

```
FlasherTutorialPanel (Prefab)
├── TutorialManager (Empty)
│   └── RemoveWagonsTutorial (Script)
│       - Wagon Container: WagonContainer
│       - Cost Text: CostText
├── DescriptionText + TypewriterEffect
├── WagonContainer (Empty) ← 3 WagonPanel child
│   ├── WagonPanel_1 (Image - Wagon sprite)
│   ├── WagonPanel_2 (Image - Wagon sprite)
│   └── WagonPanel_3 (Image - Wagon sprite)
├── CostText (TextMeshPro)
├── HandImage (Image)
└── FlasherButton + AbilityTutorialButton
    - Tutorial Behaviour: TutorialManager
```

### 4. ShuffleTutorialPanel

```
ShuffleTutorialPanel (Prefab)
├── TutorialManager (Empty)
│   └── ShuffleColorsTutorial (Script)
│       - Color Container: ColorContainer
│       - Cost Text: CostText
├── DescriptionText + TypewriterEffect
├── ColorContainer (Empty) ← 3 ColorPanel child
│   ├── ColorPanel_1 (Image - Red)
│   ├── ColorPanel_2 (Image - Green)
│   └── ColorPanel_3 (Image - Blue)
├── CostText (TextMeshPro)
├── HandImage (Image)
└── ShuffleButton + AbilityTutorialButton
    - Tutorial Behaviour: TutorialManager
```

## 🔧 AbilityTutorialButton Ayarları

**HER PREFAB İÇİN AYNI:**

```
Tutorial Reference:
- Tutorial Behaviour: TutorialManager (prefab içindeki)

UI Elements:
- Button Text: (opsiyonel)
- Cost Text: (opsiyonel)
- Description Text: DescriptionText

Typewriter Effect:
- Typewriter Effect: DescriptionText/TypewriterEffect
- Enable Auto Click: ✅ True
- Auto Click Count: 3 (Add Stop için), 2-3 (diğerleri)
- Auto Click Delay: 2
- Tutorial Manager: ❌ BOŞ BIRAK!

Visual Effects:
- Click Particle Effect Prefab: ClickEffect
- Particle Display Duration: 1

Hand Animation:
- Hand Image: HandImage
- Hand Click Anim Duration: 0.3

Audio:
- Button Click Sound: ButtonClick.wav
```

## 🎬 Her Ability'nin Davranışı

### Add Stop (4 panel)
```
1. Tıklama: Stop 2 aktif → Cost: 100
2. Tıklama: Stop 3 aktif → Cost: 200
3. Tıklama: Stop 4 aktif → Cost: 400
Completed!
```

### Universal Pathfinding (2 panel)
```
1. Tıklama: Passenger 1 cyan renk → Cost: 100
2. Tıklama: Passenger 2 cyan renk → Cost: 200
Completed!
```

### Flasher/Remove Wagons (3 panel)
```
1. Tıklama: Wagon 1 kaldırılır → Cost: 100
2. Tıklama: Wagon 2 kaldırılır → Cost: 200
3. Tıklama: Wagon 3 kaldırılır → Cost: 400
Completed!
```

### Shuffle Colors (3 panel)
```
1. Tıklama: Renkler rotate → Cost: 100
2. Tıklama: Renkler rotate → Cost: 200
3. Tıklama: Renkler rotate → Cost: 400
Completed!
```

## ✅ Prefab Oluşturma Checklist

### Her Prefab İçin:

1. **Container Oluştur:**
   - Empty GameObject
   - İlgili sayıda child panel ekle

2. **TutorialManager Oluştur:**
   - Empty GameObject
   - İlgili tutorial script ekle
   - Container referansını ayarla
   - Cost Text referansını ayarla

3. **DescriptionText:**
   - TextMeshPro
   - TypewriterEffect script ekle

4. **Button:**
   - Button component
   - AbilityTutorialButton script ekle
   - Tutorial Behaviour: TutorialManager
   - Diğer referansları ayarla
   - Tutorial Manager: BOŞ BIRAK!

5. **Prefab Yap:**
   - Project'e sürükle
   - Hierarchy'den sil

## 🧪 Test

### Her Ability İçin:

```
1. Prefab'ı oluştur
2. AbilityTutorialManager'a ekle
3. SaveGameData'da currentAbilityTutorial set et
4. Tutorial sahnesini yükle
5. Doğru prefab spawn olmalı
6. Auto-click çalışmalı
7. Maliyet artmalı (100 → 200 → 400)
8. Completed! göstermeli
```

## 📝 Önemli Notlar

### 1. Container Pattern
```
✅ Her tutorial kendi container'ını kullanır
✅ Auto-fill ile child'ları otomatik bulur
✅ Manuel array doldurmaya gerek yok
```

### 2. Tutorial Behaviour
```
✅ Her prefab kendi tutorial script'ini kullanır
✅ AbilityTutorialButton → Tutorial Behaviour referansı
✅ Prefab içinde ayarlanır
```

### 3. Tutorial Manager
```
❌ Prefab içinde AYARLAMA!
✅ Runtime'da AbilityTutorialManager set eder
✅ SetTutorialManager() ile
```

Tüm tutorial'lar hazır! 🎉
