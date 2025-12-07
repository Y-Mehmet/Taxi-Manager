using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Shuffle Wagon Colors ability tutorial implementation.
/// Döngüsel olarak: hand animasyonu → shuffle animasyonu (gri → yeni renkler) → tekrarla
/// </summary>
public class ShuffleTutorial : MonoBehaviour, IAbilityTutorial
{
    [Header("Wagon Container")]
    [SerializeField] private Transform wagonContainer; // Container with Layout Group (children are wagon images)
    
    [Header("Hand Animation")]
    [SerializeField] private GameObject handImage; // El görseli
    [SerializeField] private Transform buttonTransform; // Ability button pozisyonu
    
    [Header("Animation Settings")]
    [SerializeField] private float handAnimDuration = 1f; // Hand animasyon süresi
    [SerializeField] private float shuffleDuration = 5f; // Shuffle animasyon süresi
    [SerializeField] private float grayDuration = 1f; // Gri yapma süresi
    
    [Header("Colors")]
    [SerializeField] private Color[] initialColors; // Başlangıç renkleri (4 kırmızı, 3 mavi, 5 sarı gibi)
    [SerializeField] private Color grayColor = Color.gray; // Geçici gri renk
    
    [Header("Cost Display")]
    [SerializeField] private TMPro.TextMeshProUGUI costText; // Maliyet göstergesi
    
    [Header("Settings")]
    [SerializeField] private AbilityType abilityType = AbilityType.ShuffleWagonColors;
    [SerializeField] private string abilityName = "Shuffle Wagon Colors";
    [SerializeField] private string description = "Shuffle all wagon colors on the map. Use this to create new matching opportunities.\\n\\nCost: 100 Coins";
    
    private bool isSkipped = false;
    private Coroutine mainLoop;
    private List<Color> currentColors = new List<Color>(); // Mevcut renkler
    private Image[] wagonImages; // Container'dan alınacak image'ler
    private int currentCost = 100; // Başlangıç maliyeti, her shuffle'da 2 katına çıkar
    
    public bool IsCompleted => false; // Sürekli tekrar eder, skip ile durur
    
    private void Start()
    {
        // Get wagon images from container children
        CollectWagonImages();
        
        // Hide hand initially
        if (handImage != null)
            handImage.SetActive(false);
        
        // Initialize colors
        InitializeColors();
        
        // Update cost display
        UpdateCostDisplay();
        
        // Don't start main loop here - AbilityTutorialButton will trigger it
        // via OnAbilityUsed() after typewriter completes
        
        Debug.Log($"[ShuffleTutorial] Started - Waiting for AbilityTutorialButton to trigger animations");
    }
    
    /// <summary>
    /// Container'ın child'larından Image componentlerini topla
    /// </summary>
    private void CollectWagonImages()
    {
        if (wagonContainer == null)
        {
            Debug.LogError("[ShuffleTutorial] Wagon container not assigned!");
            wagonImages = new Image[0];
            return;
        }
        
        // Get all Image components from children
        List<Image> imageList = new List<Image>();
        
        for (int i = 0; i < wagonContainer.childCount; i++)
        {
            Transform child = wagonContainer.GetChild(i);
            Image img = child.GetComponent<Image>();
            
            if (img != null)
            {
                imageList.Add(img);
            }
            else
            {
                Debug.LogWarning($"[ShuffleTutorial] Child {i} ({child.name}) has no Image component!");
            }
        }
        
        wagonImages = imageList.ToArray();
        Debug.Log($"[ShuffleTutorial] Collected {wagonImages.Length} wagon images from container");
    }
    
    /// <summary>
    /// Başlangıç renklerini ata
    /// </summary>
    private void InitializeColors()
    {
        if (wagonImages == null || wagonImages.Length == 0)
        {
            Debug.LogError("[ShuffleTutorial] Wagon images not assigned!");
            return;
        }
        
        Debug.Log($"[ShuffleTutorial] Wagon images count: {wagonImages.Length}");
        
        // If initialColors is not set, generate random colors
        if (initialColors == null || initialColors.Length != wagonImages.Length)
        {
            Debug.LogWarning($"[ShuffleTutorial] Initial colors length ({initialColors?.Length ?? 0}) doesn't match wagon images ({wagonImages.Length}), using random colors");
            initialColors = new Color[wagonImages.Length];
            Color[] availableColors = { Color.red, Color.blue, Color.yellow, Color.black };
            
            for (int i = 0; i < initialColors.Length; i++)
            {
                initialColors[i] = availableColors[Random.Range(0, availableColors.Length)];
            }
        }
        
        // Apply initial colors
        currentColors = new List<Color>(initialColors);
        ApplyColors(currentColors);
        
        Debug.Log($"[ShuffleTutorial] Initialized {wagonImages.Length} wagons with colors");
    }
    
