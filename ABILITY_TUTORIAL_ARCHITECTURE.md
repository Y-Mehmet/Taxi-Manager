# Ability Tutorial System - Architecture Overview

## 🏛️ SOLID Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    IAbilityTutorial                         │
│                     (Interface)                             │
├─────────────────────────────────────────────────────────────┤
│ + OnAbilityUsed() : void                                    │
│ + ResetTutorial() : void                                    │
│ + IsCompleted : bool                                        │
│ + GetCost() : int                                           │
│ + GetAbilityName() : string                                 │
└─────────────────────────────────────────────────────────────┘
                            ▲
                            │ implements
        ┌───────────────────┼───────────────────┐
        │                   │                   │
┌───────┴────────┐  ┌───────┴────────┐  ┌──────┴─────────┐
│  AddStopTutorial│  │RemoveWagons   │  │UniversalPath   │
│                │  │Tutorial       │  │findingTutorial │
├────────────────┤  ├───────────────┤  ├────────────────┤
│ - stopPanels[] │  │ - wagonObjs[] │  │ - pathSteps[]  │
│ - costText     │  │ - costText    │  │ - costText     │
│ - currentIndex │  │ - usageCount  │  │ - usageCount   │
└────────────────┘  └───────────────┘  └────────────────┘

┌─────────────────────────────────────────────────────────────┐
│              AbilityTutorialButton                          │
│                 (Generic Button)                            │
├─────────────────────────────────────────────────────────────┤
│ - tutorial : IAbilityTutorial  ← Dependency Injection      │
│ - button : Button                                           │
│ - buttonText : TextMeshProUGUI                              │
│ - costText : TextMeshProUGUI                                │
├─────────────────────────────────────────────────────────────┤
│ + OnButtonClicked() : void                                  │
│ + UpdateButtonUI() : void                                   │
│ + ResetTutorial() : void                                    │
└─────────────────────────────────────────────────────────────┘
                            │
                            │ uses
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                   StopPanelData                             │
│                   (Data Class)                              │
├─────────────────────────────────────────────────────────────┤
│ + panelObject : GameObject                                  │
│ + stopImage : Image                                         │
│ + stopText : TextMeshProUGUI                                │
│ + inactiveIndicator : GameObject                            │
│ + activeIndicator : GameObject                              │
├─────────────────────────────────────────────────────────────┤
│ + SetActive(bool) : void                                    │
│ + SetStopActive(bool) : void                                │
│ + SetText(string) : void                                    │
└─────────────────────────────────────────────────────────────┘
```

## 🔄 Execution Flow

```
User Clicks Button
        │
        ▼
┌──────────────────────┐
│ AbilityTutorialButton│
│  OnButtonClicked()   │
└──────────┬───────────┘
           │
           ▼
    ┌──────────────┐
    │ Check if     │
    │ IsCompleted? │
    └──┬───────┬───┘
       │       │
      NO      YES → Disable Button
       │
       ▼
┌──────────────────────┐
│ tutorial.            │
│ OnAbilityUsed()      │
└──────────┬───────────┘
           │
           ▼
    ┌──────────────────┐
    │ AddStopTutorial  │
    │ ActivateStop()   │
    └──────┬───────────┘
           │
           ▼
    ┌──────────────────┐
    │ StopPanelData    │
    │ SetStopActive()  │
    └──────┬───────────┘
           │
           ▼
    ┌──────────────────┐
    │ Update UI:       │
    │ - Hide Inactive  │
    │ - Show Active    │
    │ - Update Cost    │
    └──────────────────┘
```

## 📦 Component Relationships

```
Panel_AddStop
├── TutorialManager (GameObject)
│   └── AddStopTutorial (MonoBehaviour, IAbilityTutorial)
│       ├── Manages: stopPanels[4]
│       ├── Updates: costText
│       └── Tracks: currentStopIndex, totalCostSpent
│
├── StopContainer
│   ├── StopPanel_1 (Always Active)
│   │   ├── InactiveIndicator (Hidden)
│   │   └── ActiveIndicator (Shown)
│   ├── StopPanel_2 (Activates on 1st click)
│   ├── StopPanel_3 (Activates on 2nd click)
│   └── StopPanel_4 (Activates on 3rd click)
│
├── CostText (Shared)
│   └── Shows: "Harcanan: X Coin"
│
└── AddStopButton
    └── AbilityTutorialButton (MonoBehaviour)
        ├── References: TutorialManager (as IAbilityTutorial)
        ├── Updates: buttonText, costText
        └── Disables: when tutorial.IsCompleted
