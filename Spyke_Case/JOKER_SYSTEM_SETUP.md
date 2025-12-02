# 🎮 Kategori Bazlı Joker Sistemi - Kurulum Kılavuzu

## 📋 Sistem Özeti

**ÖNEMLİ**: Jokerler kategori bazlı çalışır!
- **Vergi Kategorisi**: Sadece EN SON satın alınan vergi jokeri aktiftir
- **Tamir Kategorisi**: Sadece EN SON satın alınan tamir jokeri aktiftir
- **Farklı Kategoriler**: Aynı anda aktif olabilir (Vergi + Tamir)

### 💰 Vergi Azaltma Jokerları (Kategori 1)

1. **Double Bookkeeping** (10 ⭐)
   - 10% vergi oranı
   - 10 oyun süresi
   - Gizli borçlanma ile vergi azaltma

2. **Bribery** (10 ⭐)
   - 0% vergi
   - 5 oyun süresi
   - Denetçiye rüşvet ile vergi muafiyeti

3. **High Operating Expenses** (30 ⭐)
   - 0% vergi
   - 20 oyun süresi
   - Şişirilmiş giderler ile vergi muafiyeti

4. **Offshore Accounts** (100 ⭐)
   - 5% vergi
   - **SINIRSIZ** (kalıcı)
   - Offshore şirketler ile düşük vergi

### 🛡️ Kaza/Tamir Jokerları (Kategori 2)

5. **Collision Insurance** (10 ⭐)
   - 0 coin tamir maliyeti
   - 5 oyun süresi
   - Kaza sigortası ile ücretsiz tamir

6. **Own Repair Station** (100 ⭐)
   - 100 coin sabit tamir maliyeti
   - **SINIRSIZ** (kalıcı)
   - Kendi tamir istasyonu (500 yerine 100)

---

## 🔄 Kategori Sistemi Nasıl Çalışır?

### Örnek Senaryo 1: Vergi Kategorisi

```
1. Offshore Accounts satın al (100⭐)
   → Aktif: Offshore Accounts (5% vergi)
   
2. Double Bookkeeping satın al (10⭐)
   → Aktif: Double Bookkeeping (10% vergi)
   → Deaktif: Offshore Accounts (artık etkin değil!)
   
3. Bribery satın al (10⭐)
   → Aktif: Bribery (0% vergi)
   → Deaktif: Double Bookkeeping, Offshore Accounts
```

**Sonuç**: Sadece Bribery aktif, diğerleri "Inactive (Replaced)" durumunda.

### Örnek Senaryo 2: Tamir Kategorisi

```
1. Own Repair Station satın al (100⭐)
   → Aktif: Own Repair Station (100 coin tamir)
   
2. Collision Insurance satın al (10⭐)
   → Aktif: Collision Insurance (0 coin tamir)
   → Deaktif: Own Repair Station (artık etkin değil!)
```

**Sonuç**: Sadece Collision Insurance aktif.

### Örnek Senaryo 3: Farklı Kategoriler

```
1. Bribery satın al (10⭐) - Vergi kategorisi
   → Aktif: Bribery (0% vergi)
   
2. Own Repair Station satın al (100⭐) - Tamir kategorisi
   → Aktif: Bribery (0% vergi) + Own Repair Station (100 coin tamir)
   
Her iki joker de aktif çünkü farklı kategorilerde!
```

---

## 🎨 Card Durum Gösterimi

### Renk Sistemi

- **Yeşil Arka Plan**: Aktif joker (ACTIVE)
- **Gri Arka Plan**: Sahip olunan ama deaktif joker (Inactive - Replaced)
- **Beyaz Arka Plan**: Sahip olunmayan joker (Not Owned)

### Text Renkleri

- **Yeşil Text**: Aktif joker veya satın alınabilir
- **Kırmızı Text**: Yetersiz yıldız (satın alınamaz)
- **Gri Text**: Deaktif veya sahip olunmayan

### Durum Mesajları

- **"ACTIVE (Unlimited)"**: Sınırsız ve aktif joker
- **"ACTIVE: X games"**: X oyun kaldı ve aktif
- **"Inactive (Replaced)"**: Sahip olunan ama başka joker tarafından değiştirilmiş
- **"Not Owned"**: Hiç satın alınmamış

### Buy Button Durumu

- **Aktif (Yeşil)**: Yeterli yıldız var, satın alınabilir
- **Aktif (Yeşil)**: Deaktif joker, tekrar aktif etmek için satın alınabilir
- **Pasif (Gri)**: Yetersiz yıldız VEYA zaten aktif joker

---

## 🔧 Kurulum Adımları

### 1. GameObjects Oluştur

Unity Hierarchy'de:
```
├─ GameEconomy (Empty GameObject)
│  ├─ GameEconomy.cs
│  └─ AbilityUsageTracker.cs
└─ JokerSystem (Empty GameObject)
   └─ JokerSystem.cs (DontDestroyOnLoad otomatik)
```

### 2. JokerShopPanel Oluştur

UI Canvas altında:
```
JokerShopPanel (Panel)
├─ Header
│  ├─ TotalStarsText (TextMeshProUGUI) - "⭐ 0"
│  └─ CloseButton (Button)
└─ CardsContainer (Vertical Layout Group)
   ├─ JokerCard1 (Panel + JokerCard.cs) - Double Bookkeeping
   ├─ JokerCard2 (Panel + JokerCard.cs) - Bribery
   ├─ JokerCard3 (Panel + JokerCard.cs) - High Operating Expenses
   ├─ JokerCard4 (Panel + JokerCard.cs) - Offshore Accounts
   ├─ JokerCard5 (Panel + JokerCard.cs) - Collision Insurance
   └─ JokerCard6 (Panel + JokerCard.cs) - Own Repair Station
```

