using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Universal Pathfinding ability tutorial implementation.
/// Shows automated collision and pathfinding demonstration in a loop.
/// </summary>
public class UniversalPathfindingTutorial : MonoBehaviour, IAbilityTutorial
{
    [Header("Car Images")]
    [SerializeField] private Transform topCar; // Üstteki araba
    [SerializeField] private Transform bottomCar; // Alttaki araba (hareket eden)
    [SerializeField] private Transform stopImage; // Stop görseli
    
    [Header("Hand Animation")]
    [SerializeField] private GameObject handImage; // El görseli
    [SerializeField] private Transform buttonTransform; // Buton pozisyonu
    
    [Header("Audio")]
    [SerializeField] private AudioClip buttonClickSound; // Buton tıklama sesi
    [SerializeField] private AudioClip collisionSound; // Çarpışma sesi
    
    [Header("Animation Settings")]
    [SerializeField] private float carMoveSpeed = 200f; // Araba hareket hızı
    [SerializeField] private float rotationSpeed = 180f; // Dönüş hızı
    [SerializeField] private float shakeAmount = 10f; // Sallama miktarı
    [SerializeField] private float shakeDuration = 0.3f; // Sallama süresi
    [SerializeField] private float parkDuration = 5f; // Park süresi
    [SerializeField] private float handClickDuration = 0.3f; // El tıklama animasyon süresi
    [SerializeField] private float collisionDistance = 0.9f; // Çarpışma mesafesi (0.9 = %90 overlap)
    [SerializeField] private float driftRadius = 100f; // U-dönüş yarıçapı
    
    [Header("Cost Display")]
    [SerializeField] private TextMeshProUGUI costText;
    
    [Header("Settings")]
    [SerializeField] private AbilityType abilityType = AbilityType.UniversalPathfinding;
    [SerializeField] private string abilityName = "Universal Pathfinding";
    [SerializeField] private string description = "Allow a passenger group to go to any stop. Very useful when you're stuck and need flexibility!\n\n💰 Cost increases with each use:\n1st: 100 | 2nd: 200 | 3rd: 400 | 4th: 800 Coins";
    
    // Private - otomatik doldurulur
    private Vector3 bottomCarStartPos;
    private Vector3 topCarStartPos;
    private Vector3 stopStartPos;
    
    private bool isSkipped = false;
    private Coroutine mainLoop;
    private AudioSource audioSource;
    
    public bool IsCompleted => false; // Sürekli tekrar eder, skip ile durur
    
    private void Start()
    {
        // Save initial positions automatically
        if (bottomCar != null)
            bottomCarStartPos = bottomCar.localPosition;
        if (topCar != null)
            topCarStartPos = topCar.localPosition;
        if (stopImage != null)
            stopStartPos = stopImage.localPosition;
        
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
        
        // Start main loop
        StartMainLoop();
        
        Debug.Log($"[UniversalPathfindingTutorial] Started - Positions saved: Bottom={bottomCarStartPos}, Top={topCarStartPos}, Stop={stopStartPos}");
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
            // 1. Çarpışma animasyonu
            yield return StartCoroutine(CollisionAnimation());
            
            // 2. Geri dönüş
            yield return StartCoroutine(ReturnToStart());
            
            // 3. Buton tıklama animasyonu
            yield return StartCoroutine(HandClickAnimation());
            
            // 4. Pathfinding animasyonu
            yield return StartCoroutine(PathfindingAnimation());
            
            // 5. Park ve reset
            yield return new WaitForSeconds(parkDuration);
            ResetPositions();
            
            // 6. Tekrar başlamadan önce kısa bekle
            yield return new WaitForSeconds(1f);
        }
        