```

## 🎯 SOLID Principles Applied

### 1. Single Responsibility Principle (SRP)
- **AddStopTutorial**: Sadece stop panellerini yönetir
- **AbilityTutorialButton**: Sadece buton etkileşimini yönetir
- **StopPanelData**: Sadece panel verilerini tutar

### 2. Open/Closed Principle (OCP)
- Yeni ability tutorial'ları eklemek için **IAbilityTutorial** implement edin
- **AbilityTutorialButton** değişmeden tüm tutorial'larla çalışır
- Extension: Yeni tutorial class'ı oluştur
- Modification: Mevcut kod değişmez

### 3. Liskov Substitution Principle (LSP)
- Herhangi bir **IAbilityTutorial** implementation
- **AbilityTutorialButton** tarafından kullanılabilir
- Davranış tutarlılığı garanti edilir

### 4. Interface Segregation Principle (ISP)
- **IAbilityTutorial** sadece gerekli metodları içerir
- Client'lar kullanmadıkları metodlara bağımlı değil

### 5. Dependency Inversion Principle (DIP)
- **AbilityTutorialButton** concrete class'a değil **interface**'e bağımlı
- High-level module (Button) low-level module'e (Tutorial) bağımlı değil
- Her ikisi de abstraction'a (IAbilityTutorial) bağımlı

## 🔌 Dependency Injection

```csharp
// Unity Inspector üzerinden injection
[SerializeField] private MonoBehaviour tutorialBehaviour;

// Runtime'da interface'e cast
tutorial = tutorialBehaviour as IAbilityTutorial;

// Kullanım
tutorial.OnAbilityUsed();
```

## 📊 State Management

```
AddStopTutorial State:
┌─────────────────────────────────────┐
│ currentStopIndex: 1 (starts at 1)   │
│ totalCostSpent: 0                   │
│ IsCompleted: false                  │
└─────────────────────────────────────┘
                │
                │ User clicks button
                ▼
┌─────────────────────────────────────┐
│ currentStopIndex: 2                 │
│ totalCostSpent: 50                  │
│ IsCompleted: false                  │
└─────────────────────────────────────┘
                │
                │ User clicks button
                ▼
┌─────────────────────────────────────┐
│ currentStopIndex: 3                 │
│ totalCostSpent: 100                 │
│ IsCompleted: false                  │
└─────────────────────────────────────┘
                │
                │ User clicks button
                ▼
┌─────────────────────────────────────┐
│ currentStopIndex: 4                 │
│ totalCostSpent: 150                 │
│ IsCompleted: true ← Button disabled │
└─────────────────────────────────────┘
```

## 🧩 Extensibility Example

Yeni bir ability tutorial eklemek için:

```csharp
// 1. Interface'i implement et
public class MyNewTutorial : MonoBehaviour, IAbilityTutorial
{
    public void OnAbilityUsed()
    {
        // Custom logic
    }
    
    public void ResetTutorial()
    {
        // Custom reset
    }
    
    public bool IsCompleted => /* custom condition */;
    public int GetCost() => 100;
    public string GetAbilityName() => "My New Ability";
}

// 2. Unity'de:
// - GameObject oluştur
// - MyNewTutorial script ekle
// - Button oluştur
// - AbilityTutorialButton script ekle
// - Tutorial Behaviour field'ına MyNewTutorial'ı sürükle
// DONE! Kod değişikliği yok!
```

## 📝 Summary

Bu sistem:
- ✅ **Modular**: Her ability bağımsız
- ✅ **Reusable**: AbilityTutorialButton tüm ability'lerle çalışır
- ✅ **Extensible**: Yeni ability'ler kolayca eklenir
- ✅ **Maintainable**: SOLID prensipleri sayesinde bakımı kolay
- ✅ **Testable**: Interface sayesinde mock edilebilir
- ✅ **Type-safe**: Interface contract garanti eder
