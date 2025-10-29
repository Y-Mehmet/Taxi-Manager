using UnityEngine;
using System;

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
        if (UberManager.Instance != null)
        {
             UberManager.OnGameOver -= DisableInput;
        }
        UIManager.OnSpeedToggleClicked -= ToggleSpeed;
    }

    void Update()
    {
        if (isInputDisabled) return;

        // --- One-time tap to start mechanic ---
        if (!initialTapDone)
        {
            // Check for any tap or click, but don't process the raycast yet.
            if (Input.GetMouseButtonDown(0))
            {
                if (MetroManager.Instance != null)
                {
                    // On the first tap, reduce speed from 4x to 1x.
                    MetroManager.Instance.SetSpeedMultiplier(1.0f);
                }
                initialTapDone = true;
                // Absorb the first tap; don't process it for passenger selection.
                return; 
            }
        }
        // --- End of one-time tap mechanic ---

        // Regular input processing starts after the first tap.
        if (!TryGetTouchPosition(out Vector3 touchPosition))
        {
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(touchPosition);
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

    private bool TryGetTouchPosition(out Vector3 position)
    {
        position = Vector3.zero;

#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            position = Input.mousePosition;
            return true;
        }
#endif

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            position = Input.GetTouch(0).position;
            return true;
        }

        return false;
    }
    
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
