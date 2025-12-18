using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tiled (kareli) bir image'i çapraz olarak (sola ve aşağı) kaydırarak animasyonlu arkaplan oluşturur.
/// Image'in "Image Type" ayarı "Tiled" olmalıdır.
/// </summary>
[RequireComponent(typeof(RawImage))]
public class TiledBackgroundScroller : MonoBehaviour
{
    [Header("Scroll Settings")]
    [SerializeField] private Vector2 scrollSpeed = new Vector2(-0.05f, -0.05f); // X: Sola (-), Y: Aşağı (-)
    [SerializeField] private bool autoScroll = true; // Otomatik kaydırma aktif mi?
    
    private RawImage rawImage;
    private Rect uvRect;
    
    private void Awake()
    {
        rawImage = GetComponent<RawImage>();
        
        if (rawImage == null)
        {
            Debug.LogError("[TiledBackgroundScroller] RawImage component not found!");
            enabled = false;
            return;
        }
        
        // UV Rect'i al (başlangıç değeri)
        uvRect = rawImage.uvRect;
        
        /* Debug.Log($"[TiledBackgroundScroller] Initialized - Scroll Speed: {scrollSpeed}"); */
    }
    
    private void Update()
    {
        if (!autoScroll || rawImage == null) return;
        
        // UV koordinatlarını kaydır (Time.deltaTime ile frame-independent)
        uvRect.x += scrollSpeed.x * Time.deltaTime;
        uvRect.y += scrollSpeed.y * Time.deltaTime;
        
        // UV koordinatları 0-1 aralığında döngüsel olarak tekrar eder
        // Fakat sonsuz büyümeyi önlemek için modulo kullanabiliriz (opsiyonel)
        // Unity otomatik olarak wrap yapar, ama yine de kontrol edelim
        if (uvRect.x > 1f || uvRect.x < -1f)
            uvRect.x = uvRect.x % 1f;
            
        if (uvRect.y > 1f || uvRect.y < -1f)
            uvRect.y = uvRect.y % 1f;
        
        // Güncellenmiş UV Rect'i uygula
        rawImage.uvRect = uvRect;
    }
    
    /// <summary>
    /// Kaydırma hızını değiştir
    /// </summary>
    public void SetScrollSpeed(Vector2 newSpeed)
    {
        scrollSpeed = newSpeed;
    }
    
    /// <summary>
    /// Kaydırmayı durdur/başlat
    /// </summary>
    public void SetAutoScroll(bool enabled)
    {
        autoScroll = enabled;
    }
    
    /// <summary>
    /// UV pozisyonunu sıfırla
    /// </summary>
    public void ResetPosition()
    {
        uvRect = new Rect(0, 0, uvRect.width, uvRect.height);
        if (rawImage != null)
            rawImage.uvRect = uvRect;
    }
}