**Her JokerCard için**:
```
JokerCard (Panel)
├─ NameText (TextMeshProUGUI)
├─ CostText (TextMeshProUGUI)
├─ EffectText (TextMeshProUGUI)
├─ StatusText (TextMeshProUGUI)
├─ BuyButton (Button)
└─ CardBackground (Image)
```

### 3. Referansları Bağla

**JokerShopPanel Inspector**:
- Total Stars Text → TotalStarsText
- Close Button → CloseButton
- Joker Cards → 6 JokerCard'ı sırayla ekle

**Her JokerCard Inspector**:
- Name Text → NameText
- Cost Text → CostText
- Effect Text → EffectText
- Status Text → StatusText
- Buy Button → BuyButton
- Card Background → CardBackground

---

## 🎮 Kullanım Akışı

### Oyuncu Perspektifi

1. **Yıldız Toplama**: Level'leri tamamla (max 3⭐/level)
2. **Joker Mağazası**: JokerShopPanel'i aç
3. **İlk Joker**: Bir vergi jokeri satın al (örn: Offshore Accounts)
   - Aktif olur, yeşil arka plan
4. **İkinci Joker**: Başka bir vergi jokeri satın al (örn: Bribery)
   - Bribery aktif olur (yeşil)
   - Offshore Accounts deaktif olur (gri - "Inactive (Replaced)")
5. **Farklı Kategori**: Bir tamir jokeri satın al (örn: Own Repair Station)
   - Hem Bribery hem Own Repair Station aktif (farklı kategoriler)
6. **Tekrar Aktif Etme**: Deaktif joker'i tekrar satın al
   - Tekrar aktif olur, diğeri deaktif olur

### Geliştirici Perspektifi

```csharp
// Joker satın alma (kategori bazlı)
JokerSystem.Instance.BuyJoker(JokerType.Bribery);
// → Önceki vergi jokeri otomatik deaktif olur

// Joker durumu kontrolü
bool isActive = JokerSystem.Instance.IsJokerActive(JokerType.Bribery);
// → true sadece aktif joker ise

// Vergi oranı al (otomatik - aktif joker'e göre)
float taxRate = JokerSystem.Instance.GetTaxRate();

// Kaza cezası al (otomatik - aktif joker'e göre)
int crashPenalty = JokerSystem.Instance.GetCrashPenalty(500);
```

---

## 🐛 Test Senaryoları

### 1. Kategori Değiştirme Testi
1. Offshore Accounts satın al (100⭐)
2. Card yeşil olmalı, "ACTIVE (Unlimited)"
3. Bribery satın al (10⭐)
4. Bribery yeşil, Offshore Accounts gri olmalı
5. Offshore Accounts "Inactive (Replaced)" göstermeli

### 2. Farklı Kategori Testi
1. Bribery satın al (vergi)
2. Own Repair Station satın al (tamir)
3. Her iki card de yeşil olmalı
4. Level oyna: 0% vergi + 100 coin tamir

### 3. Tekrar Aktif Etme Testi
1. Offshore Accounts satın al
2. Bribery satın al (Offshore deaktif olur)
3. Offshore Accounts'u tekrar satın al
4. Offshore aktif, Bribery deaktif olmalı

### 4. Süre Azaltma Testi
1. Bribery satın al (5 oyun)
2. Her level başında sayaç azalmalı
3. 0'a ulaşınca otomatik deaktif
4. Card "Not Owned" olmalı

### 5. Sınırsız Joker Testi
1. Offshore Accounts satın al
2. "ACTIVE (Unlimited)" göstermeli
3. Sonsuz level oyna
4. Hiç deaktif olmamalı (başka joker alınmadıkça)

---

## ✅ Checklist

- [ ] GameEconomy GameObject oluşturuldu
- [ ] JokerSystem GameObject oluşturuldu
- [ ] JokerShopPanel oluşturuldu
- [ ] 6 JokerCard oluşturuldu (sırayla)
- [ ] Tüm referanslar bağlandı
- [ ] Kategori sistemi test edildi
- [ ] Deaktif jokerler doğru gösteriliyor

---

## 📝 Önemli Notlar

- ⚠️ **Kategori Kuralı**: Aynı kategoride sadece 1 joker aktif olabilir
- ⚠️ **Otomatik Deaktivasyon**: Yeni joker alınca eski otomatik deaktif olur
- ⚠️ **Tekrar Satın Alma**: Deaktif joker'i tekrar satın alarak aktif edebilirsiniz
- ⚠️ **Farklı Kategoriler**: Vergi + Tamir jokerleri aynı anda aktif olabilir
- ⚠️ **Görsel Feedback**: Yeşil=Aktif, Gri=Deaktif, Beyaz=Yok
- ⚠️ **Yıldız Sistemi**: Yıldızlar harcandığında kaybolmaz

---

## 🎯 Strateji İpuçları

### Vergi Optimizasyonu
- **Başlangıç**: Bribery (10⭐) - 0% vergi, 5 oyun
- **Orta Dönem**: High Operating Expenses (30⭐) - 0% vergi, 20 oyun
- **Son Dönem**: Offshore Accounts (100⭐) - 5% vergi, sınırsız

### Tamir Optimizasyonu
- **Başlangıç**: Collision Insurance (10⭐) - 0 coin, 5 oyun
- **Son Dönem**: Own Repair Station (100⭐) - 100 coin, sınırsız

### Kombine Strateji
1. Bribery + Collision Insurance = 20⭐ (0% vergi + 0 coin tamir)
2. High Operating Expenses + Own Repair Station = 130⭐ (0% vergi + 100 coin tamir)
3. Offshore Accounts + Own Repair Station = 200⭐ (5% vergi + 100 coin tamir, sınırsız)

---

Başarılar! 🎮
