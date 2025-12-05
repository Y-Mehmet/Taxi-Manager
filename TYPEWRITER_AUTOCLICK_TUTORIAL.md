# Typewriter Effect & Auto-Click Tutorial System

Bu sistem, ability tutorial'larında daktilo efekti ve otomatik buton tıklama özelliği sağlar.

## 🎬 Nasıl Çalışır?

### 1. **Typewriter Effect (Daktilo Efekti)**
- Description metni karakter karakter yazılır
- Okuma hızından biraz daha hızlı (30 karakter/saniye)
- Ekrana dokunulursa metin anında tamamlanır

### 2. **Auto-Click (Otomatik Tıklama)**
- Typewriter tamamlandıktan sonra başlar
- 2 saniye arayla 3 kere otomatik buton tıklaması
- Tutorial tamamlanınca durur

## 📋 Unity Setup

### Hiyerarşi:
```
Panel_AddStop
├── TutorialManager + AddStopTutorial
├── DescriptionText + TypewriterEffect ← YENİ COMPONENT
├── StopContainer
├── CostText
└── AddStopButton + AbilityTutorialButton
```

### 1. TypewriterEffect Component Ekleyin

**DescriptionText** GameObject'ine **TypewriterEffect** component'ini ekleyin:

**Settings:**
- **Characters Per Second**: 30 (typing hızı)
- **Skip On Tap**: ✅ True (ekrana dokunarak atlama)

### 2. AbilityTutorialButton Ayarları

**AddStopButton** GameObject'inde **AbilityTutorialButton** scriptini güncelleyin:

**Typewriter Effect:**
- **Typewriter Effect**: DescriptionText (TypewriterEffect component'i olan)
- **Enable Auto Click**: ✅ True
- **Auto Click Count**: 3
- **Auto Click Delay**: 2.0

## 🎮 Kullanıcı Deneyimi

### Senaryo 1: Normal Akış
```
1. Panel açılır
2. Description metni yavaşça yazılmaya başlar
   "Add a new stop to the map..."
   (Daktilo efekti)
3. Metin tamamlanır
4. 2 saniye bekler
5. Otomatik 1. tıklama → Stop 2 aktif, Cost: 200
6. 2 saniye bekler
7. Otomatik 2. tıklama → Stop 3 aktif, Cost: 400
8. 2 saniye bekler
9. Otomatik 3. tıklama → Stop 4 aktif, Completed!
```

### Senaryo 2: Kullanıcı Ekrana Dokunur
```
1. Panel açılır
2. Description metni yazılmaya başlar
   "Add a new s..."
3. 👆 KULLANICI EKRANA DOKUNUR
4. Metin anında tamamlanır
   "Add a new stop to the map. This allows..."
5. 2 saniye bekler
6. Otomatik tıklama başlar (yukarıdaki gibi)
```

## 🔧 Kod Akışı

```csharp
// Start
AbilityTutorialButton.Start()
  → typewriterEffect.StartTyping(description)
  → TypewriterEffect.TypeText() coroutine başlar
  
// Typing
TypewriterEffect.TypeText()
  → Her karakter için delay
  → "A" → "Ad" → "Add" → ...
  
// Skip (opsiyonel)
User taps screen
  → TypewriterEffect.SkipToEnd()
  → Text anında tamamlanır
  → OnTypingComplete event fire
  
// Auto-Click
OnTypingComplete()
  → AutoClickSequence() coroutine başlar
  → 2 saniye bekle
  → OnButtonClicked() (1. tıklama)
  → 2 saniye bekle
  → OnButtonClicked() (2. tıklama)
  → 2 saniye bekle
  → OnButtonClicked() (3. tıklama)
  → Tutorial completed!
```

## ⚙️ Özelleştirme

### Typing Hızını Değiştirme:
```csharp
// TypewriterEffect component
Characters Per Second: 30 // Daha hızlı: 50, Daha yavaş: 15
```

### Auto-Click Ayarları:
```csharp
// AbilityTutorialButton component
Auto Click Count: 3      // Kaç kere tıklama
Auto Click Delay: 2.0    // Tıklamalar arası süre (saniye)
Enable Auto Click: true  // Otomatik tıklama aktif/pasif
```

### Typewriter'ı Devre Dışı Bırakma:
```csharp
// AbilityTutorialButton component
Typewriter Effect: None  // Boş bırakın
// Description metni anında gösterilir
```

## 🎯 Özellikler

### TypewriterEffect.cs
- ✅ Karakter karakter yazma
- ✅ Tap to skip (ekrana dokunarak atlama)
- ✅ OnTypingComplete event
- ✅ Reset fonksiyonu
- ✅ IsTyping ve IsComplete property'leri

### AbilityTutorialButton.cs
- ✅ Typewriter entegrasyonu
- ✅ Auto-click sequence
- ✅ Tutorial tamamlandığında durma
- ✅ Event cleanup (OnDestroy)

## 📊 Timeline Örneği

```
0s:  Panel açılır, typewriter başlar
     "A"
0.03s: "Ad"
0.06s: "Add"
...
3s:  Metin tamamlanır (veya kullanıcı atlar)
5s:  1. otomatik tıklama (2s delay)
     Stop 2 aktif, Cost: 200
7s:  2. otomatik tıklama (2s delay)
     Stop 3 aktif, Cost: 400
9s:  3. otomatik tıklama (2s delay)
     Stop 4 aktif, Completed!
```

## 🐛 Sorun Giderme

### Typewriter çalışmıyor
- TypewriterEffect component'i DescriptionText'e eklenmiş mi?
- AbilityTutorialButton'da Typewriter Effect field'ı dolu mu?

### Auto-click çalışmıyor
- Enable Auto Click aktif mi?
- Typewriter tamamlandı mı? (Console'da log kontrol edin)

### Metin anında görünüyor
- Typewriter Effect field'ı boşsa metin anında gösterilir
- Bu normal davranıştır (typewriter opsiyonel)

## ✨ Avantajlar

1. **Engaging**: Daktilo efekti kullanıcı dikkatini çeker
2. **Flexible**: Tap to skip ile kullanıcı kontrolü
3. **Automated**: Auto-click ile hands-free tutorial
4. **Modular**: Typewriter opsiyonel, istenirse kapatılabilir
5. **Reusable**: Tüm ability tutorial'larında kullanılabilir

Sistem hazır! 🎉
