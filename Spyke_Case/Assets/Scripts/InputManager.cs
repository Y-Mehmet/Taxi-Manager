using UnityEngine;
using System;
using UnityEngine.EventSystems; // UI kontrolÃ¼ iÃ§in eklendi

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    public static event Action<PassengerGroup> OnPassengerGroupTapped;

    private bool isInputDisabled = false;
    private bool initialTapDone = false; // Flag for the one-time tap-to-start mechanic

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
    }

    private void OnEnable()
    {
        UberManager.OnGameOver += DisableInput;
        UIManager.OnSpeedToggleClicked += ToggleSpeed;
    }

    private void OnDisable()
    {
        // Null kontrolÃ¼, oyun sonu/kapanÄ±ÅŸÄ± senaryolarÄ±nda Ã¶nemlidir.
        if (UberManager.Instance != null)
        {
             UberManager.OnGameOver -= DisableInput;
        }
        UIManager.OnSpeedToggleClicked -= ToggleSpeed;
    }

    void Update()
    {
        if (isInputDisabled) return;

        // TÄ±klama/Dokunma tespitini tek bir yerden yapalÄ±m.
        // Hem fare tÄ±klamasÄ± hem de mobil dokunma iÃ§in 'GetInputDown' adÄ±nda yeni bir metot kullanacaÄŸÄ±z.

        if (GetInputDown(out Vector3 screenPosition))
        {
            // --- TÄ±klama Efekti ---
            // Her tÄ±klamada/dokunmada ClickEffectManager'a ekran pozisyonunu gÃ¶ndererek efekti oynat.
            // Bu efektin UI katmanÄ±nda Ã§alÄ±ÅŸmasÄ± ClickEffectManager'Ä±n iÃ§ implementasyonuna baÄŸlÄ±dÄ±r.
            if (ClickEffectManager.Instance != null)
            {
                // Ekran koordinatlarÄ±nÄ± (pixel) gÃ¶nderiyoruz.
                ClickEffectManager.Instance.PlayEffect(screenPosition);
                SoundManager.instance.PlaySfx(SoundType.btnClick);
            }
            // --- TÄ±klama Efekti Bitti ---


            // --- One-time tap to start mechanic ---
            if (!initialTapDone)
            {
                // Check for any tap or click.
                if (MetroManager.Instance != null)
                {
                    // On the first tap, reduce speed from 4x to 1x.
                    MetroManager.Instance.SetSpeedMultiplier(1.0f);
                }
                initialTapDone = true;
                // Absorb the first tap; don't process it for passenger selection.
                return; 
            }
            // --- End of one-time tap mechanic ---


            // Regular input processing starts after the first tap.
            
            // UI element'lara dokunulup dokunulmadÄ±ÄŸÄ±nÄ± kontrol et. 
            // UI'ya dokunulduysa oyun dÃ¼nyasÄ±ndaki objelere tÄ±klamayÄ± engelle (istenirse).
            if (EventSystem.current.IsPointerOverGameObject())
            {
                // UI objesi Ã¼zerine tÄ±klandÄ±/dokunuldu. Raycast iÅŸlemini atla.
                return;
            }

            // Tutorial aktifse ve input bloklanmÄ±ÅŸsa, normal input iÅŸlemlerini engelle
            if (TutorialManager.Instance != null && TutorialManager.Instance.IsInputBlocked())
            {
                // Tutorial kendi event'lerini yÃ¶netecek, burada normal input'u engelle
                Debug.Log("[InputManager] Input blocked by tutorial.");
                // Ancak raycast'i yine de yap ki tutorial event'i tetiklenebilsin
            }

            // Yolcu Grubu Tespiti iÃ§in Raycast
            Ray ray = Camera.main.ScreenPointToRay(screenPosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                PassengerGroup tappedGroup = hit.transform.GetComponent<PassengerGroup>();
                if (tappedGroup != null)
                {
                    Debug.Log($"[InputManager] Tapped on {tappedGroup.name}");
                    OnPassengerGroupTapped?.Invoke(tappedGroup);
                }
            }

        }
    }


    /// <summary>
    /// Hem mobil dokunmayÄ± hem de fare tÄ±klamasÄ±nÄ± tek bir metodla kontrol eder.
    /// </summary>
    /// <param name="screenPosition">Dokunma/TÄ±klama'nÄ±n ekran pozisyonu (pixel).</param>
    /// <returns>TÄ±klama/Dokunma olayÄ± gerÃ§ekleÅŸtiyse true dÃ¶ner.</returns>
    private bool GetInputDown(out Vector3 screenPosition)
    {
        screenPosition = Vector3.zero;

        // Mobil Dokunma KontrolÃ¼
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                screenPosition = touch.position;
                return true;
            }
        }

        // Editor/Fare TÄ±klamasÄ± KontrolÃ¼ (Mobil cihazlarda Ã§alÄ±ÅŸmaz)
        #if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButtonDown(0))
        {
            screenPosition = Input.mousePosition;
            return true;
        }
        #endif

        return false;
    }


    // Bu metot artÄ±k kullanÄ±lmÄ±yor, GetInputDown ile birleÅŸtirildi.
    // private bool TryGetTouchPosition(out Vector3 position) {...}


    public void DisableInput()
    {
        Debug.Log("[InputManager] Input has been disabled.");
        isInputDisabled = true;
    }

    public void EnableInput()
    {
        Debug.Log("[InputManager] Input has been enabled.");
        isInputDisabled = false;
    }

    private void ToggleSpeed()
    {
        if (MetroManager.Instance != null)
        {
            MetroManager.Instance.ToggleSpeed();
        }
    }
}
