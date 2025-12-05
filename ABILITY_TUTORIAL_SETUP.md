# Ability Tutorial Sahnesi Kurulum Rehberi

Bu rehber, ability'leri tanıtan öğretici sahneyi nasıl oluşturacağınızı adım adım açıklar.

## 1. Yeni Sahne Oluşturma

1. Unity'de **File > New Scene** ile yeni bir sahne oluşturun
2. Sahneyi **"AbilityTutorial"** olarak kaydedin
3. **File > Build Settings** açın
4. **AbilityTutorial** sahnesini **Build Index 2** olarak ekleyin (sıralama: MainMenu=0, AllLevel=1, AbilityTutorial=2)

## 2. Sahne Yapısı

### Canvas Oluşturma
```
Canvas (Screen Space - Overlay)
├── TutorialManager (Empty GameObject)
├── Panel_AddStop (Tutorial Panel 1)
│   ├── Title (TextMeshPro - "Add Stop Ability")
│   ├── Description (TextMeshPro - Açıklama metni)
│   ├── Icon (Image - Ability ikonu)
│   └── Example (Image/Animation - Kullanım örneği)
├── Panel_RemoveWagons (Tutorial Panel 2)
│   ├── Title (TextMeshPro - "Remove Wagons Ability")
│   ├── Description (TextMeshPro - Açıklama metni)
│   ├── Icon (Image - Ability ikonu)
│   └── Example (Image/Animation - Kullanım örneği)
├── Panel_UniversalPathfinding (Tutorial Panel 3)
│   ├── Title (TextMeshPro - "Universal Pathfinding Ability")
│   ├── Description (TextMeshPro - Açıklama metni)
│   ├── Icon (Image - Ability ikonu)
│   └── Example (Image/Animation - Kullanım örneği)
├── Panel_ShuffleColors (Tutorial Panel 4)
│   ├── Title (TextMeshPro - "Shuffle Colors Ability")
│   ├── Description (TextMeshPro - Açıklama metni)
│   ├── Icon (Image - Ability ikonu)
│   └── Example (Image/Animation - Kullanım örneği)
├── NavigationPanel (Alt kısım)
│   ├── PreviousButton (Button - "Geri")
│   ├── NextButton (Button - "İleri")
│   ├── PageIndicator (TextMeshPro - "1/4")
│   ├── SkipButton (Button - "Bir Daha Gösterme")
│   └── StartGameButton (Button - "Oyuna Başla")
```

## 3. AbilityTutorialManager Kurulumu

1. **TutorialManager** GameObject'ine **AbilityTutorialManager** scriptini ekleyin
2. Inspector'da şu alanları doldurun:

### Tutorial Panels
- **Size**: 4
- **Element 0**: Panel_AddStop
- **Element 1**: Panel_RemoveWagons
- **Element 2**: Panel_UniversalPathfinding
- **Element 3**: Panel_ShuffleColors

### Navigation Buttons
- **Next Button**: NextButton referansı
- **Previous Button**: PreviousButton referansı
- **Skip Button**: SkipButton referansı
- **Start Game Button**: StartGameButton referansı

### UI Elements
- **Page Indicator Text**: PageIndicator TextMeshPro referansı

## 4. Ability Açıklamaları (Örnek Metinler)

### Add Stop Ability
**Başlık**: "Yeni Durak Ekle"
**Açıklama**: "Bu yetenek ile haritaya yeni bir durak ekleyebilirsiniz. Yeni durak, yolcuların daha hızlı binmesini sağlar ve trafik sıkışıklığını azaltır."

### Remove Wagons Ability
**Başlık**: "Vagonları Kaldır"
**Açıklama**: "Belirli bir yolcu grubunu haritadan kaldırır. Zor durumlarda kullanarak yer açabilirsiniz."

### Universal Pathfinding Ability
**Başlık**: "Evrensel Yol Bulma"
**Açıklama**: "Bir yolcu grubunun herhangi bir durağa gitmesini sağlar. Kilitli kaldığınızda çok işe yarar!"

### Shuffle Colors Ability
**Başlık**: "Renkleri Karıştır"
**Açıklama**: "Vagonların renklerini karıştırarak yeni eşleşme fırsatları yaratır."

## 5. Buton Ayarları

### Previous Button
- İlk panelde **interactable = false** olacak (script otomatik ayarlar)

### Next Button
- Son panelde **gizlenecek** (script otomatik ayarlar)

### Skip Button
- Her panelde görünür
- Tıklandığında tutorial bir daha gösterilmez
- Direkt level sahnesine gider

### Start Game Button
- Sadece son panelde görünür (script otomatik ayarlar)
- Tıklandığında tutorial tamamlanmış sayılır
- Level sahnesine gider

## 6. Test Etme

1. **Build Settings**'de sahne sıralamasını kontrol edin:
   - 0: MainMenu
   - 1: AllLevel
   - 2: AbilityTutorial

2. Oyunu başlatın ve bir level seçin
3. İlk kez level yüklendiğinde tutorial sahnesi açılmalı
4. "Bir Daha Gösterme" butonuna basın
5. Tekrar level seçtiğinizde tutorial atlanmalı

## 7. Tutorial'ı Sıfırlama (Test için)

Tutorial'ı tekrar görmek isterseniz:
1. `savegame.json` dosyasını bulun
2. `"isAbilityTutorialCompleted": true` satırını `false` yapın
3. Veya save dosyasını silin

## 8. Özelleştirme

- **Animasyonlar**: Her panele DOTween animasyonları ekleyebilirsiniz
- **Ses Efektleri**: Buton tıklamalarına ses ekleyebilirsiniz
- **Daha Fazla Panel**: `tutorialPanels` array'ine istediğiniz kadar panel ekleyebilirsiniz
- **Video**: Ability kullanım örnekleri için video player ekleyebilirsiniz

## Kod Özeti

### SceneManager.cs
- `LoadLevelSceene()` metodu tutorial durumunu kontrol eder
- Tutorial tamamlanmamışsa önce tutorial sahnesini yükler

### AbilityTutorialManager.cs
- Panel navigasyonunu yönetir
- "Bir daha gösterme" tercihini kaydeder
- Tutorial tamamlandığında level sahnesine geçer

### SaveGameData.cs
- `isAbilityTutorialCompleted` field'i tutorial durumunu saklar
