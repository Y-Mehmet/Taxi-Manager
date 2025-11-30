# Tutorial Sistemi - Kurulum Rehberi

## 📋 Genel Bakış

Bu tutorial sistemi, Level 1'de oyunculara oyun mekaniklerini öğretmek için tasarlanmıştır. Sistem şu özelliklere sahiptir:

- ✅ **Adım adım rehberlik**: Oyuncuyu her adımda yönlendirir
- ✅ **El animasyonu**: Tıklanacak yeri gösteren animasyonlu el ikonu
- ✅ **Vurgulama**: Hedef objeyi vurgulayan daire efekti
- ✅ **Input engelleme**: Tutorial dışı tıklamaları engeller
- ✅ **Kayıt sistemi**: Tutorial bir kez gösterilir

## 🎮 Tutorial Adımları

### Adım 1: İlk Passenger'a Tıklama
- Oyuncuya ilk passenger'a (arabaya) tıklamasını öğretir
- El animasyonu ve vurgulama ile hedef gösterilir
- Sadece doğru passenger'a tıklamaya izin verilir

### Adım 2: Passenger'ın Durağa Gitmesi
- Passenger'ın durağa hareket etmesini gösterir
- Oyuncu sadece izler

### Adım 3: Boarding (Yük Alma)
- Wagon (yük) ve Passenger (araba) renklerinin eşleşmesini gösterir
- Otomatik yük alma mekanizmasını açıklar

## 🛠️ Unity'de Kurulum

### 1. Tutorial Canvas Oluşturma

1. **Hierarchy'de sağ tık** → `UI` → `Canvas`
2. Canvas'ı `TutorialCanvas` olarak adlandır
3. Canvas ayarları:
   - Render Mode: `Screen Space - Overlay`
   - Canvas Scaler → UI Scale Mode: `Scale With Screen Size`
   - Reference Resolution: `1920x1080`

### 2. Dark Overlay (Karartma Efekti)

1. TutorialCanvas içinde sağ tık → `UI` → `Image`
2. `DarkOverlay` olarak adlandır
3. Ayarlar:
   - Anchor: Stretch (tüm ekranı kaplasın)
   - Color: Siyah (R:0, G:0, B:0, A:180)
   - Raycast Target: ✅ (aktif)

### 3. Highlight Circle (Vurgulama Dairesi)

1. TutorialCanvas içinde sağ tık → `UI` → `Image`
2. `HighlightCircle` olarak adlandır
3. Ayarlar:
   - Sprite: Circle sprite (Unity'nin default circle'ı veya kendi sprite'ınız)
   - Color: Sarı veya Turuncu (A:150-200 arası)
   - Width/Height: 200x200
   - Raycast Target: ❌ (kapalı)

### 4. Hand Icon (El İkonu)

1. TutorialCanvas içinde sağ tık → `UI` → `Image`
2. `HandIcon` olarak adlandır
3. Ayarlar:
   - Sprite: El işareti sprite'ı (Assets'e eklemeniz gerekir)
   - Width/Height: 100x100
   - Raycast Target: ❌ (kapalı)

**El Sprite'ı İçin:**
- Google'dan "hand pointer icon png" araması yapabilirsiniz
- Veya Unity Asset Store'dan ücretsiz UI pack indirebilirsiniz
- Sprite'ı `Assets/Sprites/UI/` klasörüne ekleyin

### 5. Tutorial Text

1. TutorialCanvas içinde sağ tık → `UI` → `Text - TextMeshPro` (veya legacy Text)
2. `TutorialText` olarak adlandır
3. Ayarlar:
   - Font Size: 48-60
   - Alignment: Center, Middle
   - Color: Beyaz
   - Anchor: Top Center
   - Position: (0, -100, 0)
   - Width: 1200, Height: 200

### 6. TutorialManager Script Ekleme

1. Hierarchy'de boş bir GameObject oluşturun
2. `TutorialManager` olarak adlandırın
3. Inspector'da `Add Component` → `TutorialManager` script'ini ekleyin

### 7. TutorialManager Referansları Bağlama

Inspector'da TutorialManager component'inde:

- **Enable Tutorial**: ✅ (aktif)
- **Tutorial Level**: `1`
- **Tutorial Canvas**: TutorialCanvas objesini sürükleyin
- **Hand Icon**: HandIcon RectTransform'unu sürükleyin
- **Dark Overlay**: DarkOverlay Image component'ini sürükleyin
- **Highlight Circle**: HighlightCircle GameObject'ini sürükleyin
- **Tutorial Text**: TutorialText component'ini sürükleyin

## 🔧 Kod Entegrasyonu

### PassengerGroup.cs Güncelleme

