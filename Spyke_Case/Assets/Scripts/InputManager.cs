using UnityEngine;
using System;
using UnityEngine.EventSystems; // UI kontrolü için eklendi

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
        // Null kontrolü, oyun sonu/kapanışı senaryolarında önemlidir.
        if (UberManager.Instance != null)
        {
             UberManager.OnGameOver -= DisableInput;
        }
        UIManager.OnSpeedToggleClicked -= ToggleSpeed;
    }

    void Update()
    {
        if (isInputDisabled) return;

        // Tıklama/Dokunma tespitini tek bir yerden yapalım.
        // Hem fare tıklaması hem de mobil dokunma için 'GetInputDown' adında yeni bir metot kullanacağız.

        if (GetInputDown(out Vector3 screenPosition))
        {
            // --- Tıklama Efekti ---
            // Her tıklamada/dokunmada ClickEffectManager'a ekran pozisyonunu göndererek efekti oynat.
            // Bu efektin UI katmanında çalışması ClickEffectManager'ın iç implementasyonuna bağlıdır.
            if (ClickEffectManager.Instance != null)
            {
                // Ekran koordinatlarını (pixel) gönderiyoruz.
                ClickEffectManager.Instance.PlayEffect(screenPosition); 
            }
            // --- Tıklama Efekti Bitti ---


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
            
            // UI element'lara dokunulup dokunulmadığını kontrol et. 
            // UI'ya dokunulduysa oyun dünyasındaki objelere tıklamayı engelle (istenirse).
            if (EventSystem.current.IsPointerOverGameObject())
            {
                // UI objesi üzerine tıklandı/dokunuldu. Raycast işlemini atla.
                return;
            }


            // Yolcu Grubu Tespiti için Raycast
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
    /// Hem mobil dokunmayı hem de fare tıklamasını tek bir metodla kontrol eder.
    /// </summary>
    /// <param name="screenPosition">Dokunma/Tıklama'nın ekran pozisyonu (pixel).</param>
    /// <returns>Tıklama/Dokunma olayı gerçekleştiyse true döner.</returns>
    private bool GetInputDown(out Vector3 screenPosition)
    {
        screenPosition = Vector3.zero;

        // Mobil Dokunma Kontrolü
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                screenPosition = touch.position;
                return true;
            }
        }

        // Editor/Fare Tıklaması Kontrolü (Mobil cihazlarda çalışmaz)
        #if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButtonDown(0))
        {
            screenPosition = Input.mousePosition;
            return true;
        }
        #endif

        return false;
    }


    // Bu metot artık kullanılmıyor, GetInputDown ile birleştirildi.
    // private bool TryGetTouchPosition(out Vector3 position) {...}


    public void DisableInput()
    {
        Debug.Log("[InputManager] Input has been disabled.");
        isInputDisabled = true;
    }

    private void ToggleSpeed()
    {
        if (MetroManager.Instance != null)
        {
            MetroManager.Instance.ToggleSpeed();
        }
    }
}