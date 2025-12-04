using UnityEngine;
using TMPro;
using DG.Tweening;

/// <summary>
/// Coin UI'da para kaybedildiÄŸinde sarsÄ±lma ve renk deÄŸiÅŸimi animasyonu.
/// Hyper-casual oyunlardaki gibi feedback verir.
/// </summary>
public class CoinUIShakeEffect : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform coinUIContainer; // Coin UI'Ä±n container'Ä±
    [SerializeField] private TextMeshProUGUI coinText; // Coin text
    
    [Header("Shake Settings")]
    [SerializeField] private float shakeDuration = 0.5f;
    [SerializeField] private float shakeStrength = 20f;
    [SerializeField] private int shakeVibrato = 10;
    
    [Header("Color Settings")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color loseColor = Color.red;
    [SerializeField] private float colorChangeDuration = 0.3f;
    
    private Vector3 originalPosition;
    private Sequence shakeSequence;
    
    private void Awake()
    {
        // Orijinal pozisyonu kaydet
        if (coinUIContainer != null)
        {
            originalPosition = coinUIContainer.localPosition;
        }
        
        // Coin text'i otomatik bul
        if (coinText == null && coinUIContainer != null)
        {
            coinText = coinUIContainer.GetComponentInChildren<TextMeshProUGUI>();
        }
    }
    
    /// <summary>
    /// Para kaybedildiÄŸinde sarsÄ±lma ve kÄ±rmÄ±zÄ±ya dÃ¶nme animasyonu oynatÄ±r
    /// </summary>
    public void PlayLoseMoneyAnimation()
    {
        // Ã–nceki animasyonu durdur
        shakeSequence?.Kill();
        
        // Yeni animasyon sekansÄ± oluÅŸtur
        shakeSequence = DOTween.Sequence();
        
        // 1. Coin UI'Ä± sarsmak
        if (coinUIContainer != null)
        {
            // Pozisyonu sÄ±fÄ±rla
            coinUIContainer.localPosition = originalPosition;
            
            // Shake animasyonu
            shakeSequence.Append(
                coinUIContainer.DOShakePosition(
                    shakeDuration, 
                    shakeStrength, 
                    shakeVibrato, 
                    90, 
                    false, 
                    true
                ).SetEase(Ease.OutQuad)
            );
            
            // Pozisyonu normale dÃ¶ndÃ¼r
            shakeSequence.Append(
                coinUIContainer.DOLocalMove(originalPosition, 0.2f).SetEase(Ease.OutQuad)
            );
        }
        
        // 2. Text rengini deÄŸiÅŸtir (kÄ±rmÄ±zÄ±ya dÃ¶n, sonra normale dÃ¶n)
        if (coinText != null)
        {
            Sequence colorSequence = DOTween.Sequence();
            
            // KÄ±rmÄ±zÄ±ya dÃ¶n
            colorSequence.Append(
                coinText.DOColor(loseColor, colorChangeDuration).SetEase(Ease.OutQuad)
            );
            
            // KÄ±sa bekle
            colorSequence.AppendInterval(0.2f);
            
            // Normale dÃ¶n
            colorSequence.Append(
                coinText.DOColor(normalColor, colorChangeDuration).SetEase(Ease.InQuad)
            );
            
            // Paralel olarak Ã§alÄ±ÅŸtÄ±r
            shakeSequence.Join(colorSequence);
        }
        
        // 3. Opsiyonel: Scale animasyonu (kÃ¼Ã§Ã¼l-bÃ¼yÃ¼ efekti)
        if (coinUIContainer != null)
        {
            Sequence scaleSequence = DOTween.Sequence();
            
            // KÃ¼Ã§Ã¼l
            scaleSequence.Append(
                coinUIContainer.DOScale(0.85f, 0.15f).SetEase(Ease.OutQuad)
            );
            
            // BÃ¼yÃ¼ (biraz fazla)
            scaleSequence.Append(
                coinUIContainer.DOScale(1.1f, 0.15f).SetEase(Ease.OutQuad)
            );
            
            // Normale dÃ¶n
            scaleSequence.Append(
                coinUIContainer.DOScale(1f, 0.2f).SetEase(Ease.OutQuad)
            );
            
            // Paralel olarak Ã§alÄ±ÅŸtÄ±r
            shakeSequence.Join(scaleSequence);
        }
    }
    
    private void OnDestroy()
    {
        // Tween'leri temizle
        shakeSequence?.Kill();
    }
}
