using UnityEngine;
using TMPro;
using System.Collections;
using DG.Tweening;

/// <summary>
/// Royal Match tarzı coin animasyonlarını yöneten manager.
/// Coin sprite'ları ve text feedback'i birlikte yönetir.
/// </summary>
public class CoinAnimationManager : MonoBehaviour
{
    public static CoinAnimationManager Instance { get; private set; }

    [Header("Prefabs")]
    [SerializeField] private GameObject coinSpritePrefab; // Coin sprite prefab'ı
    [SerializeField] private GameObject floatingTextPrefab; // Text prefab'ı

    [Header("Target UI")]
    [SerializeField] private RectTransform coinUITarget; // Coin text'in bulunduğu UI elementi
    [SerializeField] private CoinUIShakeEffect coinUIShakeEffect; // Coin UI shake efekti
    [SerializeField] private RectTransform coinBagImage; // Coin bag image (bounce animasyonu için)

    [Header("Canvas")]
    [SerializeField] private Canvas canvas;

    [Header("Coin Animation Settings")]
    [SerializeField] private int coinsPerUnit = 5; // Her 20 coin için kaç sprite (örn: 20 coin = 5 sprite)
    [SerializeField] private int maxCoins = 10; // Maksimum coin sprite sayısı
    [SerializeField] private float spreadRadius = 50f; // Coin'lerin yayılma yarıçapı
    
    [Header("Coin Bag Animation")]
    [SerializeField] private float bagBounceScale = 1.2f; // Coin bag büyüme oranı
    [SerializeField] private float bagBounceDuration = 0.2f; // Bounce animasyon süresi
    [SerializeField] private float bagShakeStrength = 10f; // Sallama gücü

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        // Canvas'ı otomatik bul
        if (canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
        }
    }
    
    private void OnEnable()
    {
        // Coin hedefe ulaştığında bag animasyonu oynat
        CoinSpriteAnimation.OnCoinReachedTarget += PlayCoinBagBounce;
    }
    
    private void OnDisable()
    {
        CoinSpriteAnimation.OnCoinReachedTarget -= PlayCoinBagBounce;
    }

    /// <summary>
    /// Coin kazanma animasyonu gösterir (dünya pozisyonundan)
    /// </summary>
    public void ShowCoinGain(int amount, Vector3 worldPosition)
    {
        if (amount <= 0) return;

        // Text feedback göster
        ShowFloatingText(amount, worldPosition, true);

        // Coin sprite'ları göster
        StartCoroutine(SpawnCoinSprites(amount, worldPosition, true));
    }

    /// <summary>
    /// Coin harcama animasyonu gösterir (UI pozisyonundan)
    /// Sadece text feedback gösterir, coin sprite animasyonu yok
    /// Shake animasyonu YOK (Uber ve Crush Penalty için)
    /// </summary>
    public void ShowCoinSpend(int amount, Vector3 uiPosition)
    {
        if (amount <= 0) return;

        // UI pozisyonunu dünya pozisyonuna çevir
        Vector3 worldPosition = ConvertUIToWorldPosition(uiPosition);

        // Sadece text feedback göster (coin sprite yok, shake yok)
        ShowFloatingText(-amount, worldPosition, true);
    }

    /// <summary>
    /// Coin harcama animasyonu gösterir + Shake efekti (Ability için)
    /// </summary>
    public void ShowCoinSpendWithShake(int amount, Vector3 uiPosition)
    {
        if (amount <= 0) return;

        // UI pozisyonunu dünya pozisyonuna çevir
        Vector3 worldPosition = ConvertUIToWorldPosition(uiPosition);

        // Text feedback göster
        ShowFloatingText(-amount, worldPosition, true);
        
        // Coin UI'da shake animasyonu oynat (sadece ability için)
        if (coinUIShakeEffect != null)
        {
            coinUIShakeEffect.PlayLoseMoneyAnimation();
        }
    }

    /// <summary>
    /// Harcama feedback'i gösterir (yeni ekonomi sistemi için alias)
    /// Shake animasyonu YOK
    /// </summary>
    public void ShowSpendingFeedback(int amount, Vector3 position)
    {
        ShowCoinSpend(amount, position);
    }

    /// <summary>
    /// Genel feedback metodu (eski sistem ile uyumluluk için)
    /// </summary>
    public void ShowCoinFeedback(int amount, Vector3 position)
    {
        if (amount > 0)
        {
            ShowCoinGain(amount, position);
        }
        else
        {
            ShowCoinSpend(-amount, position);
        }
    }

    /// <summary>
    /// Floating text oluşturur
    /// </summary>
    private void ShowFloatingText(int amount, Vector3 position, bool isWorldPosition)
    {
        if (floatingTextPrefab == null) return;

        // Pool'dan al veya yeni oluştur
        GameObject textObj;
        if (CoinObjectPool.Instance != null)
        {
            textObj = CoinObjectPool.Instance.GetFloatingText(canvas.transform);
        }
        else
        {
            textObj = Instantiate(floatingTextPrefab, canvas.transform);
        }

        FloatingCoinText floatingText = textObj.GetComponent<FloatingCoinText>();

        if (floatingText != null)
        {
            floatingText.Initialize(amount, position, coinUITarget, isWorldPosition);
        }
    }

    /// <summary>
    /// Coin sprite'larını spawn eder
    /// </summary>
    private IEnumerator SpawnCoinSprites(int amount, Vector3 position, bool isGain)
    {
        if (coinSpritePrefab == null || coinUITarget == null)
        {
//             Debug.LogWarning("[CoinAnimationManager] Coin sprite prefab or target not assigned!");
            yield break;
        }

        // Kaç coin sprite oluşturulacak hesapla
        int coinCount = Mathf.Min(Mathf.CeilToInt(amount / (float)coinsPerUnit), maxCoins);
        
        // Pozisyonu ekran koordinatına çevir
        Vector3 screenPosition = GetScreenPosition(position, isGain);

        for (int i = 0; i < coinCount; i++)
        {
            // Coin sprite'ı pool'dan al veya yeni oluştur
            GameObject coinObj;
            if (CoinObjectPool.Instance != null)
            {
                coinObj = CoinObjectPool.Instance.GetCoinSprite(canvas.transform);
            }
            else
            {
                coinObj = Instantiate(coinSpritePrefab, canvas.transform);
            }
            
            CoinSpriteAnimation coinAnim = coinObj.GetComponent<CoinSpriteAnimation>();

            if (coinAnim != null)
            {
                // Random offset ekle (dağınık görünüm için)
                Vector3 randomOffset = Random.insideUnitCircle * spreadRadius;
                Vector3 startPos = screenPosition + randomOffset;

                // Animasyonu başlat
                Vector3 targetPos = isGain ? coinUITarget.position : screenPosition;
                Vector3 sourcePos = isGain ? startPos : coinUITarget.position;
                
                coinAnim.Initialize(sourcePos, targetPos, i * 0.05f);
            }

            yield return new WaitForSeconds(0.05f);
        }
    }

    /// <summary>
    /// Pozisyonu ekran koordinatına çevirir
    /// </summary>
    private Vector3 GetScreenPosition(Vector3 position, bool isWorldPosition)
    {
        if (!isWorldPosition)
        {
            // Zaten UI pozisyonu
            return position;
        }

        // Dünya pozisyonunu ekran pozisyonuna çevir
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
//             Debug.LogWarning("[CoinAnimationManager] Main camera not found!");
            return position;
        }

        // Canvas render mode'a göre dönüşüm yap
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            // Overlay mode: WorldToScreenPoint kullan
            return mainCamera.WorldToScreenPoint(position);
        }
        else if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            // Camera mode: WorldToScreenPoint kullan
            return mainCamera.WorldToScreenPoint(position);
        }
        else
        {
            // World Space: Direkt pozisyon kullan
            return position;
        }
    }

    /// <summary>
    /// UI pozisyonunu dünya pozisyonuna çevirir
    /// </summary>
    private Vector3 ConvertUIToWorldPosition(Vector3 uiPosition)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
