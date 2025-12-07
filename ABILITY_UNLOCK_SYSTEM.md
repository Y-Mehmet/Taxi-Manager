# Ability Unlock System - Implementation Guide

## ✅ Tamamlanan Değişiklikler

### 1. SaveGameData.cs
Eklenen alanlar:
```csharp
public int abilityAddNewStopUnlockLevel = 4;
public int abilityUniversalPathfindingUnlockLevel = 8;
public int abilityRemoveWagonsUnlockLevel = 16;
public int abilityShuffleWagonColorsUnlockLevel = 32;
```

### 2. AbilityUnlockManager.cs (YENİ DOSYA)
Merkezi unlock yönetim sistemi oluşturuldu.
Lokasyon: `Assets/Scripts/Core/AbilityUnlockManager.cs`

Özellikler:
- `IsAbilityUnlocked(AbilityType)` - Ability unlock mu kontrol eder
- `GetAbilityUnlockLevel(AbilityType)` - Unlock level'ı döndürür
- `GetLockedIconSprite()` - Locked icon sprite'ını döndürür
- `GetUnlockLevelText(AbilityType)` - "Unlocks at Level 5" formatında text döndürür

### 3. AbilityButton.cs
Eklenen özellikler:
- AllLevel sahnesinde (build index 1) unlock kontrolü
- Locked durumda:
  - Cost text → "Unlocks at Level X" gösterir
  - Icon → Locked sprite'a değişir
  - Button → Interactable false olur

## 🔧 Manuel Yapılması Gerekenler

### AbilityButton.cs'e Eklenmesi Gereken Field:

Satır 18'den sonra ekleyin:
```csharp
[SerializeField] private Image abilityIconImage; // Ability icon image (locked/unlocked sprite için)
```

Satır 20'den sonra ekleyin:
```csharp
private Sprite originalIconSprite; // Orijinal icon sprite (unlock durumu için)
```

## 🎮 Unity Inspector Ayarları

### 1. AbilityUnlockManager GameObject Oluştur:
- Hierarchy'de boş GameObject oluştur
- İsim: "AbilityUnlockManager"
- Component ekle: `AbilityUnlockManager.cs`
- Inspector'da:
  - **Locked Icon Sprite:** Kilit ikonu sprite'ını sürükle

### 2. Her AbilityButton için:
- Inspector'da yeni field görünecek:
  - **Ability Icon Image:** Ability'nin icon Image component'ini sürükle

## 📋 Unlock Level Yapılandırması

SaveGame.json'da varsayılan değerler:
- **Add New Stop:** Level 4'te açılır (index 4)
- **Universal Pathfinding:** Level 8'de açılır (index 8)
- **Remove Wagons (Flasher):** Level 16'da açılır (index 16)
- **Shuffle Wagon Colors:** Level 32'de açılır (index 32)

## 🎯 Çalışma Mantığı

```
Player Level 3'te
    ↓
AbilityButton Initialize
    ↓
Scene Index == 1? (AllLevel)
    ↓ YES
AbilityUnlockManager.IsAbilityUnlocked(AddNewStop)?
    ↓
MaxOpenedLevel (3) >= UnlockLevel (4)?
    ↓ NO
SetLockedState()
    ↓
- Text: "Unlocks at Level 5"
- Icon: Locked sprite
- Interactable: false
```

## 🔍 Test Senaryoları

1. **Level 3'te:**
   - Add New Stop → LOCKED (Unlocks at Level 5)
   - Diğerleri → LOCKED

2. **Level 4'te:**
   - Add New Stop → UNLOCKED (100 Coin)
   - Diğerleri → LOCKED

3. **Level 8'de:**
   - Add New Stop → UNLOCKED
   - Universal Pathfinding → UNLOCKED (100 Coin)
   - Diğerleri → LOCKED

## ⚠️ Önemli Notlar

- Unlock kontrolü **SADECE AllLevel sahnesinde** (build index 1) yapılır
- Tutorial sahnesinde veya diğer sahnelerde kontrol yapılmaz
- Level index 0'dan başlar, ama kullanıcıya "Level 1" olarak gösterilir (+1)
- Unlock level'lar SaveGameData'dan okunur (data-driven)
