using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Remove Wagons (Flasher) ability tutorial implementation.
/// Döngüsel olarak: ability button tıkla → parked car'a tıkla → pathfinding ile park et → tekrarla
/// </summary>
public class RemoveWagonsTutorial : MonoBehaviour, IAbilityTutorial
{
    [Header("Car Images")]
    [SerializeField] private Transform parkedCar; // Park edilmiş araba (hareket edecek)
    [SerializeField] private Transform parkImage; // Park yeri (hedef)
    [SerializeField] private Transform obstacle1; // 1. engel
    [SerializeField] private Transform obstacle2; // 2. engel
    
    [Header("Hand Animation")]
    [SerializeField] private GameObject handImage; // El görseli
    [SerializeField] private Transform buttonTransform; // Ability button pozisyonu
    
    [Header("Audio")]
    [SerializeField] private AudioClip buttonClickSound; // Buton tıklama sesi
    [SerializeField] private AudioClip carClickSound; // Araba tıklama sesi
    
    [Header("Animation Settings")]
    [SerializeField] private float carMoveSpeed = 200f; // Araba hareket hızı
    [SerializeField] private float rotationSpeed = 180f; // Dönüş hızı
    [SerializeField] private float parkDuration = 3f; // Park süresi (döngü arası bekleme)
    [SerializeField] private float handClickDuration = 0.3f; // El tıklama animasyon süresi
    [SerializeField] private float waitAfterAbilityClick = 1f; // Ability tıklamasından sonra bekleme
    
    [Header("Cost Display")]
    [SerializeField] private TextMeshProUGUI costText;
    
    [Header("Settings")]
    [SerializeField] private AbilityType abilityType = AbilityType.RemoveWagons;
    [SerializeField] private string abilityName = "Remove Wagons (Flasher)";
    [SerializeField] private string description = "Remove a specific passenger group from the map. Use this to clear space when you're stuck.\\n\\n💰 Cost increases with each use:\\n1st: 100 | 2nd: 200 | 3rd: 400 | 4th: 800 Coins";
    
    // Private - otomatik doldurulur
    private Vector3 parkedCarStartPos;
    private Vector3 parkStartPos;
    private Vector3 obstacle1StartPos;
    private Vector3 obstacle2StartPos;
    
    private bool isSkipped = false;
    private Coroutine mainLoop;
    private AudioSource audioSource;
    private int currentCost = 100; // Başlangıç maliyeti, her döngüde 2 katına çıkar
    
    public bool IsCompleted => false; // Sürekli tekrar eder, skip ile durur
    
    private void Start()
    {
        // Save initial positions automatically
        if (parkedCar != null)
            parkedCarStartPos = parkedCar.localPosition;
        if (parkImage != null)
            parkStartPos = parkImage.localPosition;
        if (obstacle1 != null)
            obstacle1StartPos = obstacle1.localPosition;
        if (obstacle2 != null)
            obstacle2StartPos = obstacle2.localPosition;
        
        // Hide hand initially
        if (handImage != null)
            handImage.SetActive(false);
        
        // Setup AudioSource
        if (buttonClickSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.clip = buttonClickSound;
        }
        
        UpdateCostDisplay();
        
        // Don't start main loop here - AbilityTutorialButton will trigger it
        // via OnAbilityUsed() after typewriter completes
        
        /* Debug.Log($"[RemoveWagonsTutorial] Started - Waiting for AbilityTutorialButton to trigger animations"); */
    }
    
    /// <summary>
    /// Ana döngüyü başlat (skip edilene kadar tekrar eder)
    /// </summary>
    private void StartMainLoop()
    {
        if (mainLoop != null)
            StopCoroutine(mainLoop);
            
        mainLoop = StartCoroutine(MainLoopCoroutine());
    }
    
