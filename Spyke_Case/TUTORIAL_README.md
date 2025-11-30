# 🎮 Tutorial Sistemi - Özet ve Hızlı Başlangıç

## 📋 Oluşturulan Dosyalar

1. **TutorialManager.cs** - Ana tutorial sistemi script'i
2. **TUTORIAL_SETUP.md** - Detaylı kurulum rehberi
3. **PASSENGERGROUP_TUTORIAL_PATCH.md** - PassengerGroup.cs için kod ekleme rehberi
4. **TUTORIAL_UI_ASSETS.md** - UI sprite'ları rehberi
5. **Bu dosya** - Hızlı başlangıç özeti

## 🎯 Tutorial Sistemi Özellikleri

### ✅ Tamamlanan Özellikler

- **TutorialManager Script**: Adım adım tutorial yönetimi
- **El Animasyonu**: Tıklanacak yeri gösteren animasyonlu el
- **Vurgulama Sistemi**: Hedef objeyi vurgulayan daire efekti
- **Input Engelleme**: Tutorial dışı tıklamaları engeller
- **Kayıt Sistemi**: Tutorial bir kez gösterilir (PlayerPrefs)
- **Event Sistemi**: StopManager ve InputManager ile entegrasyon
- **Adım Kontrolü**: 3 aşamalı tutorial akışı

### 🎮 Tutorial Akışı

```
1. Oyun Başlar (Level 1)
   ↓
2. Tutorial Canvas Aktif Olur
   ↓
3. ADIM 1: İlk Passenger'a Tıklama
   - El animasyonu gösterilir
   - Hedef passenger vurgulanır
   - Sadece doğru passenger'a tıklanabilir
   ↓
4. ADIM 2: Passenger Durağa Gider
   - Oyuncu izler
   - "Harika! Araba durağa gidiyor..." mesajı
   ↓
5. ADIM 3: Boarding (Yük Alma)
   - Wagon ve Passenger eşleşmesi gösterilir
   - "Aynı renkteki yükler otomatik alınacak!" mesajı
   ↓
6. Tutorial Tamamlanır
   - Canvas fade out olur
   - Normal oyun başlar
   - Tutorial bir daha gösterilmez
```

## 🚀 Hızlı Kurulum (5 Dakika)

### 1. Unity'de UI Oluşturma (2 dakika)

```
Hierarchy → Sağ Tık → UI → Canvas (TutorialCanvas)
  ├── Image (DarkOverlay) - Siyah, Alpha: 180
  ├── Image (HighlightCircle) - Sarı daire, 200x200
  ├── Image (HandIcon) - El sprite'ı, 100x100
  └── Text (TutorialText) - Beyaz, Font Size: 48
```

### 2. TutorialManager Ekleme (1 dakika)

```
Hierarchy → Sağ Tık → Create Empty (TutorialManager)
Inspector → Add Component → TutorialManager
```

### 3. Referansları Bağlama (1 dakika)

TutorialManager Inspector'da:
- Tutorial Canvas → TutorialCanvas'ı sürükle
- Hand Icon → HandIcon RectTransform'u sürükle
- Dark Overlay → DarkOverlay Image'ı sürükle
- Highlight Circle → HighlightCircle GameObject'ini sürükle
- Tutorial Text → TutorialText'i sürükle

### 4. Kod Entegrasyonu (1 dakika)

`PassengerGroup.cs` → `HandleTap` metoduna ekle:

```csharp
// Tutorial kontrolü
if (TutorialManager.Instance != null && TutorialManager.Instance.IsInputBlocked())
{
    Debug.Log($"[PassengerGroup] Tap on {name} during tutorial - letting tutorial handle it.");
    return;
}
```

**Detaylar için**: `PASSENGERGROUP_TUTORIAL_PATCH.md`

## 📦 Gereksinimler

### Unity Paketleri
- ✅ **DOTween** - Animasyonlar için (zaten kurulu)
- ✅ **TextMeshPro** - Metin için (opsiyonel, legacy Text de kullanılabilir)

### Sprite'lar
- 🖼️ **Hand Icon** - El işareti sprite'ı
- 🖼️ **Circle** - Vurgulama dairesi (Unity built-in kullanılabilir)

**Detaylar için**: `TUTORIAL_UI_ASSETS.md`

## 🎨 UI Sprite'ları Nereden Bulunur?

