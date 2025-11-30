using UnityEngine;
using TMPro;
using DG.Tweening;

/// <summary>
/// Ekranda beliren animasyonlu coin text feedback'i.
/// Hem dünya pozisyonlarını hem UI pozisyonlarını destekler.
/// </summary>
public class FloatingCoinText : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Animation Settings")]
    [SerializeField] private float popDuration = 0.3f;
    [SerializeField] private float popScale = 2.4f; // 2 kat büyük
    [SerializeField] private float displayDuration = 1f; // Ekranda durma süresi
    [SerializeField] private float moveUpDistance = 100f; // Yukarı hareket mesafesi
    [SerializeField] private float moveUpDuration = 0.8f; // Yukarı hareket süresi
    [SerializeField] private float fadeDuration = 0.8f; // Fade out süresi

    private RectTransform rectTransform;
    private Sequence animationSequence;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        
        if (coinText == null)
            coinText = GetComponent<TextMeshProUGUI>();
            
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
    }

    /// <summary>
    /// Animasyonu başlatır
    /// </summary>
    /// <param name="amount">Coin miktarı</param>
    /// <param name="startPosition">Başlangıç pozisyonu</param>
    /// <param name="targetUIPosition">Hedef UI pozisyonu</param>
    /// <param name="isWorldPosition">Başlangıç pozisyonu dünya koordinatı mı?</param>
    public void Initialize(int amount, Vector3 startPosition, RectTransform targetUIPosition, bool isWorldPosition = true)
    {
        // Text'i ayarla
        if (amount > 0)
        {
            coinText.text = $"+{amount}";
            coinText.color = new Color(0.2f, 0.8f, 0.2f); // Yeşil
        }
        else
        {
            coinText.text = amount.ToString();
            coinText.color = new Color(0.9f, 0.2f, 0.2f); // Kırmızı
        }

        // Canvas'ı al
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[FloatingCoinText] Canvas not found!");
            return;
        }

        // Başlangıç pozisyonunu ayarla
        if (isWorldPosition)
        {
            // Dünya pozisyonunu ekran pozisyonuna çevir
            SetPositionFromWorld(startPosition, canvas);
        }
        else
        {
            // UI pozisyonu direkt kullan
            rectTransform.position = startPosition;
        }

        // Başlangıç değerleri
        transform.localScale = Vector3.zero;
        canvasGroup.alpha = 1f;

        // Animasyon sekansı oluştur
        animationSequence = DOTween.Sequence();

        // 1. Pop-up animasyonu (küçükten büyüğe)
        animationSequence.Append(transform.DOScale(popScale, popDuration).SetEase(Ease.OutBack));
        
        // 2. Ekranda dur (1 saniye)
        animationSequence.AppendInterval(displayDuration);

        // 3. Yukarı doğru hareket et + Fade out (paralel)
        Vector3 targetPos = rectTransform.localPosition + new Vector3(0, moveUpDistance, 0);
        animationSequence.Append(rectTransform.DOLocalMove(targetPos, moveUpDuration).SetEase(Ease.OutQuad));
        animationSequence.Join(canvasGroup.DOFade(0f, fadeDuration).SetEase(Ease.InQuad));

        // 4. Animasyon bitince pool'a geri dön
        animationSequence.OnComplete(() => {
            if (CoinObjectPool.Instance != null)
            {
                CoinObjectPool.Instance.ReturnFloatingText(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        });
    }

    /// <summary>
    /// Dünya pozisyonunu UI pozisyonuna çevirir
    /// </summary>
    private void SetPositionFromWorld(Vector3 worldPosition, Canvas canvas)
    {
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            // Overlay mode: Dünya pozisyonunu ekran pozisyonuna çevir
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                Vector3 screenPoint = mainCamera.WorldToScreenPoint(worldPosition);
                rectTransform.position = screenPoint;
            }
            else
            {
                Debug.LogWarning("[FloatingCoinText] Main camera not found!");
                rectTransform.position = worldPosition;
            }
        }
        else
        {
            // Camera mode: RectTransformUtility kullan
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("[FloatingCoinText] Main camera not found!");
                return;
            }

            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(mainCamera, worldPosition);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                screenPoint,
                canvas.worldCamera,
                out Vector2 localPoint
            );
            
            rectTransform.localPosition = localPoint;
        }
    }

    private void OnDestroy()
    {
        // Tween'leri temizle
        animationSequence?.Kill();
    }
}
