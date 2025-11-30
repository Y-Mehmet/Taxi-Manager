# Tutorial UI Assets Rehberi

## 🎨 Gerekli Sprite'lar

Tutorial sistemi için aşağıdaki sprite'lara ihtiyacınız var:

### 1. Hand Icon (El İkonu) ✋

**Boyut**: 256x256 px (veya 512x512 px daha yüksek kalite için)
**Format**: PNG (transparan arka plan)
**Stil**: Beyaz veya açık renkli, parmak işaret eden el

**Nereden Bulabilirsiniz:**
- [Flaticon](https://www.flaticon.com/search?word=hand%20pointer) - Ücretsiz ve premium ikonlar
- [Icons8](https://icons8.com/icons/set/hand-pointer) - Ücretsiz ikonlar
- [Unity Asset Store](https://assetstore.unity.com/packages/2d/gui/icons/simple-button-set-01-153979) - UI paketleri

**Önerilen İkonlar:**
1. Parmak işaret eden el (👆 benzeri)
2. Tıklama efekti olan el
3. Animasyonlu el (sprite sheet olarak)

**Dosya Adı**: `hand_pointer.png`
**Konum**: `Assets/Sprites/UI/Tutorial/`

### 2. Circle Highlight (Vurgulama Dairesi) ⭕

**Boyut**: 512x512 px
**Format**: PNG (transparan arka plan)
**Stil**: Yumuşak kenarlar, gradient efekti

**Unity'de Oluşturma:**
Unity'nin built-in sprite'larını kullanabilirsiniz:
1. Hierarchy'de UI → Image oluşturun
2. Inspector'da Source Image → `UI/Skin/Knob` veya `UI/Skin/UISprite`
3. Veya kendi circle sprite'ınızı oluşturun

**Photoshop/GIMP ile Oluşturma:**
1. 512x512 px yeni dosya oluşturun
2. Ellipse Tool ile mükemmel daire çizin
3. Gradient Overlay ekleyin (merkez açık, kenarlar koyu)
4. Outer Glow efekti ekleyin
5. PNG olarak export edin

**Dosya Adı**: `circle_highlight.png`
**Konum**: `Assets/Sprites/UI/Tutorial/`

## 🖼️ Sprite Import Ayarları

Her sprite için Unity'de:

1. Sprite'ı seçin
2. Inspector'da:
   - **Texture Type**: `Sprite (2D and UI)`
   - **Sprite Mode**: `Single`
   - **Pixels Per Unit**: `100`
   - **Filter Mode**: `Bilinear`
   - **Compression**: `None` veya `High Quality`
   - **Max Size**: `2048` (yüksek kalite için)

3. `Apply` butonuna tıklayın

## 🎨 Alternatif: Kod ile Sprite Oluşturma

Eğer sprite bulamazsanız, Unity'de kod ile basit şekiller oluşturabilirsiniz:

### Circle Sprite Oluşturma

```csharp
// Bu kodu bir Editor script olarak kullanabilirsiniz
using UnityEngine;
using UnityEditor;

public class SpriteGenerator : MonoBehaviour
{
    [MenuItem("Tools/Generate Circle Sprite")]
    static void GenerateCircle()
    {
        int size = 512;
        Texture2D texture = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];
        
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f - 10f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                
                if (distance < radius)
                {
                    // İç kısım - gradient
                    float alpha = 1f - (distance / radius) * 0.3f;
                    pixels[y * size + x] = new Color(1f, 0.8f, 0f, alpha);
                }
                else if (distance < radius + 5f)
                {
                    // Kenar - yumuşak geçiş
                    float alpha = 1f - ((distance - radius) / 5f);
                    pixels[y * size + x] = new Color(1f, 0.8f, 0f, alpha);
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        // Sprite olarak kaydet
        byte[] bytes = texture.EncodeToPNG();
        System.IO.File.WriteAllBytes(Application.dataPath + "/Sprites/UI/Tutorial/circle_highlight.png", bytes);
        AssetDatabase.Refresh();
        
        Debug.Log("Circle sprite created!");
    }
}
```

## 📦 Hazır UI Paketleri

Eğer hızlı bir çözüm istiyorsanız, Unity Asset Store'dan ücretsiz UI paketleri indirebilirsiniz:

1. **Simple Button Set 01** (Ücretsiz)
   - Link: https://assetstore.unity.com/packages/2d/gui/icons/simple-button-set-01-153979
   - İçerik: Temel UI ikonları

2. **UI Pack** (Ücretsiz)
   - Link: https://assetstore.unity.com/packages/2d/gui/ui-pack-144877
   - İçerik: Kapsamlı UI elementleri

3. **Casual Game UI** (Ücretsiz)
   - Link: https://assetstore.unity.com/packages/2d/gui/casual-game-ui-201381
   - İçerik: Hyper-casual oyunlar için UI

## 🎨 Renk Paleti Önerileri

Tutorial UI için uyumlu renkler:

### Highlight Circle
- **Sarı**: `#FFD700` (Gold)
- **Turuncu**: `#FF8C00` (Dark Orange)
- **Yeşil**: `#00FF00` (Lime) - Başarı için
- **Mavi**: `#00BFFF` (Deep Sky Blue) - Bilgi için

### Hand Icon
- **Beyaz**: `#FFFFFF` - Evrensel, her arka plana uyar
- **Açık Gri**: `#E0E0E0` - Daha yumuşak görünüm

### Dark Overlay
- **Siyah**: `#000000` Alpha: 0.7 (180/255)
- **Koyu Gri**: `#1A1A1A` Alpha: 0.8

## 🔧 Unity'de Kurulum

1. **Klasör Yapısı Oluşturun:**
   ```
   Assets/
   ├── Sprites/
   │   └── UI/
   │       └── Tutorial/
   │           ├── hand_pointer.png
   │           └── circle_highlight.png
   ```

2. **Sprite'ları İçe Aktarın:**
   - Sprite'ları `Assets/Sprites/UI/Tutorial/` klasörüne sürükleyin
   - Her birini seçip import ayarlarını yapın

3. **UI'da Kullanın:**
   - TutorialCanvas → HandIcon → Source Image → hand_pointer
   - TutorialCanvas → HighlightCircle → Source Image → circle_highlight

## 📱 Mobil Optimizasyon

Mobil cihazlar için:

1. **Sprite Boyutları:**
   - Hand Icon: 256x256 px (yeterli)
   - Circle: 512x512 px (yeterli)

2. **Compression:**
   - Android: `ETC2` veya `ASTC`
   - iOS: `ASTC` veya `PVRTC`

3. **Max Size:**
   - Mobil için: `1024` veya `512`
   - Tablet için: `2048`

## 🎬 Animasyon İçin Sprite Sheet

Eğer animasyonlu el ikonu istiyorsanız:

**Sprite Sheet Boyutu**: 1024x256 px (4 frame, her biri 256x256)
**Frame Sayısı**: 4-8 frame
**Animasyon**: El yukarı-aşağı hareket eder

**Unity'de Kullanım:**
1. Sprite Mode: `Multiple`
2. Sprite Editor'da frame'leri kesin
3. Animator ile animasyon oluşturun

---

**Not**: Eğer sprite bulamıyorsanız veya oluşturamıyorsanız, Unity'nin built-in sprite'larını kullanabilirsiniz. Tutorial yine de çalışacaktır, sadece görsel olarak daha basit olacaktır.
