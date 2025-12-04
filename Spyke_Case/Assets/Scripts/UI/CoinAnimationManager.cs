using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Royal Match tarzÄ± coin animasyonlarÄ±nÄ± yÃ¶neten manager.
/// Coin sprite'larÄ± ve text feedback'i birlikte yÃ¶netir.
/// </summary>
public class CoinAnimationManager : MonoBehaviour
{
    public static CoinAnimationManager Instance { get; private set; }

    [Header("Prefabs")]
    [SerializeField] private GameObject coinSpritePrefab; // Coin sprite prefab'Ä±
    [SerializeField] private GameObject floatingTextPrefab; // Text prefab'Ä±

    [Header("Target UI")]
    [SerializeField] private RectTransform coinUITarget; // Coin text'in bulunduÄŸu UI elementi
    [SerializeField] private CoinUIShakeEffect coinUIShakeEffect; // Coin UI shake efekti

    [Header("Canvas")]
    [SerializeField] private Canvas canvas;

    [Header("Coin Animation Settings")]
    [SerializeField] private int coinsPerUnit = 5; // Her 20 coin iÃ§in kaÃ§ sprite (Ã¶rn: 20 coin = 5 sprite)
    [SerializeField] private int maxCoins = 10; // Maksimum coin sprite sayÄ±sÄ±
    [SerializeField] private float spreadRadius = 50f; // Coin'lerin yayÄ±lma yarÄ±Ã§apÄ±

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

        // Canvas'Ä± otomatik bul
        if (canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
        }
    }

    /// <summary>
    /// Coin kazanma animasyonu gÃ¶sterir (dÃ¼nya pozisyonundan)
    /// </summary>
    public void ShowCoinGain(int amount, Vector3 worldPosition)
    {
        if (amount <= 0) return;

        // Text feedback gÃ¶ster
        ShowFloatingText(amount, worldPosition, true);

        // Coin sprite'larÄ± gÃ¶ster
        StartCoroutine(SpawnCoinSprites(amount, worldPosition, true));
    }

    /// <summary>
    /// Coin harcama animasyonu gÃ¶sterir (UI pozisyonundan)
    /// Sadece text feedback gÃ¶sterir, coin sprite animasyonu yok
    /// </summary>
    public void ShowCoinSpend(int amount, Vector3 uiPosition)
    {
        if (amount <= 0) return;

        // UI pozisyonunu dÃ¼nya pozisyonuna Ã§evir
        Vector3 worldPosition = ConvertUIToWorldPosition(uiPosition);

        // Sadece text feedback gÃ¶ster (coin sprite yok)
        ShowFloatingText(-amount, worldPosition, true);
        
        // Coin UI'da shake animasyonu oynat
        if (coinUIShakeEffect != null)
        {
            coinUIShakeEffect.PlayLoseMoneyAnimation();
        }
    }

    /// <summary>
    /// Harcama feedback'i gÃ¶sterir (yeni ekonomi sistemi iÃ§in alias)
    /// </summary>
    public void ShowSpendingFeedback(int amount, Vector3 position)
    {
        ShowCoinSpend(amount, position);
    }

    /// <summary>
    /// Genel feedback metodu (eski sistem ile uyumluluk iÃ§in)
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
    /// Floating text oluÅŸturur
    /// </summary>
    private void ShowFloatingText(int amount, Vector3 position, bool isWorldPosition)
    {
        if (floatingTextPrefab == null) return;

        // Pool'dan al veya yeni oluÅŸtur
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
    /// Coin sprite'larÄ±nÄ± spawn eder
    /// </summary>
    private IEnumerator SpawnCoinSprites(int amount, Vector3 position, bool isGain)
    {
        if (coinSpritePrefab == null || coinUITarget == null)
        {
//             Debug.LogWarning("[CoinAnimationManager] Coin sprite prefab or target not assigned!");
            yield break;
        }

        // KaÃ§ coin sprite oluÅŸturulacak hesapla
        int coinCount = Mathf.Min(Mathf.CeilToInt(amount / (float)coinsPerUnit), maxCoins);
        
        // Pozisyonu ekran koordinatÄ±na Ã§evir
        Vector3 screenPosition = GetScreenPosition(position, isGain);

        for (int i = 0; i < coinCount; i++)
        {
            // Coin sprite'Ä± pool'dan al veya yeni oluÅŸtur
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
                // Random offset ekle (daÄŸÄ±nÄ±k gÃ¶rÃ¼nÃ¼m iÃ§in)
                Vector3 randomOffset = Random.insideUnitCircle * spreadRadius;
                Vector3 startPos = screenPosition + randomOffset;

                // Animasyonu baÅŸlat
                Vector3 targetPos = isGain ? coinUITarget.position : screenPosition;
                Vector3 sourcePos = isGain ? startPos : coinUITarget.position;
                
                coinAnim.Initialize(sourcePos, targetPos, i * 0.05f);
            }

            yield return new WaitForSeconds(0.05f);
        }
    }

    /// <summary>
    /// Pozisyonu ekran koordinatÄ±na Ã§evirir
    /// </summary>
    private Vector3 GetScreenPosition(Vector3 position, bool isWorldPosition)
    {
        if (!isWorldPosition)
        {
            // Zaten UI pozisyonu
            return position;
        }

        // DÃ¼nya pozisyonunu ekran pozisyonuna Ã§evir
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
//             Debug.LogWarning("[CoinAnimationManager] Main camera not found!");
            return position;
        }

        // Canvas render mode'a gÃ¶re dÃ¶nÃ¼ÅŸÃ¼m yap
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
    /// UI pozisyonunu dÃ¼nya pozisyonuna Ã§evirir
    /// </summary>
    private Vector3 ConvertUIToWorldPosition(Vector3 uiPosition)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
//             Debug.LogWarning("[CoinAnimationManager] Main camera not found for UI to World conversion!");
            return uiPosition;
        }

        // UI pozisyonunu (ekran koordinatÄ±) dÃ¼nya pozisyonuna Ã§evir
        // Ekran derinliÄŸini belirle (kameranÄ±n Ã¶nÃ¼nde bir mesafe)
        float distanceFromCamera = 10f;
        
        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(new Vector3(uiPosition.x, uiPosition.y, distanceFromCamera));
        return worldPosition;
    }

    /// <summary>
    /// Coin UI target'Ä± runtime'da ayarlamak iÃ§in
    /// </summary>
    public void SetCoinUITarget(RectTransform target)
    {
        coinUITarget = target;
    }
}
