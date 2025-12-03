using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Level 1'de oyuncuya oyun mekaniklerini öğreten tutorial sistemi.
/// </summary>
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("Tutorial Settings")]
    [SerializeField] private bool enableTutorial = true;
    [SerializeField] private int tutorialLevel = 0; // Tutorial sadece bu levelde çalışır (Level 0 = İlk Level)

    [Header("UI References")]
    [SerializeField] private GameObject tutorialCanvas;
    [SerializeField] private RectTransform handIcon;
    [SerializeField] private Image darkOverlay;
    [SerializeField] private GameObject highlightCircle;
    [SerializeField] private TMP_Text tutorialText;

    private enum TutorialStep
    {
        None,
        WaitingForStart,
        ClickFirstPassenger,
        WaitForPassengerAtStop,
        WaitForBoarding,
        TutorialComplete
    }

    [Header("Tutorial Steps")]
    [SerializeField] private TutorialStep currentStep = TutorialStep.None;
    private PassengerGroup targetPassenger;
    private bool tutorialCompleted = false;
    private bool isInputBlocked = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // Inspector hatasını önlemek için tutorialLevel'i zorla 0 yap
        tutorialLevel = 0;

        // Tutorial'ın daha önce tamamlanıp tamamlanmadığını kontrol et
        if (GameDataManager.Instance != null && GameDataManager.Instance.GetSaveData() != null)
        {
            tutorialCompleted = GameDataManager.Instance.GetSaveData().isTutorialShown;
        }
        else
        {
            Debug.LogWarning("[TutorialManager] GameDataManager not found or Data is null. Defaulting tutorialCompleted to false.");
            tutorialCompleted = false;
        }

        // Tutorial gerekli mi kontrol et
        string disableReason;
        if (!ShouldShowTutorial(out disableReason))
        {
            DisableTutorial(disableReason);
            return;
        }

        // Tutorial UI'ını başlat
        InitializeTutorialUI();

        // Event'leri dinle
        StopManager.OnPassengerArrivedAtStop += OnPassengerArrivedAtStop;
        
        // Tutorial'ı başlat
        StartCoroutine(StartTutorialSequence());
    }

    private void OnDestroy()
    {
        StopManager.OnPassengerArrivedAtStop -= OnPassengerArrivedAtStop;
    }

    private bool ShouldShowTutorial(out string reason)
    {
        reason = "";

        if (!enableTutorial) 
        {
            reason = "Tutorial disabled via inspector 'Enable Tutorial' setting.";
            return false;
        }
        
        if (tutorialCompleted) 
        {
            reason = "Tutorial already completed (isTutorialShown is true).";
            return false;
        }
        
        // Sadece belirtilen levelde tutorial göster
        if (GameDataManager.Instance != null && GameDataManager.Instance.GetSaveData() != null)
        {
            int currentLevelIndex = GameDataManager.Instance.GetSaveData().levelIndex;
            if (currentLevelIndex != tutorialLevel)
            {
                reason = $"Current Level Index ({currentLevelIndex}) does not match Tutorial Level ({tutorialLevel}).";
                return false;
            }
        }
        else
        {
            Debug.LogWarning("[TutorialManager] GameDataManager or SaveData is null! Skipping level check.");
            reason = "GameDataManager or SaveData is null.";
            return false;
        }

        Debug.Log("[TutorialManager] Tutorial conditions met. Starting tutorial.");
        return true;
    }

    private void InitializeTutorialUI()
    {
        if (tutorialCanvas != null)
        {
            tutorialCanvas.SetActive(false); // Initially hidden
        }

        if (darkOverlay != null)
        {
            darkOverlay.color = new Color(0, 0, 0, 0.7f);
            darkOverlay.raycastTarget = false;
            darkOverlay.gameObject.SetActive(false); // Initially hidden
        }

        if (handIcon != null)
        {
            handIcon.gameObject.SetActive(false);
        }

        if (highlightCircle != null)
        {
            highlightCircle.SetActive(false);
        }

        if (tutorialText != null)
        {
            tutorialText.text = "";
        }
    }

    private void DisableTutorial(string reason = "")
    {
        if (!string.IsNullOrEmpty(reason))
        {
            Debug.LogWarning($"[TutorialManager] Disabling tutorial. Reason: {reason}");
        }
        
        if (tutorialCanvas != null)
        {
            tutorialCanvas.SetActive(false);
        }
        enabled = false;
    }

    private IEnumerator StartTutorialSequence()
    {    // IMPORTANT: Disable ALL input at start
    if (InputManager.Instance != null)
    {
        InputManager.Instance.DisableInput();
        Debug.Log("[TutorialManager] ALL INPUT DISABLED for 3 seconds.");
    }
        // 1. Oyunun başlamasını ve vagonların ilerlemesini bekle (3 saniye)
        yield return new WaitForSeconds(3f);

        currentStep = TutorialStep.WaitingForStart;
        
        // Vagonları durdur
        SetMetroMovementActive(false);

        // 2. İlk adım: Bilgilendirme ve İlk Passenger
        yield return StartCoroutine(Step1_ClickFirstPassenger());

        // 3. İkinci adım: Boarding'i bekle
        yield return StartCoroutine(Step2_WaitForBoardingAndSecondPassenger());

        // Tutorial tamamlandı
        CompleteTutorial();
    }

    private IEnumerator Step1_ClickFirstPassenger()
    {
        currentStep = TutorialStep.ClickFirstPassenger;
        
        Debug.Log("[Tutorial] Step 1: Click First Passenger");

        // Passengerları bul ve sırala
        List<PassengerGroup> passengers = GetSortedPassengers();
        if (passengers.Count > 0)
        {
            targetPassenger = passengers[0];
        }
        
        if (targetPassenger == null)
        {
            Debug.LogError("[Tutorial] No passenger found for tutorial!");
            SetMetroMovementActive(true);
            CompleteTutorial();
            yield break;
        }

        // Overlay ve Metni göster
        if (darkOverlay != null) darkOverlay.gameObject.SetActive(true);
        if (tutorialCanvas != null) tutorialCanvas.SetActive(true);

        if (tutorialText != null)
        {
            tutorialText.text = "Don't lose passengers to VIP cars!\nSend a car to the stop!";
        }

        // Passenger'ı highlight et ve El animasyonunu başlat
        ShowHighlight(targetPassenger.transform);
        ShowHandAnimation(targetPassenger.transform);
if (InputManager.Instance != null)
{
    InputManager.Instance.EnableInput();
    Debug.Log("[TutorialManager] INPUT ENABLED - panel shown.");
}
        // Input'u blokla
        isInputBlocked = true;
        InputManager.OnPassengerGroupTapped += OnTutorialPassengerTapped;

        // Tıklanana kadar bekle
        yield return new WaitUntil(() => currentStep != TutorialStep.ClickFirstPassenger);

        // --- Tıklama Sonrası ---
        
        // Input dinlemeyi bırak
        InputManager.OnPassengerGroupTapped -= OnTutorialPassengerTapped;
        
        // Görselleri kapat
        HideHandAnimation();
        HideHighlight();
        if (darkOverlay != null) darkOverlay.gameObject.SetActive(false);
        if (tutorialText != null) tutorialText.text = "";

        // Vagonları tekrar hareket ettir
        SetMetroMovementActive(true);
        
        // Input bloğunu kaldır
        isInputBlocked = false;
        
        Debug.LogWarning("<color=green>[Tutorial] Step1_ClickFirstPassenger COMPLETED!</color>");
        
        // *** TUTORIAL TAMAMLANDI OLARAK İŞARETLE ***
        // Step1 tamamlandığında tutorial'ı "gösterildi" olarak kaydet
        // Böylece oyuncu bir daha tutorial görmez
        tutorialCompleted = true;
        
        if (GameDataManager.Instance != null && GameDataManager.Instance.GetSaveData() != null)
        {
            GameDataManager.Instance.GetSaveData().isTutorialShown = true;
            GameDataManager.Instance.SaveGame();
            Debug.LogWarning("<color=yellow>[Tutorial] ✓ isTutorialShown = TRUE saved after Step1!</color>");
        }
        else
        {
            Debug.LogWarning("<color=red>[Tutorial] ✗ GameDataManager or SaveData is null! Could not save tutorial completion!</color>");
        }
    }

    private IEnumerator Step2_WaitForBoardingAndSecondPassenger()
    {
        currentStep = TutorialStep.WaitForBoarding;
        Debug.Log("[Tutorial] Step 2: Wait For Boarding");

        // İlk passenger dolana kadar bekle
        PassengerGroup firstPassenger = targetPassenger;
        yield return new WaitUntil(() => firstPassenger == null || !firstPassenger.gameObject.activeInHierarchy || firstPassenger.GroupSize <= 0);

        Debug.Log("[Tutorial] First passenger boarded/left. Finding second passenger...");

        // --- İkinci Passenger ---
        
        // Vagonları tekrar durdur
        SetMetroMovementActive(false);

        // İkinci passenger'ı bulmak için biraz bekle (sahne güncellensin)
        yield return new WaitForSeconds(0.5f);

        PassengerGroup secondTarget = null;
        
        // Doğru passenger'ı bulana kadar dene (maksimum 5 saniye)
        float timeout = 5f;
        float timer = 0f;

        while (secondTarget == null && timer < timeout)
        {
            List<PassengerGroup> passengers = GetSortedPassengers();
            Debug.LogWarning($"[Tutorial] Search attempt {timer}: Found {passengers.Count} candidates.");

            foreach(var p in passengers)
            {
                if (p == firstPassenger) 
                {
                    Debug.LogWarning($"[Tutorial] Skipping {p.name}: Is first passenger.");
                    continue;
                }
                if (p.GroupSize <= 0)
                {
                    Debug.LogWarning($"[Tutorial] Skipping {p.name}: GroupSize is {p.GroupSize}.");
                    continue;
                }
                if (p.onConveyorBelt || p.fromConveyor)
                {
                    Debug.LogWarning($"[Tutorial] Skipping {p.name}: On conveyor belt.");
                    continue;
                }

                // Uygun aday bulundu
                Debug.LogWarning($"[Tutorial] Found suitable second passenger: {p.name}");
                secondTarget = p;
                break;
            }

            if (secondTarget == null)
            {
                Debug.LogWarning("[Tutorial] No suitable passenger found in this attempt. Retrying...");
                yield return new WaitForSeconds(0.5f);
                timer += 0.5f;
            }
        }
        
        if (secondTarget != null)
        {
            targetPassenger = secondTarget;
            
            Debug.Log($"[Tutorial] Step 2.5: Click Second Passenger ({targetPassenger.name})");
            
            // Canvas'ı ve El animasyonunu göster
            if (tutorialCanvas != null) tutorialCanvas.SetActive(true);
            ShowHandAnimation(targetPassenger.transform);
            
            // Input'u tekrar blokla
            isInputBlocked = true;
            InputManager.OnPassengerGroupTapped += OnTutorialPassengerTapped;

            // Tıklanana kadar bekle
            // Not: OnTutorialPassengerTapped, currentStep'i WaitForPassengerAtStop yapacak.
            // Bu yüzden currentStep'i ClickFirstPassenger olarak ayarlıyoruz.
            currentStep = TutorialStep.ClickFirstPassenger; 
            
            yield return new WaitUntil(() => currentStep != TutorialStep.ClickFirstPassenger);
            
            InputManager.OnPassengerGroupTapped -= OnTutorialPassengerTapped;
            HideHandAnimation();
            
            // Vagonları başlat
            SetMetroMovementActive(true);
            
            // *** ÖNEMLİ: İkinci passenger'ın da boarding yapmasını bekle! ***
            Debug.LogWarning($"[Tutorial] Waiting for second passenger ({targetPassenger.name}) to complete boarding...");
            PassengerGroup secondPassenger = targetPassenger;
            yield return new WaitUntil(() => secondPassenger == null || !secondPassenger.gameObject.activeInHierarchy || secondPassenger.GroupSize <= 0);
            Debug.LogWarning("[Tutorial] Second passenger boarding completed!");
        }
        else
        {
            Debug.LogWarning("[Tutorial] Could not find second passenger!");
            SetMetroMovementActive(true);
        }
        
        Debug.LogWarning("<color=green>[Tutorial] Step2_WaitForBoardingAndSecondPassenger COMPLETED!</color>");
    }

    private void OnTutorialPassengerTapped(PassengerGroup tappedPassenger)
    {
        if (currentStep != TutorialStep.ClickFirstPassenger) return;

        if (tappedPassenger == targetPassenger)
        {
            Debug.Log("[Tutorial] Correct passenger tapped!");
            currentStep = TutorialStep.WaitForPassengerAtStop; // Döngüden çıkmak için durumu değiştiriyoruz
        }
    }

    private void OnPassengerArrivedAtStop(PassengerGroup passenger, int stopIndex)
    {
        // Event listener
    }

    private void SetMetroMovementActive(bool isActive)
    {
        if (isActive)
        {
            MetroManager.StartMovement();
        }
        else
        {
            MetroManager.StopMovement();
        }
    }

    private List<PassengerGroup> GetSortedPassengers()
    {
        PassengerGroup[] allPassengers = FindObjectsOfType<PassengerGroup>();
        List<PassengerGroup> sortedList = new List<PassengerGroup>();

        foreach (var passenger in allPassengers)
        {
            if (!passenger.onConveyorBelt && !passenger.fromConveyor)
            {
                sortedList.Add(passenger);
            }
        }

        sortedList.Sort((a, b) => Vector3.Distance(a.transform.position, Vector3.zero).CompareTo(Vector3.Distance(b.transform.position, Vector3.zero)));

        return sortedList;
    }

    private void ShowHighlight(Transform target)
    {
        if (highlightCircle == null) return;

        highlightCircle.SetActive(true);
        StartCoroutine(UpdateHighlightPosition(target));
        
        highlightCircle.transform.DOScale(Vector3.one * 1.2f, 0.8f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }

    private void HideHighlight()
    {
        if (highlightCircle == null) return;

        highlightCircle.transform.DOKill();
        highlightCircle.SetActive(false);
        StopAllCoroutines();
    }

    private IEnumerator UpdateHighlightPosition(Transform target)
    {
        while (highlightCircle.activeSelf && target != null)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(target.position);
            highlightCircle.transform.position = screenPos;
            yield return null;
        }
    }

    private void ShowHandAnimation(Transform target)
    {
        if (handIcon == null) return;

        handIcon.gameObject.SetActive(true);
        StartCoroutine(AnimateHand(target));
    }

    private void HideHandAnimation()
    {
        if (handIcon == null) return;

        handIcon.DOKill();
        handIcon.gameObject.SetActive(false);
    }

    private IEnumerator AnimateHand(Transform target)
    {
        while (handIcon.gameObject.activeSelf && target != null)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(target.position);
            
            Vector3 startPos = screenPos + new Vector3(50f, 100f, 0f);
            Vector3 endPos = screenPos + new Vector3(0f, 20f, 0f);

            handIcon.position = startPos;
            
            Sequence handSequence = DOTween.Sequence();
            handSequence.Append(handIcon.DOMove(endPos, 0.8f).SetEase(Ease.InOutSine));
            handSequence.Join(handIcon.DOScale(Vector3.one * 0.9f, 0.8f).SetEase(Ease.InOutSine));
            handSequence.Append(handIcon.DOMove(startPos, 0.8f).SetEase(Ease.InOutSine));
            handSequence.Join(handIcon.DOScale(Vector3.one, 0.8f).SetEase(Ease.InOutSine));
            
            yield return handSequence.WaitForCompletion();
            yield return new WaitForSeconds(0.3f);
        }
    }

    private void CompleteTutorial()
    {
        currentStep = TutorialStep.TutorialComplete;

        Debug.LogWarning("<color=yellow>[Tutorial] Tutorial completed!</color>");

        // Tutorial zaten Step1'de kaydedildi, tekrar kaydetmeye gerek yok
        if (GameDataManager.Instance != null && GameDataManager.Instance.GetSaveData() != null)
        {
            if (!GameDataManager.Instance.GetSaveData().isTutorialShown)
            {
                // Eğer bir şekilde Step1'de kaydedilmemişse burada kaydet
                GameDataManager.Instance.GetSaveData().isTutorialShown = true;
                GameDataManager.Instance.SaveGame();
                Debug.LogWarning("[Tutorial] Saved tutorial completion to GameDataManager (fallback).");
            }
            else
            {
                Debug.LogWarning("[Tutorial] Tutorial already saved in Step1, skipping duplicate save.");
            }
        }
        else
        {
            Debug.LogWarning("[Tutorial] GameDataManager or SaveData is null!");
        }

        if (tutorialCanvas != null) tutorialCanvas.SetActive(false);
        if (darkOverlay != null) darkOverlay.gameObject.SetActive(false);

        isInputBlocked = false;
        SetMetroMovementActive(true);
    }

    public bool IsInputBlocked()
    {
        return isInputBlocked && currentStep != TutorialStep.None && currentStep != TutorialStep.TutorialComplete;
    }

    [ContextMenu("Reset Tutorial")]
    public void ResetTutorial()
    {
        if (GameDataManager.Instance != null && GameDataManager.Instance.GetSaveData() != null)
        {
            GameDataManager.Instance.GetSaveData().isTutorialShown = false;
            GameDataManager.Instance.SaveGame();
            Debug.Log("[Tutorial] Tutorial reset via GameDataManager!");
        }
        
        PlayerPrefs.DeleteKey("TutorialCompleted");
    }
}
