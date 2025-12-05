using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Generic ability tutorial button.
/// Herhangi bir IAbilityTutorial implementation ile çalışır.
/// SOLID: Open/Closed Principle - Yeni ability'ler için extend edilebilir, modify edilmez.
/// </summary>
[RequireComponent(typeof(Button))]
public class AbilityTutorialButton : MonoBehaviour
{
    [Header("Tutorial Reference")]
    [SerializeField] private MonoBehaviour tutorialBehaviour; // IAbilityTutorial implement eden MonoBehaviour
    
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI buttonText; // Buton üzerindeki text (opsiyonel)
    [SerializeField] private TextMeshProUGUI costText; // Maliyet göstergesi (opsiyonel)
    [SerializeField] private TextMeshProUGUI descriptionText; // Açıklama metni (ortak, opsiyonel)
    
    [Header("Typewriter Effect")]
    [SerializeField] private TypewriterEffect typewriterEffect; // Typewriter component (opsiyonel)
    [SerializeField] private bool enableAutoClick = true; // Otomatik tıklama aktif mi?
    [SerializeField] private int autoClickCount = 3; // Kaç kere otomatik tıklama
    [SerializeField] private float autoClickDelay = 2f; // Tıklamalar arası bekleme (saniye)
    [SerializeField] private AbilityTutorialManager tutorialManager; // Tutorial manager (opsiyonel)
    
    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem clickParticleEffectPrefab; // Particle prefab (opsiyonel)
    [SerializeField] private float particleDisplayDuration = 1f; // Particle kaç saniye görünsün
    
    [Header("Hand Animation")]
    [SerializeField] private GameObject handImage; // El görseli (tıklama animasyonu için)
    [SerializeField] private float handClickAnimDuration = 0.3f; // El tıklama animasyon süresi
    
    [Header("Audio")]
    [SerializeField] private AudioClip buttonClickSound; // Button click ses efekti (opsiyonel)
    