    /// <summary>
    /// Renkleri image'lara uygula
    /// </summary>
    private void ApplyColors(List<Color> colors)
    {
        Debug.Log($"[ShuffleTutorial] Applying {colors.Count} colors to {wagonImages.Length} images");
        
        for (int i = 0; i < wagonImages.Length && i < colors.Count; i++)
        {
            if (wagonImages[i] != null)
            {
                wagonImages[i].color = colors[i];
                Debug.Log($"[ShuffleTutorial] Image {i}: {colors[i]}");
            }
            else
            {
                Debug.LogWarning($"[ShuffleTutorial] Image {i} is NULL!");
            }
        }
    }
    
    /// <summary>
    /// Ability kullanıldığında (AbilityTutorialButton tarafından çağrılır)
    /// Typewriter bitip 2 saniye bekledikten sonra burası çağrılır
    /// </summary>
    public void OnAbilityUsed()
    {
        // Start main loop if not already started
        if (mainLoop == null)
        {
            Debug.Log("[ShuffleTutorial] OnAbilityUsed - Starting main loop");
            StartMainLoop();
        }
    }
    
    /// <summary>
    /// Ana döngüyü başlat (skip edilene kadar tekrar eder)
    /// </summary>
    private void StartMainLoop()
    {
        if (mainLoop != null)
        {
            StopCoroutine(mainLoop);
        }
        
        mainLoop = StartCoroutine(MainLoopCoroutine());
    }
    
    /// <summary>
    /// Ana döngü: hand animasyonu → shuffle animasyonu → tekrarla
    /// </summary>
    private IEnumerator MainLoopCoroutine()
    {
        while (!isSkipped)
        {
            // 1. Hand animasyonu (1 saniye)
            yield return StartCoroutine(PlayHandAnimation());
            
            // 2. Shuffle animasyonu (5 saniye)
            yield return StartCoroutine(ShuffleAnimation());
            
            // 3. Kısa bekle
            yield return new WaitForSeconds(0.5f);
        }
        
        Debug.Log("[ShuffleTutorial] Loop stopped (skipped)");
    }
    
    /// <summary>
    /// Hand tıklama animasyonu
    /// </summary>
    private IEnumerator PlayHandAnimation()
    {
        if (handImage == null || buttonTransform == null)
        {
            yield return new WaitForSeconds(handAnimDuration);
            yield break;
        }
        
        Debug.Log("[ShuffleTutorial] Playing hand animation");
        
        // Show hand at button position
        handImage.SetActive(true);
        handImage.transform.position = buttonTransform.position;
        
        // Wait for animation duration
        yield return new WaitForSeconds(handAnimDuration);
        
        // Hide hand
        handImage.SetActive(false);
    }
    