        Debug.Log("[UniversalPathfindingTutorial] Loop stopped (skipped)");
    }
    
    /// <summary>
    /// Çarpışma animasyonu
    /// </summary>
    private IEnumerator CollisionAnimation()
    {
        Debug.Log("========== ÇARPIŞMA BAŞLIYOR ==========");
        
        // Araç boyutlarını al
        RectTransform bottomRect = bottomCar.GetComponent<RectTransform>();
        RectTransform topRect = topCar.GetComponent<RectTransform>();
        
        // Bottom car: Dikey (height kullan)
        // Top car: Yatay (width'i height olarak kullan)
        float bottomCarHeight = bottomRect != null ? bottomRect.rect.height : 50f;
        float topCarHeight = topRect != null ? topRect.rect.width : 50f; // YATAY: width kullan!
        
        Debug.Log($"[1] ARAÇ BOYUTLARI:");
        Debug.Log($"    Bottom Car Height: {bottomCarHeight}");
        Debug.Log($"    Top Car Width (yatay): {topCarHeight}");
        
        Debug.Log($"[2] BAŞLANGIÇ POZİSYONLARI:");
        Debug.Log($"    Bottom Car Start (merkez): {bottomCarStartPos}");
        Debug.Log($"    Top Car Start (merkez): {topCarStartPos}");
        
        // KENAR POZİSYONLARI
        float bottomCarTopEdge = bottomCarStartPos.y + (bottomCarHeight / 2f);
        float topCarBottomEdge = topCarStartPos.y - (topCarHeight / 2f);
        
        Debug.Log($"[3] KENAR POZİSYONLARI:");
        Debug.Log($"    Bottom Car ÜST KENAR: {bottomCarTopEdge}");
        Debug.Log($"    Top Car ALT KENAR: {topCarBottomEdge}");
        
        // Kenarlar arası gap (doğru hesaplama!)
        float gapBetweenEdges = topCarBottomEdge - bottomCarTopEdge;
        
        Debug.Log($"[4] KENARLAR ARASI GAP:");
        Debug.Log($"    Gap = Top alt kenar - Bottom üst kenar");
        Debug.Log($"    Gap = {topCarBottomEdge} - {bottomCarTopEdge}");
        Debug.Log($"    Gap = {gapBetweenEdges}");
        
        // Overlap hesabı
        float overlap = gapBetweenEdges * collisionDistance;
        float remainingGap = gapBetweenEdges - overlap;
        
        Debug.Log($"[5] OVERLAP HESABI:");
        Debug.Log($"    Collision Distance: {collisionDistance} ({collisionDistance * 100}%)");
        Debug.Log($"    Overlap miktarı: {overlap}");
        Debug.Log($"    Kalan gap: {remainingGap}");
        
        // Bottom car'ın hedef Y pozisyonu (üst kenarı hedef gap kadar yaklaşacak)
        float targetTopEdge = topCarBottomEdge - remainingGap;
        float targetY = targetTopEdge - (bottomCarHeight / 2f);
        
        Debug.Log($"[6] HEDEF POZİSYON HESABI:");
        Debug.Log($"    Bottom üst kenar hedefi: {targetTopEdge}");
        Debug.Log($"    Bottom merkez hedefi: {targetY}");
        Debug.Log($"    (Hedef = üst kenar - yarı yükseklik)");
        
        Vector3 targetPos = new Vector3(
            bottomCarStartPos.x,
            targetY,
            bottomCarStartPos.z
        );
        
        Debug.Log($"[7] HEDEF POZİSYON: {targetPos}");
        Debug.Log($"[8] GİDİLECEK MESAFE: {Vector3.Distance(bottomCar.localPosition, targetPos)}");
        Debug.Log("========================================");
        
        while (Vector3.Distance(bottomCar.localPosition, targetPos) > 1f)
        {
            bottomCar.localPosition = Vector3.MoveTowards(
                bottomCar.localPosition,
                targetPos,
                carMoveSpeed * Time.deltaTime
            );
            yield return null;
        }
        
        Debug.Log($"[ÇARPIŞMA] Bottom car hedef pozisyona ulaştı: {bottomCar.localPosition}");
        Debug.Log($"[ÇARPIŞMA] Bottom üst kenar final: {bottomCar.localPosition.y + (bottomCarHeight / 2f)}");
        
        // Çarpışma sesi çal
        if (audioSource != null && collisionSound != null)
        {
            audioSource.PlayOneShot(collisionSound);
        }
        
        // Çarpışma - Üstteki araba sallan
        yield return StartCoroutine(ShakeCar(topCar));
    }
    
    /// <summary>
    /// Arabayı sallar (kaza efekti)
    /// </summary>
    private IEnumerator ShakeCar(Transform car)
    {
        Vector3 originalPos = car.localPosition;
        float elapsed = 0f;
        
        while (elapsed < shakeDuration)
        {
            float x = originalPos.x + Random.Range(-shakeAmount, shakeAmount);
            float y = originalPos.y + Random.Range(-shakeAmount, shakeAmount);
            
            car.localPosition = new Vector3(x, y, originalPos.z);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        car.localPosition = originalPos;
    }
    
    /// <summary>
    /// Alttaki araba başlangıç pozisyonuna döner
    /// </summary>
    private IEnumerator ReturnToStart()
    {
        while (Vector3.Distance(bottomCar.localPosition, bottomCarStartPos) > 1f)
        {
            bottomCar.localPosition = Vector3.MoveTowards(
                bottomCar.localPosition,
                bottomCarStartPos,
                carMoveSpeed * Time.deltaTime
            );
            yield return null;
        }
    }
    
    /// <summary>
    /// El ile buton tıklama animasyonu
    /// </summary>
    private IEnumerator HandClickAnimation()
    {
        if (handImage == null || buttonTransform == null) yield break;
        
        // Eli göster
        handImage.SetActive(true);
        
        // Eli butonun üzerine getir
        handImage.transform.position = buttonTransform.position;
        
        // Tıklama animasyonu (scale down/up)
        Vector3 originalScale = handImage.transform.localScale;
        Vector3 clickedScale = originalScale * 0.8f;
        
        // Scale down
        float elapsed = 0f;
        float halfDuration = handClickDuration / 2f;
        
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            handImage.transform.localScale = Vector3.Lerp(originalScale, clickedScale, t);
            yield return null;
        }
        
        // Play button click sound
        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
        
        // Scale up
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            handImage.transform.localScale = Vector3.Lerp(clickedScale, originalScale, t);
            yield return null;
        }
        
        handImage.transform.localScale = originalScale;
        
        // Kısa bekle
        yield return new WaitForSeconds(0.2f);
        
        // Eli alttaki arabaya getir
        handImage.transform.position = bottomCar.position;
        
        // Tekrar tıklama animasyonu
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            handImage.transform.localScale = Vector3.Lerp(originalScale, clickedScale, t);
            yield return null;
        }
        
        // Play car click sound
        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
        
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            handImage.transform.localScale = Vector3.Lerp(clickedScale, originalScale, t);
            yield return null;
        }
        
        handImage.transform.localScale = originalScale;
        
        // Eli gizle
        handImage.SetActive(false);
    }
    
    /// <summary>
    /// Pathfinding animasyonu: Top car'a temas etmeden park et
    /// Sağa kaç → Yukarı çık → Sola dön → Park
    /// </summary>
    private IEnumerator PathfindingAnimation()
    {
        float currentRotation = 0f;
        
        // 1. Sağa dön (90 derece)
        float targetRotation = 90f;
        
        while (Mathf.Abs(currentRotation - targetRotation) > 1f)
        {
            currentRotation = Mathf.MoveTowards(currentRotation, targetRotation, rotationSpeed * Time.deltaTime);
            bottomCar.localRotation = Quaternion.Euler(0, 0, currentRotation);
            yield return null;
        }
        
        // 2. Sağa git (top car'dan kaç)
        // Stop'un x pozisyonundan biraz daha sağa git
        float safeDistance = driftRadius; // Güvenli mesafe
        Vector3 rightTarget = new Vector3(stopStartPos.x + safeDistance, bottomCar.localPosition.y, bottomCar.localPosition.z);
        
        while (Vector3.Distance(bottomCar.localPosition, rightTarget) > 1f)
        {
            bottomCar.localPosition = Vector3.MoveTowards(
                bottomCar.localPosition,
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
            bottomCar.localRotation = Quaternion.Euler(0, 0, currentRotation);
            yield return null;
        }
        
        // 4. Yukarı git (stop'un y pozisyonuna)
        Vector3 upTarget = new Vector3(bottomCar.localPosition.x, stopStartPos.y, bottomCar.localPosition.z);
        
        while (Vector3.Distance(bottomCar.localPosition, upTarget) > 1f)
        {
            bottomCar.localPosition = Vector3.MoveTowards(
                bottomCar.localPosition,
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
            bottomCar.localRotation = Quaternion.Euler(0, 0, currentRotation);
            yield return null;
        }
        
        // 6. Sola git (park noktasına)
        Vector3 parkTarget = stopStartPos;
        
        while (Vector3.Distance(bottomCar.localPosition, parkTarget) > 1f)
        {
            bottomCar.localPosition = Vector3.MoveTowards(
                bottomCar.localPosition,
                parkTarget,
                carMoveSpeed * Time.deltaTime
            );
            yield return null;
        }
    }
    
    /// <summary>
    /// Pozisyonları sıfırla
    /// </summary>
    private void ResetPositions()
    {
        if (bottomCar != null)
        {
            bottomCar.localPosition = bottomCarStartPos;
            bottomCar.localRotation = Quaternion.identity;
        }
        
        if (topCar != null)
        {
            topCar.localPosition = topCarStartPos;
            topCar.localRotation = Quaternion.identity;
        }
        
        if (stopImage != null)
        {
            stopImage.localPosition = stopStartPos;
        }
        
        if (handImage != null)
        {
            handImage.SetActive(false);
        }
    }
    
    /// <summary>
    /// Ability kullanıldığında (manuel - kullanılmıyor, otomatik çalışıyor)
    /// </summary>
    public void OnAbilityUsed()
    {
        // Bu tutorial otomatik çalışır, manuel kullanım yok
        Debug.Log("[UniversalPathfindingTutorial] OnAbilityUsed called (auto mode, ignored)");
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
        
        Debug.Log("[UniversalPathfindingTutorial] Skipped");
    }
    
    /// <summary>
    /// Maliyet metnini günceller
    /// </summary>
    private void UpdateCostDisplay()
    {
        if (costText != null)
        {
            costText.text = "100 Coin";
        }
    }
    
    /// <summary>
    /// Tutorial'ı sıfırlar
    /// </summary>
    public void ResetTutorial()
    {
        isSkipped = false;
        ResetPositions();
        UpdateCostDisplay();
        StartMainLoop();
        
        Debug.Log("[UniversalPathfindingTutorial] Tutorial reset");
    }
    
    public int GetCost() => 100;
    public string GetAbilityName() => abilityName;
    public string GetDescription() => description;
    
    private void OnDestroy()
    {
        if (mainLoop != null)
            StopCoroutine(mainLoop);
    }
}
