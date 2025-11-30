# 🪙 Royal Match Tarzı Coin Animasyon Sistemi - Kurulum

## 📋 Sistem Özellikleri

### ✨ Kazanma Animasyonu (Passenger, vb.)
1. **Coin Sprite'ları:** Kazanılan yerden 5-10 adet dolar ikonu uçar
2. **Pop Animasyonu:** Her coin büyür → küçülür → coin UI'a gider
3. **Text Feedback:** "+20" yeşil text → 1 saniye bekler → yukarı hareket eder → fade out
4. **Sıralı Spawn:** Coin'ler arka arkaya spawn olur (0.05s gecikme)

### 💸 Harcama Animasyonu (Ability, vb.)
1. **Text Feedback:** "-100" kırmızı text → 1 saniye bekler → yukarı hareket eder → fade out
2. **Coin UI Shake:** Coin UI sarsilir (hyper-casual tarzı)
3. **Renk Değişimi:** Coin text kırmızıya döner, sonra normale döner
4. **Scale Efekti:** Küçülüp büyüme animasyonu
5. **Basit ve Net:** Kullanıcı para kaybettiğini hemen hisseder

## 🎯 Oluşturulan Dosyalar

1. **`CoinSpriteAnimation.cs`** - Tek bir coin sprite'ının animasyonu
2. **`CoinAnimationManager.cs`** - Tüm sistemi yöneten manager
3. **`FloatingCoinText.cs`** - Text feedback (güncellendi)
4. **`GameEconomy.cs`** - Yeni metotlar eklendi
5. **`CoinUIShakeEffect.cs`** - Coin UI shake efekti
6. **`CoinObjectPool.cs`** - Object pooling sistemi (performans optimizasyonu)
7. **`UberManager.cs`** - Uber pozisyonundan animasyon gösterimi

## 🔧 Unity Kurulum Adımları

### 1️⃣ Coin Sprite Prefab Oluştur

**a) Coin Image Oluştur:**
1. Hierarchy → Canvas → sağ tık → **UI → Image**
2. İsim: `CoinSprite`

**b) Sprite Ayarları:**
1. **Image Component:**
   - Source Image: Dolar ikonu sprite'ı (yoksa geçici olarak Unity default sprite)
   - Preserve Aspect: ✅ Aktif
   - Raycast Target: ❌ Pasif

2. **RectTransform:**
   - Width: 40
   - Height: 40

**c) Script Ekle:**
1. Add Component → `CoinSpriteAnimation`

**d) Prefab'a Çevir:**
1. `Assets/Prefabs/UI/CoinSprite` olarak kaydet
2. Hierarchy'den sil

### 2️⃣ Floating Text Prefab Güncelle

Eğer daha önce oluşturduysanız, aynı prefab kullanılabilir. Yoksa:

1. Hierarchy → Canvas → **UI → Text - TextMeshPro**
2. İsim: `FloatingCoinText`
3. Settings:
   - Font Size: 100 (büyük ve belirgin)
   - Alignment: Center
   - Outline: Aktif (kalınlık: 2)
4. Add Component → `FloatingCoinText`
5. Add Component → `CanvasGroup`
6. Prefab'a çevir: `Assets/Prefabs/UI/FloatingCoinText`

### 3️⃣ CoinAnimationManager Kurulumu

**a) GameObject Oluştur:**
1. Hierarchy → Canvas altında → Create Empty
2. İsim: `CoinAnimationManager`

**b) Script ve Referanslar:**
1. Add Component → `CoinAnimationManager`
2. Inspector'da:
   - **Coin Sprite Prefab:** `CoinSprite` prefab'ını sürükle
   - **Floating Text Prefab:** `FloatingCoinText` prefab'ını sürükle
   - **Coin UI Target:** Coin text RectTransform'unu sürükle
   - **Coins Per Unit:** 5 (her 20 coin için 5 sprite)
   - **Max Coins:** 10 (maksimum sprite sayısı)
   - **Spread Radius:** 50 (coin'lerin yayılma yarıçapı)

### 4️⃣ Coin UI Shake Effect Kurulumu

**a) Coin UI Container Hazırla:**
1. Hierarchy'de coin text'inizi bulun (örn: `Canvas/CoinDisplay/CoinText`)
2. Eğer yoksa, coin text'in parent'ı olacak bir Empty GameObject oluşturun
3. İsim: `CoinUIContainer`
4. Coin text'i bu container'ın altına alın

**b) Shake Effect Script Ekle:**
1. `CoinUIContainer`'ı seç
2. Add Component → `CoinUIShakeEffect`
3. Inspector'da:
   - **Coin UI Container:** Kendi RectTransform'unu sürükle (otomatik bulunacak)
   - **Coin Text:** Coin text'i sürükle (otomatik bulunacak)
   - **Shake Duration:** 0.5
   - **Shake Strength:** 20
   - **Normal Color:** Beyaz
   - **Lose Color:** Kırmızı