//             Debug.LogWarning("[CoinAnimationManager] Main camera not found for UI to World conversion!");
            return uiPosition;
        }

        // UI pozisyonunu (ekran koordinatı) dünya pozisyonuna çevir
        // Ekran derinliğini belirle (kameranın önünde bir mesafe)
        float distanceFromCamera = 10f;
        
        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(new Vector3(uiPosition.x, uiPosition.y, distanceFromCamera));
        return worldPosition;
    }

    /// <summary>
    /// Coin UI target'ı runtime'da ayarlamak için
    /// </summary>
    public void SetCoinUITarget(RectTransform target)
    {
        coinUITarget = target;
    }
    
    /// <summary>
    /// Coin bag bounce animasyonu (coinler vardığında)
    /// </summary>
    private void PlayCoinBagBounce()
    {
        if (coinBagImage == null) return;
        
        // Coin sound effect çal
        if (SoundManager.instance != null)
        {
            SoundManager.instance.PlaySfx(SoundType.EarnCoin, 0f, true);
        }
        
        // Önceki animasyonu durdur
        coinBagImage.DOKill();
        
        // Bounce + Shake animasyonu
        DG.Tweening.Sequence bounceSequence = DOTween.Sequence();
        
        // 1. Büyü
        bounceSequence.Append(coinBagImage.DOScale(bagBounceScale, bagBounceDuration * 0.5f).SetEase(Ease.OutQuad));
        
        // 2. Salla (shake)
        bounceSequence.Join(coinBagImage.DOShakeRotation(bagBounceDuration, bagShakeStrength, 10, 90, false));
        
        // 3. Normal boyuta dön
        bounceSequence.Append(coinBagImage.DOScale(1f, bagBounceDuration * 0.5f).SetEase(Ease.InOutQuad));
    }
}