    private Button button;
    private IAbilityTutorial tutorial;
    private bool hasStartedAutoClick = false;
    private ParticleSystem instantiatedParticle; // Instantiate edilmiş particle
    private AudioSource audioSource; // Audio source component
    private int handClickCount = 0; // El kaç kere tıkladı
    
    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnButtonClicked);
        
        // Interface'i al
        if (tutorialBehaviour != null)
        {
            tutorial = tutorialBehaviour as IAbilityTutorial;
            
            if (tutorial == null)
            {
                Debug.LogError($"[AbilityTutorialButton] {tutorialBehaviour.GetType().Name} does not implement IAbilityTutorial!");
            }
        }
        else
        {
            Debug.LogError("[AbilityTutorialButton] No tutorial behaviour assigned!");
        }
        
        // Instantiate particle effect as child
        if (clickParticleEffectPrefab != null)
        {
            instantiatedParticle = Instantiate(clickParticleEffectPrefab, transform);
            instantiatedParticle.transform.localPosition = Vector3.zero;
            instantiatedParticle.gameObject.SetActive(false); // Başlangıçta gizli
            
            Debug.Log($"[AbilityTutorialButton] Particle effect instantiated: {instantiatedParticle.name}");
        }
        else
        {
            Debug.LogWarning("[AbilityTutorialButton] No particle prefab assigned!");
        }
        
        // Setup hand image
        if (handImage != null)
        {
            handImage.SetActive(false); // Başlangıçta gizli
            Debug.Log("[AbilityTutorialButton] Hand image initialized (hidden)");
        }
        
        // Setup AudioSource for button click sound
        if (buttonClickSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.clip = buttonClickSound;
            
            Debug.Log($"[AbilityTutorialButton] AudioSource added with clip: {buttonClickSound.name}");
        }
        else
        {
            Debug.LogWarning("[AbilityTutorialButton] No button click sound assigned!");
        }
    }
    
    private void Start()
    {
        UpdateButtonUI();
        
        // Start typewriter effect if available
        if (typewriterEffect != null && tutorial != null)
        {
            string description = tutorial.GetDescription();
            
            // Subscribe to typing complete event
            typewriterEffect.OnTypingComplete += OnTypingComplete;
            
            // Start typing
            typewriterEffect.StartTyping(description);
            
            Debug.Log("[AbilityTutorialButton] Started typewriter effect");
        }
    }
    
    /// <summary>
    /// Called when typewriter effect completes (either finished or skipped)
    /// </summary>
    private void OnTypingComplete()
    {
        Debug.Log("[AbilityTutorialButton] Typewriter complete, starting auto-click");
        
        // Start auto-click sequence
        if (enableAutoClick && !hasStartedAutoClick)
        {
            hasStartedAutoClick = true;
            StartCoroutine(AutoClickSequence());
        }
    }
    
    /// <summary>
    /// Auto-click the button multiple times with delay
    /// </summary>
    private System.Collections.IEnumerator AutoClickSequence()
    {
        for (int i = 0; i < autoClickCount; i++)
        {
            // Wait before clicking
            yield return new WaitForSeconds(autoClickDelay);
            
            // Simulate button click
            if (!tutorial.IsCompleted)
            {
                Debug.Log($"[AbilityTutorialButton] Auto-click {i + 1}/{autoClickCount}");
                OnButtonClicked();
            }
            else
            {
                Debug.Log("[AbilityTutorialButton] Tutorial completed, stopping auto-click");
                break;
            }
        }
        
        // Notify manager that tutorial is completed
        if (tutorialManager != null && tutorial.IsCompleted)
        {
            Debug.Log("[AbilityTutorialButton] Notifying manager: tutorial completed");
            tutorialManager.OnTutorialCompleted();
        }
    }
    
    /// <summary>
    /// Set tutorial manager (called by AbilityTutorialManager after spawning)
    /// </summary>
    public void SetTutorialManager(AbilityTutorialManager manager)
    {
        tutorialManager = manager;
        Debug.Log("[AbilityTutorialButton] Tutorial manager set");
    }
    
    /// <summary>
    /// Buton tıklandığında çağrılır
    /// </summary>
    private void OnButtonClicked()
    {
        if (tutorial == null)
        {
            Debug.LogError("[AbilityTutorialButton] Tutorial is null!");
            return;
        }
        
        if (tutorial.IsCompleted)
        {
            Debug.Log($"[AbilityTutorialButton] {tutorial.GetAbilityName()} tutorial already completed!");
            return;
        }
        
        // Ability'yi kullan
        tutorial.OnAbilityUsed();
        
        // Play effects AFTER ability is used (stop activated)
        PlayClickEffect();
        
        // UI'ı güncelle
        UpdateButtonUI();
        
        // Tamamlandıysa butonu devre dışı bırak
        if (tutorial.IsCompleted)
        {
            button.interactable = false;
            Debug.Log($"[AbilityTutorialButton] {tutorial.GetAbilityName()} tutorial completed, button disabled");
        }
    }
    
    /// <summary>
    /// Play particle effect and sound on button click (when stop is activated)
    /// </summary>
    private void PlayClickEffect()
    {
        Debug.Log("[AbilityTutorialButton] PlayClickEffect called");
        
        // Play particle effect (1 saniye görünsün)
        if (instantiatedParticle != null)
        {
            StartCoroutine(ShowParticleForDuration());
        }
        else
        {
            Debug.LogWarning("[AbilityTutorialButton] instantiatedParticle is NULL!");
        }
        
        // Play hand click animation
        if (handImage != null)
        {
            handClickCount++;
            StartCoroutine(PlayHandClickAnimation());
            
            // 3 tıklamadan sonra eli gizle
            if (handClickCount >= 3)
            {
                StartCoroutine(HideHandAfterDelay());
            }
        }
        
        // Play sound effect
        if (audioSource != null && buttonClickSound != null)
        {
            Debug.Log($"[AbilityTutorialButton] Playing sound: {buttonClickSound.name}, Volume: {audioSource.volume}");
            audioSource.PlayOneShot(buttonClickSound);
        }
        else
        {
            if (audioSource == null)
                Debug.LogWarning("[AbilityTutorialButton] audioSource is NULL!");
            if (buttonClickSound == null)
                Debug.LogWarning("[AbilityTutorialButton] buttonClickSound is NULL!");
        }
    }
    
    /// <summary>
    /// Particle'ı 1 saniye göster, sonra gizle
    /// </summary>
    private System.Collections.IEnumerator ShowParticleForDuration()
    {
        instantiatedParticle.gameObject.SetActive(true);
        Debug.Log("[AbilityTutorialButton] Particle shown");
        
        yield return new WaitForSeconds(particleDisplayDuration);
        
        instantiatedParticle.gameObject.SetActive(false);
        Debug.Log("[AbilityTutorialButton] Particle hidden");
    }
    
    /// <summary>
    /// El tıklama animasyonu (scale down/up)
    /// </summary>
    private System.Collections.IEnumerator PlayHandClickAnimation()
    {
        if (handImage == null) yield break;
        
        handImage.SetActive(true);
        Vector3 originalScale = handImage.transform.localScale;
        Vector3 clickedScale = originalScale * 0.8f;
        
        // Scale down (tıklama)
        float elapsed = 0f;
        float halfDuration = handClickAnimDuration / 2f;
        
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            handImage.transform.localScale = Vector3.Lerp(originalScale, clickedScale, t);
            yield return null;
        }
        
        // Scale up (bırakma)
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            handImage.transform.localScale = Vector3.Lerp(clickedScale, originalScale, t);
            yield return null;
        }
        
        handImage.transform.localScale = originalScale;
    }
    
    /// <summary>
    /// 3 tıklamadan sonra eli gizle
    /// </summary>
    private System.Collections.IEnumerator HideHandAfterDelay()
    {
        yield return new WaitForSeconds(handClickAnimDuration);
        
        if (handImage != null)
        {
            handImage.SetActive(false);
            Debug.Log("[AbilityTutorialButton] Hand hidden after 3 clicks");
        }
    }
    
    /// <summary>
    /// Buton UI'ını günceller
    /// </summary>
    private void UpdateButtonUI()
    {
        if (tutorial == null) return;
        
        // Buton metnini güncelle
        if (buttonText != null)
        {
            buttonText.text = tutorial.GetAbilityName();
        }
        
        // Maliyet metnini güncelle
        if (costText != null)
        {
            costText.text = $"{tutorial.GetCost()} Coin";
        }
        
        // Açıklama metnini güncelle (sadece typewriter yoksa)
        if (descriptionText != null && typewriterEffect == null)
        {
            descriptionText.text = tutorial.GetDescription();
        }
    }
    
    /// <summary>
    /// Tutorial'ı sıfırlar (test için)
    /// </summary>
    public void ResetTutorial()
    {
        if (tutorial != null)
        {
            tutorial.ResetTutorial();
            button.interactable = true;
            UpdateButtonUI();
        }
    }
    
    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClicked);
        }
        
        // Unsubscribe from typewriter event
        if (typewriterEffect != null)
        {
            typewriterEffect.OnTypingComplete -= OnTypingComplete;
        }
    }
}