`HandleTap` metoduna tutorial kontrolü ekleyin:

```csharp
private void HandleTap(PassengerGroup tappedGroup)
{
    if (tappedGroup != this) return;

    if (onConveyorBelt)
    {
        TryMoveToWaitingArea();
        return;
    }

    // Tutorial kontrolü EKLE
    if (TutorialManager.Instance != null && TutorialManager.Instance.IsInputBlocked())
    {
        Debug.Log($"[PassengerGroup] Tap on {name} during tutorial - letting tutorial handle it.");
        return;
    }

    if (AbilityManager.Instance != null && AbilityManager.Instance.IsAbilityModeActive)
    {
        // The tap will be handled by the AbilityManager's subscriber. Do nothing here.
        Debug.Log($"[PassengerGroup] Tap on {name} is being handled by an active ability.");
        return;
    }

    // ... geri kalan kod
}
```

### InputManager.cs Güncelleme (Opsiyonel)

InputManager'da tutorial kontrolü zaten eklendi. Eğer eklenmemişse:

```csharp
// Tutorial aktifse ve input bloklanmışsa, normal input işlemlerini engelle
if (TutorialManager.Instance != null && TutorialManager.Instance.IsInputBlocked())
{
    Debug.Log("[InputManager] Input blocked by tutorial.");
    // Ancak raycast'i yine de yap ki tutorial event'i tetiklenebilsin
}
```

## 🎨 Görsel İyileştirmeler

### Highlight Circle için Glow Efekti

1. HighlightCircle'a `Outline` component ekleyin:
   - Effect Distance: (5, -5)
   - Effect Color: Beyaz veya Sarı

2. Veya `Shadow` component ekleyin:
   - Effect Distance: (0, 0)
   - Effect Color: Aynı renk ama daha açık

### Hand Icon Animasyonu İyileştirme

El ikonu için daha iyi bir sprite kullanmak isterseniz:
- Parmak işaret eden el
- Tıklama efekti olan el
- Animasyonlu el sprite sheet'i

## 🧪 Test Etme

1. **İlk Test**: Level 1'i başlatın
   - Tutorial otomatik olarak başlamalı
   - El animasyonu görünmeli
   - Sadece hedef passenger'a tıklanabilmeli

2. **İkinci Test**: Tutorial'ı tamamlayın
   - Passenger durağa gitmeli
   - Boarding gerçekleşmeli
   - Tutorial kapanmalı

3. **Üçüncü Test**: Oyunu yeniden başlatın
   - Tutorial bir daha gösterilmemeli
   - Normal oyun akışı devam etmeli

## 🔄 Tutorial'ı Sıfırlama

Test sırasında tutorial'ı sıfırlamak için:

1. **Inspector'dan**: TutorialManager component'inde sağ tık → `Reset Tutorial`
2. **Kod ile**: `TutorialManager.Instance.ResetTutorial()`
3. **PlayerPrefs**: Unity menüsünden `Edit` → `Clear All PlayerPrefs`

## 📝 Özelleştirme

### Tutorial Metinlerini Değiştirme

TutorialManager.cs içinde metinleri değiştirebilirsiniz:

```csharp
// Step 1
tutorialText.text = "Arabaya tıklayarak durağa gönderin!";

// Step 2
tutorialText.text = "Harika! Araba durağa gidiyor...";

// Step 3
tutorialText.text = "Aynı renkteki yükler otomatik olarak alınacak!";
```

### Tutorial'ı Farklı Levellerde Gösterme

```csharp
[SerializeField] private int tutorialLevel = 1; // Bunu değiştirin
```

### Tutorial Adımları Ekleme

Yeni adımlar eklemek için:

1. `TutorialStep` enum'una yeni adım ekleyin
2. `StartTutorialSequence()` içinde yeni coroutine ekleyin
3. Yeni adım için coroutine oluşturun

## 🐛 Sorun Giderme

### Tutorial Görünmüyor
- TutorialCanvas aktif mi kontrol edin
- TutorialManager referansları doğru mu kontrol edin
- Level 1'de misiniz kontrol edin

### El Animasyonu Çalışmıyor
- HandIcon sprite'ı atanmış mı kontrol edin
- DOTween kurulu mu kontrol edin

### Input Bloklanmıyor
- PassengerGroup.cs'de tutorial kontrolü var mı kontrol edin
- TutorialManager.Instance null değil mi kontrol edin

## 📚 Ek Kaynaklar

- DOTween Documentation: http://dotween.demigiant.com/documentation.php
- Unity UI Tutorial: https://learn.unity.com/tutorial/ui-components

---

**Not**: Bu tutorial sistemi DOTween kullanır. Projenizde DOTween yoksa Unity Package Manager'dan ekleyin.