### Hızlı Çözüm (Önerilen)
1. [Flaticon](https://www.flaticon.com/search?word=hand%20pointer) - "hand pointer" ara
2. PNG indir (256x256 veya 512x512)
3. `Assets/Sprites/UI/Tutorial/` klasörüne ekle
4. Unity'de import ayarlarını yap (Sprite 2D/UI)

### Unity Built-in Kullanma
- Circle için: Unity'nin default `UI/Skin/Knob` sprite'ı
- Hand için: Basit bir ok işareti bile yeterli

## 🧪 Test Etme

### İlk Test
1. Level 1'i başlat
2. Tutorial otomatik başlamalı
3. El animasyonu görünmeli
4. Hedef passenger vurgulanmalı
5. Sadece o passenger'a tıklanabilmeli

### Tutorial'ı Sıfırlama
```csharp
// Inspector'da TutorialManager → Sağ Tık → Reset Tutorial
// Veya
TutorialManager.Instance.ResetTutorial();
```

## 🔧 Özelleştirme

### Tutorial Metinlerini Değiştir
`TutorialManager.cs` içinde:
- Satır ~140: "Arabaya tıklayarak durağa gönderin!"
- Satır ~160: "Harika! Araba durağa gidiyor..."
- Satır ~175: "Aynı renkteki yükler otomatik olarak alınacak!"

### Tutorial Level'ını Değiştir
```csharp
[SerializeField] private int tutorialLevel = 1; // İstediğiniz level
```

### Tutorial'ı Devre Dışı Bırak
```csharp
[SerializeField] private bool enableTutorial = false;
```

## 📝 Kod Yapısı

### TutorialManager.cs
```csharp
public class TutorialManager : MonoBehaviour
{
    // Singleton
    public static TutorialManager Instance { get; private set; }
    
    // Tutorial adımları
    private enum TutorialStep
    {
        None,
        WaitingForStart,
        ClickFirstPassenger,
        WaitForPassengerAtStop,
        WaitForBoarding,
        TutorialComplete
    }
    
    // Ana metodlar
    - StartTutorialSequence() // Tutorial başlatır
    - Step1_ClickFirstPassenger() // İlk adım
    - Step2_WaitForPassengerAtStop() // İkinci adım
    - Step3_WaitForBoarding() // Üçüncü adım
    - CompleteTutorial() // Tutorial'ı bitirir
    
    // Yardımcı metodlar
    - ShowHandAnimation() // El animasyonu
    - ShowHighlight() // Vurgulama
    - FindFirstPassenger() // Hedef passenger bulma
    - IsInputBlocked() // Input kontrolü
}
```

## 🐛 Sorun Giderme

### Tutorial Görünmüyor
1. ✅ TutorialCanvas aktif mi?
2. ✅ Level 1'de misiniz?
3. ✅ `enableTutorial = true` mi?
4. ✅ Tutorial daha önce tamamlanmış mı? (Reset Tutorial)

### El Animasyonu Çalışmıyor
1. ✅ HandIcon sprite'ı var mı?
2. ✅ DOTween kurulu mu?
3. ✅ HandIcon RectTransform bağlı mı?

### Input Bloklanmıyor
1. ✅ PassengerGroup.cs'de tutorial kontrolü var mı?
2. ✅ InputManager.cs güncellenmiş mi?

### Passenger Hareket Etmiyor
1. ✅ Tutorial tamamlandı mı?
2. ✅ `IsInputBlocked()` false dönüyor mu?
3. ✅ Console'da hata var mı?

## 📚 Detaylı Dökümanlar

1. **TUTORIAL_SETUP.md** - Kapsamlı kurulum rehberi
2. **PASSENGERGROUP_TUTORIAL_PATCH.md** - Kod entegrasyonu
3. **TUTORIAL_UI_ASSETS.md** - UI sprite'ları rehberi

## 🎯 Sonraki Adımlar

### Şimdi Yapılacaklar
1. ✅ Unity'de UI oluştur
2. ✅ TutorialManager ekle ve referansları bağla
3. ✅ PassengerGroup.cs'yi güncelle
4. ✅ Sprite'ları ekle
5. ✅ Test et

### Gelecekte Eklenebilecekler
- 🔮 Daha fazla tutorial adımı (ability kullanımı, vb.)
- 🔮 Ses efektleri
- 🔮 Daha gelişmiş animasyonlar
- 🔮 Çoklu dil desteği
- 🔮 Tutorial skip özelliği

## 💡 İpuçları

1. **Test Sırasında**: Tutorial'ı sık sık sıfırlayın
2. **Sprite'lar**: Basit sprite'larla başlayın, sonra iyileştirin
3. **Metinler**: Kısa ve net tutun
4. **Animasyonlar**: Abartmayın, oyuncuyu rahatsız etmesin
5. **Input Bloklama**: Sadece tutorial sırasında aktif olmalı

## 📞 Destek

Sorun yaşarsanız:
1. Console'daki hataları kontrol edin
2. Debug.Log mesajlarını takip edin
3. Tutorial adımlarını tek tek test edin
4. Referansların doğru bağlandığından emin olun

---

**Hazırlayan**: Gemini AI Assistant
**Tarih**: 30 Kasım 2025
**Versiyon**: 1.0

**Not**: Bu tutorial sistemi tamamen modüler ve genişletilebilir bir yapıya sahiptir. İhtiyacınıza göre kolayca özelleştirebilirsiniz.