**c) CoinAnimationManager'a Bağla:**
1. `CoinAnimationManager`'ı seç
2. Inspector'da:
   - **Coin UI Shake Effect:** `CoinUIContainer` GameObject'ini sürükle

### 5️⃣ Object Pooling Kurulumu (Performans Optimizasyonu)

**a) Pool GameObject Oluştur:**
1. Hierarchy → Canvas altında → Create Empty
2. İsim: `CoinObjectPool`

**b) Script ve Ayarlar:**
1. Add Component → `CoinObjectPool`
2. Inspector'da:
   - **Coin Sprite Prefab:** `CoinSprite` prefab'ını sürükle
   - **Floating Text Prefab:** `FloatingCoinText` prefab'ını sürükle
   - **Initial Coin Sprite Pool Size:** 20 (başlangıç pool boyutu)
   - **Initial Text Pool Size:** 10 (başlangıç text pool boyutu)

**Not:** Pool sistemi otomatik çalışır. Prefab'lar spawn/destroy edilmek yerine activate/deactivate edilir.

### 6️⃣ Dolar Sprite Ekle (Opsiyonel)

Eğer dolar ikonu yoksa:
1. Google'dan "coin icon png" ara
2. Transparent PNG indir
3. Unity'ye import et
4. `CoinSprite` prefab'ında Image → Source Image'e ata

## 🎮 Test Etme

### Passenger Tamamlandığında:
```
✅ Passenger pozisyonundan 5 coin sprite uçmalı
✅ "+20" yeşil text görünmeli
✅ Coin'ler yukarıdaki coin UI'a gitmeli
```

### Uber Çağrıldığında:
```
✅ Wagon pozisyonundan 5 coin sprite uçmalı
✅ "-100" kırmızı text görünmeli
✅ Coin'ler yukarıdaki coin UI'a gitmeli
```

### Ability Satın Alındığında:
```
✅ "-100" kırmızı text görünmeli (büyük ve belirgin)
✅ Ability butonunun yakınında belirmeli
✅ Coin UI sarsılmalı (shake efekti)
✅ Coin text kırmızıya dönmeli, sonra beyaza dönmeli
✅ Coin UI küçülüp büyümeli
✅ Coin sprite animasyonu YOK (sadece text ve shake)
```

## 🎨 Özelleştirme

### Coin Sayısını Değiştir
`CoinAnimationManager` Inspector:
- **Coins Per Unit:** 10 → Her 10 coin için 1 sprite
- **Max Coins:** 30 → Maksimum 30 sprite

### Animasyon Hızını Değiştir
`CoinSpriteAnimation.cs`:
```csharp
[SerializeField] private float popDuration = 0.15f;      // Pop süresi
[SerializeField] private float moveDuration = 0.6f;      // Hareket süresi
[SerializeField] private float delayBetweenCoins = 0.05f; // Coin'ler arası gecikme
```

### Yayılma Alanını Değiştir
`CoinAnimationManager` Inspector:
- **Spread Radius:** 100 → Daha geniş alan

## 🐛 Sorun Giderme

### Coin'ler görünmüyor
- ✅ CoinSprite prefab'ı atandı mı?
- ✅ Sprite'da Image component var mı?
- ✅ Canvas Render Mode doğru mu?

### Coin'ler yanlış yere gidiyor
- ✅ Coin UI Target doğru atandı mı?
- ✅ RectTransform mi, Transform mu kontrol et

### Animasyon çok hızlı/yavaş
- ✅ CoinSpriteAnimation prefab'ındaki değerleri ayarla

## 📝 Kod Örnekleri

### Manuel Coin Kazanma
```csharp
if (CoinAnimationManager.Instance != null)
{
    CoinAnimationManager.Instance.ShowCoinGain(50, transform.position);
}
```

### Manuel Coin Harcama
```csharp
if (CoinAnimationManager.Instance != null)
{
    CoinAnimationManager.Instance.ShowCoinSpend(100, buttonTransform.position);
}
```

## 🎯 Sistem Akışı

### Kazanma:
```
Passenger Pozisyonu → Coin Sprite'lar Spawn → Pop Animasyon → Coin UI'a Git
                   → Text "+20" Göster → Fade Out
```

### Harcama:
```
UI Pozisyonu → Dünya Pozisyonuna Çevir → Text "-100" Göster → Fade Out
(Coin sprite animasyonu yok)
```

---

**Not:** Royal Match tarzı smooth animasyonlar için DOTween kullanılıyor. Projenizde DOTween yüklü olmalı.