    /// <summary>
    /// Ana döngü - Skip edilene kadar sürekli tekrar eder
    /// </summary>
    private IEnumerator MainLoopCoroutine()
    {
        while (!isSkipped)
        {
            // 1. Ability button'a tıklama animasyonu
            yield return StartCoroutine(HandClickOnButton());
            
            // 2. 1 saniye bekle
            yield return new WaitForSeconds(waitAfterAbilityClick);
            
            // 3. Parked car'a tıklama animasyonu
            yield return StartCoroutine(HandClickOnCar());
            
            // 4. Pathfinding animasyonu (engelleri geçerek park et)
            yield return StartCoroutine(PathfindingAnimation());
            
            // 5. Park süresi bekle
            yield return new WaitForSeconds(parkDuration);
            
            // 6. Pozisyonları sıfırla
            ResetPositions();
            
            // 7. Cost'u 2 katına çıkar ve güncelle
            currentCost *= 2;
            UpdateCostDisplay();
            /* Debug.Log($"[RemoveWagonsTutorial] Cost doubled to: {currentCost}"); */
            
            // 8. Tekrar başlamadan önce kısa bekle
            yield return new WaitForSeconds(1f);
        }
        
        /* Debug.Log("[RemoveWagonsTutorial] Loop stopped (skipped)"); */
    }
    
    /// <summary>
    /// El ile ability button'a tıklama animasyonu
    /// </summary>
    private IEnumerator HandClickOnButton()
    {
        if (handImage == null || buttonTransform == null) yield break;
        
        // Eli göster ve butonun üzerine getir
        handImage.SetActive(true);
        handImage.transform.position = buttonTransform.position;
        
        // Tıklama animasyonu
        yield return StartCoroutine(PlayHandClickAnimation());
        
        // Ses çal
        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
        
        /* Debug.Log("[RemoveWagonsTutorial] Hand clicked on ability button"); */
    }
    
    /// <summary>
    /// El ile parked car'a tıklama animasyonu
    /// </summary>
    private IEnumerator HandClickOnCar()
    {
        if (handImage == null || parkedCar == null) yield break;
        
        // Eli arabaya getir
        handImage.transform.position = parkedCar.position;
        
        // Tıklama animasyonu
        yield return StartCoroutine(PlayHandClickAnimation());
        
        // Ses çal
        if (audioSource != null && carClickSound != null)
        {
            audioSource.PlayOneShot(carClickSound);
        }
        else if (audioSource != null && buttonClickSound != null)
        {
            // Fallback: button click sound kullan
            audioSource.PlayOneShot(buttonClickSound);
        }
        
        // Eli gizle
        handImage.SetActive(false);
        
        /* Debug.Log("[RemoveWagonsTutorial] Hand clicked on parked car"); */
    }
    
