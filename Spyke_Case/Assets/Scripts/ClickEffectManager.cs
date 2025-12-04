using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ClickEffectManager : MonoBehaviour
{
    public static ClickEffectManager Instance { get; private set; }

    [Header("Pool Settings")]
    [SerializeField] private ClickEffect clickEffectPrefab; // ClickEffect scriptini tutan prefab
    [SerializeField] private Canvas mainCanvas;             // Efektlerin gÃ¶sterileceÄŸi UI Canvas'Ä±
    [SerializeField] private int initialPoolSize = 10;

    private readonly Queue<ClickEffect> effectPool = new Queue<ClickEffect>();
    private int createdEffectCount = 0;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Bu yÃ¶neticinin sahne geÃ§iÅŸlerinde kalmasÄ± isteniyorsa DontDestroyOnLoad(gameObject); kullanÄ±labilir.
            // Ancak genellikle sahneye Ã¶zel yÃ¶neticiler iÃ§in bu kaldÄ±rÄ±lÄ±r. Mevcut isteÄŸe uyarak bÄ±raktÄ±m.
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
        // 1. Prefab'Ä± yarat
        ClickEffect clickEffect = Instantiate(clickEffectPrefab, transform);
        
        // 2. Ä°sim ver
        createdEffectCount++;
        clickEffect.gameObject.name = $"ClickEffect_{createdEffectCount}";

        // 3. GameObject'i kapat ve havuzla
        clickEffect.gameObject.SetActive(false);
        effectPool.Enqueue(clickEffect);
    }

    /// <summary>
    /// Havuzdan bir efekt alÄ±r, pozisyonunu ayarlar ve oynatÄ±r.
    /// </summary>
    /// <param name="screenPosition">TÄ±klama/Dokunma'nÄ±n ekran pozisyonu (pixel).</param>
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
            // Havuz boÅŸsa yeni bir tane oluÅŸtur
//             Debug.LogWarning("[ClickEffectManager] Pool is empty. Creating a new effect on the fly.");
            CreateNewEffectForPool();
            effectToPlay = effectPool.Dequeue(); // Yeni oluÅŸturulanÄ± al
        }

        // 1. Canvas'Ä±n Ã§ocuÄŸu yap (gÃ¶rÃ¼nÃ¼rlÃ¼k iÃ§in)
        effectToPlay.transform.SetParent(mainCanvas.transform, false);

        // 2. Ekran pozisyonunu Canvas Ã¼zerindeki yerel pozisyona dÃ¶nÃ¼ÅŸtÃ¼r
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            mainCanvas.GetComponent<RectTransform>(),
            screenPosition,
            mainCanvas.worldCamera, // Screen Space - Camera kullanÄ±lÄ±yorsa kamera bilgisi Ã¶nemli
            out Vector2 localPosition
        );
        
        // 3. Efekti doÄŸru konuma yerleÅŸtir
        effectToPlay.GetComponent<RectTransform>().anchoredPosition = localPosition;

        // 4. GameObject'i aÃ§ ve oynat
        effectToPlay.gameObject.SetActive(true);
        effectToPlay.Play();
    }

    /// <summary>
    /// Efekti sÄ±fÄ±rlar, gÃ¶rÃ¼nmez yapar ve havuza geri gÃ¶nderir.
    /// </summary>
    /// <param name="effect">Havuzlanacak ClickEffect.</param>
    public void ReturnToPool(ClickEffect effect)
    {
        if (effect == null) return;
        
        // 1. Particle sistemini durdur/temizle
        effect.StopAndClear(); 

        // 2. YÃ¶neticinin altÄ±na geri al
        effect.transform.SetParent(transform, false);

        // 3. GameObject'i kapat
        effect.gameObject.SetActive(false);

        // 4. Havuza ekle
        effectPool.Enqueue(effect);
    }
}
