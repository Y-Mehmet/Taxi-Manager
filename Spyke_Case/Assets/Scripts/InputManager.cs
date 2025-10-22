using UnityEngine;
using System;

/// <summary>
/// Handles all player input, such as touches and clicks, in a centralized place.
/// It detects which objects are tapped and fires events accordingly.
/// </summary>
public class InputManager : MonoBehaviour
{
    // A singleton instance to ensure only one InputManager exists.
    public static InputManager Instance { get; private set; }

    // Event fired when a PassengerGroup is successfully tapped.
    public static event Action<PassengerGroup> OnPassengerGroupTapped;

    // A flag to disable input, for example when the game is over.
    private bool isInputDisabled = false;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        // Subscribe to the game over event to disable input
        UberManager.OnGameOver += DisableInput;
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        if (UberManager.Instance != null)
        {
             UberManager.OnGameOver -= DisableInput;
        }
    }

    void Update()
    {
        // If input is disabled, do nothing.
        if (isInputDisabled) return;

        // Detect screen tap/click
        if (!TryGetTouchPosition(out Vector3 touchPosition))
        {
            return;
        }

        // Cast a ray from the camera to the tap position
        Ray ray = Camera.main.ScreenPointToRay(touchPosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Check if the ray hit a PassengerGroup
            PassengerGroup tappedGroup = hit.transform.GetComponent<PassengerGroup>();
            if (tappedGroup != null)
            {
                // Fire the event, passing the tapped group
                Debug.Log($"[InputManager] Tapped on {tappedGroup.name}");
                OnPassengerGroupTapped?.Invoke(tappedGroup);
            }
        }
    }

    /// <summary>
    /// Checks for touch or mouse input and returns the screen position.
    /// </summary>
    /// <param name="position">The screen position of the input.</param>
    /// <returns>True if there was a new touch or click, false otherwise.</returns>
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
    
    /// <summary>
    /// Public method to disable all input processing.
    /// </summary>
    public void DisableInput()
    {
        Debug.Log("[InputManager] Input has been disabled.");
        isInputDisabled = true;
    }
}
