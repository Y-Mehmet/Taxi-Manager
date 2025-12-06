# Universal Pathfinding Tutorial - Otomatik Döngü Sistemi

Bu tutorial, tamamen otomatik çalışır ve skip edilene kadar sürekli tekrar eder.

## 🔄 Tam Döngü (Sürekli Tekrar)

```
1. Çarpışma Animasyonu:
   → Alttaki araba yukarı hareket
   → Üstteki arabaya çarpar
   → Üstteki araba sallanır (kaza efekti)
   
2. Geri Dönüş:
   → Alttaki araba başlangıç pozisyonuna döner
   
3. Buton Tıklama Animasyonu:
   → El görseli görünür
   → El butona gider ve tıklar (scale animasyon)
   → El alttaki arabaya gider ve tıklar
   → El gizlenir
   
4. Pathfinding Animasyonu:
   → Sola dön (-90°)
   → Sola git (150 birim)
   → Yukarı dön (0°)
   → Yukarı git (stop yüksekliğine)
   → Sağa dön (90°)
   → Stop'a git ve park et
   
5. Park:
   → 5 saniye park halinde bekle
   
6. Reset:
   → Tüm pozisyonlar başlangıca ışınlanır
   → 1 saniye bekle
   
7. Tekrar Başla (1'e dön) ♻️
```

## 📋 Unity Setup

### Hierarchy (Basitleştirilmiş):

```
UniversalPathfindingTutorialPanel (Prefab)
├── TutorialManager (Empty)
│   └── UniversalPathfindingTutorial (Script)
│       - Top Car: TopCar
│       - Bottom Car: BottomCar
│       - Stop Image: StopImage
│       - Hand Image: HandImage
│       - Button Transform: UniversalPathButton
│       - Car Move Speed: 200
│       - Rotation Speed: 180
│       - Shake Amount: 10
│       - Shake Duration: 0.3
│       - Park Duration: 5
│       - Hand Click Duration: 0.3
│       - Cost Text: CostText
├── DescriptionText + TypewriterEffect
├── AnimationArea (Empty)
│   ├── TopCar (Image)
│   │   - Position: Canvas'ta istediğiniz yere koyun
│   ├── BottomCar (Image)
│   │   - Position: Canvas'ta istediğiniz yere koyun
│   └── StopImage (Image)
│       - Position: Canvas'ta istediğiniz yere koyun
├── CostText (TextMeshPro)
├── HandImage (Image)
│   - Active: ❌ False (script otomatik açar)
└── UniversalPathButton + AbilityTutorialButton
    - Tutorial Behaviour: TutorialManager
    - Enable Auto Click: ❌ FALSE ← ÖNEMLİ!
```

## ✨ Önemli Değişiklikler

### 1. Pozisyonlar Otomatik
```csharp
// ❌ ESKI: Inspector'da manuel gir
[SerializeField] private Vector3 bottomCarStartPos;

// ✅ YENİ: Otomatik Start()'ta kaydedilir
private Vector3 bottomCarStartPos; // private!

void Start()
{
    bottomCarStartPos = bottomCar.localPosition; // Otomatik!
}
```

### 2. Auto-Click KAPALI
```
AbilityTutorialButton:
- Enable Auto Click: ❌ FALSE

Çünkü:
- Tutorial zaten otomatik çalışıyor
- Manuel tıklama gerekmiyor
- El animasyonu script tarafından kontrol ediliyor
```

### 3. Sürekli Döngü
```csharp
while (!isSkipped)
{
    // Tüm animasyon adımları
    // Skip edilene kadar sürekli tekrar eder
}
```

### 4. Skip Fonksiyonu
```csharp
public void Skip()
{
    isSkipped = true;
    // Döngü durur
}
```

## 🎯 Pozisyon Yerleştirme

### Canvas'ta İstediğiniz Yere Koyun:

```
Örnek 1 (Merkez):
TopCar: (0, 200, 0)
BottomCar: (0, -200, 0)
StopImage: (400, 200, 0)

Örnek 2 (Sol):
TopCar: (-400, 100, 0)
BottomCar: (-400, -300, 0)
StopImage: (200, 100, 0)

Örnek 3 (Sağ):
TopCar: (400, 150, 0)
BottomCar: (400, -250, 0)
StopImage: (800, 150, 0)

Script otomatik olarak bu pozisyonları kaydeder!
```

## 🔧 Script Ayarları

### Sadece Bunları Ayarlayın:

```
Car Images:
- Top Car: TopCar (Transform)
- Bottom Car: BottomCar (Transform)
- Stop Image: StopImage (Transform)

Hand Animation:
- Hand Image: HandImage (GameObject)
- Button Transform: UniversalPathButton (Transform)

Animation Settings:
- Car Move Speed: 200
- Rotation Speed: 180
- Shake Amount: 10
- Shake Duration: 0.3
- Park Duration: 5
- Hand Click Duration: 0.3

Cost Display:
- Cost Text: CostText
```

### ❌ AYARLAMA:
```
- Bottom Car Start Pos (private, otomatik)
- Top Car Start Pos (private, otomatik)
- Stop Start Pos (private, otomatik)
```

## 🎬 El Animasyonu Detayları

### Buton Tıklama:
```
1. El görünür
2. El butonun pozisyonuna ışınlanır
3. Scale down (0.8x) - 0.15s
4. Scale up (1.0x) - 0.15s
5. 0.2s bekle
```

### Araba Tıklama:
```
6. El arabanın pozisyonuna ışınlanır
7. Scale down (0.8x) - 0.15s
8. Scale up (1.0x) - 0.15s
9. El gizlenir
```

## ⏱️ Timeline

```
0s:   Çarpışma başlar
1s:   Çarpışma tamamlandı, geri dönüş
2s:   Başlangıç pozisyonuna döndü
2.5s: El butona tıklar
3s:   El arabaya tıklar
3.5s: Pathfinding başlar (sola dön)
4s:   Sola git
5s:   Yukarı dön
6s:   Yukarı git
7s:   Sağa dön
8s:   Stop'a git
9s:   Park et
14s:  Park bitti (5s)
15s:  Reset, 1s bekle
16s:  Tekrar başla (0s'e dön) ♻️
```

## 🐛 Sorun Giderme

### Döngü başlamıyor
- Transform referansları dolu mu?
- Start() çağrıldı mı?
- Console'da error var mı?

### Pozisyonlar yanlış
- Canvas'ta doğru yerleştirdiniz mi?
- Script Start()'ta pozisyonları kaydetti mi?
- Console'da "Positions saved" log'u var mı?

### El görünmüyor
- Hand Image aktif mi? (başlangıçta false olmalı)
- Button Transform referansı dolu mu?

### Döngü durmuyor
- Skip() çağrıldı mı?
- isSkipped = true oldu mu?

## ✅ Checklist

- ✅ TopCar, BottomCar, StopImage Canvas'a yerleştir
- ✅ HandImage ekle (başlangıçta false)
- ✅ UniversalPathfindingTutorial script'ine referansları ata
- ✅ AbilityTutorialButton'da Enable Auto Click: FALSE
- ✅ Test et - otomatik döngü başlamalı

## 📝 Önemli Notlar

### 1. Otomatik Pozisyon
```
✅ Canvas'ta istediğiniz yere koyun
✅ Script otomatik kaydeder
✅ Inspector'da ayarlamaya gerek yok
```

### 2. Auto-Click Kapalı
```
✅ Tutorial otomatik çalışır
✅ Manuel tıklama yok
✅ El animasyonu script kontrolünde
```

### 3. Sürekli Döngü
```
✅ Skip edilene kadar tekrar eder
✅ Next button veya Skip button ile durur
✅ AbilityTutorialManager.OnNextButtonClicked() → Skip()
```

Sistem hazır! Tamamen otomatik çalışıyor! 🚗💨♻️