    /// <summary>
    /// Shuffle animasyonu: gri → renk cümbüşü → meksika dalgası → nihai renkler
    /// </summary>
    private IEnumerator ShuffleAnimation()
    {
        Debug.Log("[ShuffleTutorial] Starting shuffle animation");
        
        // 1. Önce hepsini gri yap
        for (int i = 0; i < wagonImages.Length; i++)
        {
            if (wagonImages[i] != null)
            {
                wagonImages[i].color = grayColor;
            }
        }
        
        yield return new WaitForSeconds(0.3f);
        
        // 2. Renk cümbüşü - rastgele renkler yanıp sönsün
        Color[] flashColors = { Color.red, Color.blue, Color.yellow, Color.green, Color.magenta, Color.cyan };
        float flashDuration = 1f;
        float flashInterval = 0.1f;
        int flashCount = (int)(flashDuration / flashInterval);
        
        for (int flash = 0; flash < flashCount; flash++)
        {
            for (int i = 0; i < wagonImages.Length; i++)
            {
                if (wagonImages[i] != null)
                {
                    // Rastgele renk
                    Color randomColor = flashColors[Random.Range(0, flashColors.Length)];
                    wagonImages[i].color = randomColor;
                }
            }
            
            yield return new WaitForSeconds(flashInterval);
        }
        
        // 3. Tekrar gri yap
        for (int i = 0; i < wagonImages.Length; i++)
        {
            if (wagonImages[i] != null)
            {
                wagonImages[i].color = grayColor;
            }
        }
        
        yield return new WaitForSeconds(0.2f);
        
        // 4. Renkleri shuffle et (nihai renkler)
        List<Color> shuffledColors = new List<Color>(currentColors);
        
        // Fisher-Yates shuffle algorithm
        for (int i = shuffledColors.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Color temp = shuffledColors[i];
            shuffledColors[i] = shuffledColors[j];
            shuffledColors[j] = temp;
        }
        
        currentColors = shuffledColors;
        
        // 5. Meksika dalgası - sırayla yukarı zıpla ve nihai renge dön
        float waveDelay = 0.1f; // Her wagon arasındaki gecikme
        float jumpHeight = 30f; // Yukarı zıplama yüksekliği
        float jumpDuration = 0.3f; // Zıplama süresi
        
        // Tüm wagon'ların orijinal pozisyonlarını kaydet
        Vector3[] originalPositions = new Vector3[wagonImages.Length];
        for (int i = 0; i < wagonImages.Length; i++)
        {
            if (wagonImages[i] != null)
            {
                originalPositions[i] = wagonImages[i].rectTransform.anchoredPosition;
            }
        }
        
        // Meksika dalgası - her wagon sırayla
        List<Coroutine> waveCoroutines = new List<Coroutine>();
        
        for (int i = 0; i < wagonImages.Length; i++)
        {
            if (wagonImages[i] != null)
            {
                int index = i; // Closure için
                Coroutine waveCoroutine = StartCoroutine(WagonWaveJump(
                    wagonImages[index],
                    originalPositions[index],
                    shuffledColors[index],
                    jumpHeight,
                    jumpDuration
                ));
                waveCoroutines.Add(waveCoroutine);
                
                yield return new WaitForSeconds(waveDelay);
            }
        }
        
        // Tüm wagon'ların dalgasının bitmesini bekle
        yield return new WaitForSeconds(jumpDuration);
        
        // 4. Cost'u 2 katına çıkar ve güncelle
        currentCost *= 2;
        UpdateCostDisplay();
        Debug.Log($"[ShuffleTutorial] Cost doubled to: {currentCost}");
        
        Debug.Log("[ShuffleTutorial] Shuffle animation complete");
    }
    
    /// <summary>
    /// Tek bir wagon için dalga zıplama animasyonu
    /// </summary>
    private IEnumerator WagonWaveJump(Image wagon, Vector3 originalPos, Color finalColor, float jumpHeight, float duration)
    {
        RectTransform rect = wagon.rectTransform;
        float elapsed = 0f;
        
        // Yukarı zıpla
        while (elapsed < duration / 2)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (duration / 2);
            float yOffset = Mathf.Sin(t * Mathf.PI) * jumpHeight;
            
            rect.anchoredPosition = new Vector2(originalPos.x, originalPos.y + yOffset);
            
            yield return null;
        }
        
        // Aşağı inerken nihai renge dön
        elapsed = 0f;
        while (elapsed < duration / 2)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (duration / 2);
            float yOffset = Mathf.Sin((1 - t) * Mathf.PI / 2) * jumpHeight;
            
            rect.anchoredPosition = new Vector2(originalPos.x, originalPos.y + yOffset);
            
            // Aşağı inerken rengi değiştir
            wagon.color = Color.Lerp(grayColor, finalColor, t);
            
            yield return null;
        }
        
        // Tam pozisyon ve renge dön
        rect.anchoredPosition = originalPos;
        wagon.color = finalColor;
    }
    
    /// <summary>
    /// Skip edildiğinde döngüyü durdur
    /// </summary>
    public void Skip()
    {
        isSkipped = true;
        
        if (mainLoop != null)
        {
            StopCoroutine(mainLoop);
            mainLoop = null;
        }
        
        if (handImage != null)
        {
            handImage.SetActive(false);
        }
        
        Debug.Log("[ShuffleTutorial] Skipped");
    }
    
    /// <summary>
    /// Tutorial'ı sıfırla
    /// </summary>
    public void ResetTutorial()
    {
        isSkipped = false;
        
        if (mainLoop != null)
        {
            StopCoroutine(mainLoop);
            mainLoop = null;
        }
        
        InitializeColors();
        
        if (handImage != null)
        {
            handImage.SetActive(false);
        }
        
        Debug.Log("[ShuffleTutorial] Reset");
    }
    
    /// <summary>
    /// Maliyet metnini günceller
    /// </summary>
    private void UpdateCostDisplay()
    {
        if (costText != null)
        {
            costText.text = currentCost.ToString();
        }
    }
    
    // IAbilityTutorial interface implementation
    public AbilityType GetAbilityType() => abilityType;
    public string GetAbilityName() => abilityName;
    public string GetDescription() => description;
    public int GetCost() => currentCost; // Dynamic cost
}