    /// <summary>
    /// El tıklama animasyonu (scale down/up)
    /// </summary>
    private IEnumerator PlayHandClickAnimation()
    {
        if (handImage == null) yield break;
        
        Vector3 originalScale = handImage.transform.localScale;
        Vector3 clickedScale = originalScale * 0.8f;
        
        float halfDuration = handClickDuration / 2f;
        
        // Scale down (tıklama)
        float elapsed = 0f;
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
    /// Pathfinding animasyonu: Engelleri geçerek park yerine git
    /// Mantık: Sağa kaç → Yukarı çık → Sola dön → Park
    /// </summary>
    private IEnumerator PathfindingAnimation()
    {
        float currentRotation = 0f;
        
        // 1. Sağa dön (90 derece)
        float targetRotation = 90f;
        
        while (Mathf.Abs(currentRotation - targetRotation) > 1f)
        {
            currentRotation = Mathf.MoveTowards(currentRotation, targetRotation, rotationSpeed * Time.deltaTime);
            parkedCar.localRotation = Quaternion.Euler(0, 0, currentRotation);
            yield return null;
        }
        
        // 2. Sağa git (engelleri geç)
        float safeDistance = 150f; // Engelleri geçmek için yeterli mesafe
        Vector3 rightTarget = new Vector3(parkStartPos.x + safeDistance, parkedCar.localPosition.y, parkedCar.localPosition.z);
        
        while (Vector3.Distance(parkedCar.localPosition, rightTarget) > 1f)
        {
            parkedCar.localPosition = Vector3.MoveTowards(
                parkedCar.localPosition,
                rightTarget,
                carMoveSpeed * Time.deltaTime
            );
            yield return null;
        }
        
        // 3. Yukarı dön (0 derece)
        targetRotation = 0f;
        
        while (Mathf.Abs(currentRotation - targetRotation) > 1f)
        {
            currentRotation = Mathf.MoveTowards(currentRotation, targetRotation, rotationSpeed * Time.deltaTime);
            parkedCar.localRotation = Quaternion.Euler(0, 0, currentRotation);
            yield return null;
        }
        
        // 4. Yukarı git (park yerinin y pozisyonuna)
        Vector3 upTarget = new Vector3(parkedCar.localPosition.x, parkStartPos.y, parkedCar.localPosition.z);
        
        while (Vector3.Distance(parkedCar.localPosition, upTarget) > 1f)
        {
            parkedCar.localPosition = Vector3.MoveTowards(
                parkedCar.localPosition,
                upTarget,
                carMoveSpeed * Time.deltaTime
            );
            yield return null;
        }
        
        // 5. Sola dön (-90 derece)
        targetRotation = -90f;
        
        while (Mathf.Abs(currentRotation - targetRotation) > 1f)
        {
            currentRotation = Mathf.MoveTowards(currentRotation, targetRotation, rotationSpeed * Time.deltaTime);
            parkedCar.localRotation = Quaternion.Euler(0, 0, currentRotation);
            yield return null;
        }
        
        // 6. Sola git (park noktasına)
        Vector3 parkTarget = parkStartPos;
        
        while (Vector3.Distance(parkedCar.localPosition, parkTarget) > 1f)
        {
            parkedCar.localPosition = Vector3.MoveTowards(
                parkedCar.localPosition,
                parkTarget,
                carMoveSpeed * Time.deltaTime
            );
            yield return null;
        }
        
        /* Debug.Log("[RemoveWagonsTutorial] Pathfinding complete - Car parked!"); */
    }
    
    /// <summary>
    /// Pozisyonları sıfırla
    /// </summary>
    private void ResetPositions()
    {
        if (parkedCar != null)
        {
            parkedCar.localPosition = parkedCarStartPos;
            // Reset to -90 degrees (facing left, parked state) instead of 0 (facing up)
            parkedCar.localRotation = Quaternion.Euler(0, 0, -90f);
        }
        
        if (parkImage != null)
        {
            parkImage.localPosition = parkStartPos;
        }
        
        if (obstacle1 != null)
        {
            obstacle1.localPosition = obstacle1StartPos;
        }
        
        if (obstacle2 != null)
        {
            obstacle2.localPosition = obstacle2StartPos;
        }
        
        if (handImage != null)
        {
            handImage.SetActive(false);
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
            /* Debug.Log("[RemoveWagonsTutorial] OnAbilityUsed - Starting main loop"); */
            StartMainLoop();
        }
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
        
        /* Debug.Log("[RemoveWagonsTutorial] Skipped"); */
    }
    
    /// <summary>
    /// Maliyet metnini günceller
    /// </summary>
    private void UpdateCostDisplay()
    {
        if (costText != null)
        {
            costText.text = $"{currentCost} Coin";
        }
    }
    
    /// <summary>
    /// Tutorial'ı sıfırlar
    /// </summary>
    public void ResetTutorial()
    {
        isSkipped = false;
        currentCost = 100; // Reset cost to initial value
        ResetPositions();
        UpdateCostDisplay();
        StartMainLoop();
        
        /* Debug.Log("[RemoveWagonsTutorial] Tutorial reset"); */
    }
    
    public int GetCost() => currentCost;
    public string GetAbilityName() => abilityName;
    public string GetDescription() => description;
    
    private void OnDestroy()
    {
        if (mainLoop != null)
            StopCoroutine(mainLoop);
    }
}
