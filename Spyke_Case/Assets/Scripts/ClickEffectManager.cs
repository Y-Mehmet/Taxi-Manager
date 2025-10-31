using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ClickEffectManager : MonoBehaviour
{
    public static ClickEffectManager Instance { get; private set; }

    [Header("Pool Settings")]
    [SerializeField] private ClickEffect clickEffectPrefab; // ClickEffect scriptini tutan prefab
    [SerializeField] private Canvas mainCanvas;             // Efektlerin gösterileceği UI Canvas'ı
    [SerializeField] private int initialPoolSize = 10;

    private readonly Queue<ClickEffect> effectPool = new Queue<ClickEffect>();
    private int createdEffectCount = 0;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Bu yöneticinin sahne geçişlerinde kalması isteniyorsa DontDestroyOnLoad(gameObject); kullanılabilir.
            // Ancak genellikle sahneye özel yöneticiler için bu kaldırılır. Mevcut isteğe uyarak bıraktım.
            // DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        InitializePool();
    }

    private void InitializePool()
    {
        if (clickEffectPrefab == null || mainCanvas == null)
        {
            Debug.LogError("[ClickEffectManager] Prefab or Canvas is missing. Cannot initialize pool.");
            return;
        }

        Debug.Log($"[ClickEffectManager] Initializing pool with size: {initialPoolSize}");
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewEffectForPool();
        }
    }

    private void CreateNewEffectForPool()
    {
        // 1. Prefab'ı yarat
        ClickEffect clickEffect = Instantiate(clickEffectPrefab, transform);
        
        // 2. İsim ver
        createdEffectCount++;
        clickEffect.gameObject.name = $"ClickEffect_{createdEffectCount}";

        // 3. GameObject'i kapat ve havuzla
        clickEffect.gameObject.SetActive(false);
        effectPool.Enqueue(clickEffect);
    }

    /// <summary>
    /// Havuzdan bir efekt alır, pozisyonunu ayarlar ve oynatır.
    /// </summary>
    /// <param name="screenPosition">Tıklama/Dokunma'nın ekran pozisyonu (pixel).</param>
    public void PlayEffect(Vector2 screenPosition)
    {
        if (mainCanvas == null) return;

        ClickEffect effectToPlay;

        // Havuzdan efekt al
        if (effectPool.Count > 0)
        {
            effectToPlay = effectPool.Dequeue();
        }
        else
        {
            // Havuz boşsa yeni bir tane oluştur
            Debug.LogWarning("[ClickEffectManager] Pool is empty. Creating a new effect on the fly.");
            CreateNewEffectForPool();
            effectToPlay = effectPool.Dequeue(); // Yeni oluşturulanı al
        }

        // 1. Canvas'ın çocuğu yap (görünürlük için)
        effectToPlay.transform.SetParent(mainCanvas.transform, false);

        // 2. Ekran pozisyonunu Canvas üzerindeki yerel pozisyona dönüştür
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            mainCanvas.GetComponent<RectTransform>(),
            screenPosition,
            mainCanvas.worldCamera, // Screen Space - Camera kullanılıyorsa kamera bilgisi önemli
            out Vector2 localPosition
        );
        
        // 3. Efekti doğru konuma yerleştir
        effectToPlay.GetComponent<RectTransform>().anchoredPosition = localPosition;

        // 4. GameObject'i aç ve oynat
        effectToPlay.gameObject.SetActive(true);
        effectToPlay.Play();
    }

    /// <summary>
    /// Efekti sıfırlar, görünmez yapar ve havuza geri gönderir.
    /// </summary>
    /// <param name="effect">Havuzlanacak ClickEffect.</param>
    public void ReturnToPool(ClickEffect effect)
    {
        if (effect == null) return;
        
        // 1. Particle sistemini durdur/temizle
        effect.StopAndClear(); 

        // 2. Yöneticinin altına geri al
        effect.transform.SetParent(transform, false);

        // 3. GameObject'i kapat
        effect.gameObject.SetActive(false);

        // 4. Havuza ekle
        effectPool.Enqueue(effect);
    }
}
