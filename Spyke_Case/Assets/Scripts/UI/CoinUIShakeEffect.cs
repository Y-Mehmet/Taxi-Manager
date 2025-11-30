using UnityEngine;
using TMPro;
using DG.Tweening;

/// <summary>
/// Coin UI'da para kaybedildiğinde sarsılma ve renk değişimi animasyonu.
/// Hyper-casual oyunlardaki gibi feedback verir.
/// </summary>
public class CoinUIShakeEffect : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform coinUIContainer; // Coin UI'ın container'ı
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
    /// Para kaybedildiğinde sarsılma ve kırmızıya dönme animasyonu oynatır
    /// </summary>
    public void PlayLoseMoneyAnimation()
    {
        // Önceki animasyonu durdur
        shakeSequence?.Kill();
        
        // Yeni animasyon sekansı oluştur
        shakeSequence = DOTween.Sequence();
        
        // 1. Coin UI'ı sarsmak
        if (coinUIContainer != null)
        {
            // Pozisyonu sıfırla
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
            
            // Pozisyonu normale döndür
            shakeSequence.Append(
                coinUIContainer.DOLocalMove(originalPosition, 0.2f).SetEase(Ease.OutQuad)
            );
        }
        
        // 2. Text rengini değiştir (kırmızıya dön, sonra normale dön)
        if (coinText != null)
        {
            Sequence colorSequence = DOTween.Sequence();
            
            // Kırmızıya dön
            colorSequence.Append(
                coinText.DOColor(loseColor, colorChangeDuration).SetEase(Ease.OutQuad)
            );
            
            // Kısa bekle
            colorSequence.AppendInterval(0.2f);
            
            // Normale dön
            colorSequence.Append(
                coinText.DOColor(normalColor, colorChangeDuration).SetEase(Ease.InQuad)
            );
            
            // Paralel olarak çalıştır
            shakeSequence.Join(colorSequence);
        }
        
        // 3. Opsiyonel: Scale animasyonu (küçül-büyü efekti)
        if (coinUIContainer != null)
        {
            Sequence scaleSequence = DOTween.Sequence();
            
            // Küçül
            scaleSequence.Append(
                coinUIContainer.DOScale(0.85f, 0.15f).SetEase(Ease.OutQuad)
            );
            
            // Büyü (biraz fazla)
            scaleSequence.Append(
                coinUIContainer.DOScale(1.1f, 0.15f).SetEase(Ease.OutQuad)
            );
            
            // Normale dön
            scaleSequence.Append(
                coinUIContainer.DOScale(1f, 0.2f).SetEase(Ease.OutQuad)
            );
            
            // Paralel olarak çalıştır
            shakeSequence.Join(scaleSequence);
        }
    }
    
    private void OnDestroy()
    {
        // Tween'leri temizle
        shakeSequence?.Kill();
    }
}
