using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

/// <summary>
/// Royal Match tarzÄ± coin animasyonu - coin sprite'larÄ± uÃ§ar
/// </summary>
public class CoinSpriteAnimation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image coinImage;
    
    [Header("Animation Settings")]
    [SerializeField] private float popDuration = 0.15f;
    [SerializeField] private float popScale = 1.3f;
    [SerializeField] private float delayBetweenCoins = 0.05f;
    [SerializeField] private float moveDuration = 0.6f;
    
    private RectTransform rectTransform;
    private Sequence animationSequence;
    
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        
        if (coinImage == null)
            coinImage = GetComponent<Image>();
    }
    
    /// <summary>
    /// Coin animasyonunu baÅŸlatÄ±r
    /// </summary>
    /// <param name="startPosition">BaÅŸlangÄ±Ã§ pozisyonu (ekran koordinatÄ±)</param>
    /// <param name="targetPosition">Hedef pozisyon (coin UI)</param>
    /// <param name="delay">BaÅŸlama gecikmesi</param>
    public void Initialize(Vector3 startPosition, Vector3 targetPosition, float delay = 0f)
    {
        // Pozisyonu ayarla
        rectTransform.position = startPosition;
        
        // BaÅŸlangÄ±Ã§ deÄŸerleri
        transform.localScale = Vector3.zero;
        coinImage.color = Color.white;
        
        // Animasyon sekansÄ±
        animationSequence = DOTween.Sequence();
        
        // Gecikme ekle
        if (delay > 0)
        {
            animationSequence.AppendInterval(delay);
        }
        
        // 1. Pop-up (bÃ¼yÃ¼me)
        animationSequence.Append(transform.DOScale(popScale, popDuration).SetEase(Ease.OutBack));
        
        // 2. KÄ±sa bekleme (ekranda dur)
        animationSequence.AppendInterval(0.1f);
        
        // 3. Normal boyuta dÃ¶n
        animationSequence.Append(transform.DOScale(1f, popDuration * 0.5f).SetEase(Ease.InOutQuad));
        
        // 4. Hedefe doÄŸru hareket et
        animationSequence.Append(rectTransform.DOMove(targetPosition, moveDuration).SetEase(Ease.InQuad));
        
        // 5. Hareket ederken kÃ¼Ã§Ã¼l
        animationSequence.Join(transform.DOScale(0.3f, moveDuration).SetEase(Ease.InQuad));
        
        // 6. Animasyon bitince pool'a geri dÃ¶n
        animationSequence.OnComplete(() => {
            if (CoinObjectPool.Instance != null)
            {
                CoinObjectPool.Instance.ReturnCoinSprite(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        });
    }
    
    private void OnDestroy()
    {
        animationSequence?.Kill();
    }
}
