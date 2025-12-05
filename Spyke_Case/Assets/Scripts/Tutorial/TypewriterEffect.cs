using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Typewriter effect for tutorial description text.
/// Shows text character by character, can be skipped by tapping.
/// </summary>
public class TypewriterEffect : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float charactersPerSecond = 30f; // Typing speed
    [SerializeField] private bool skipOnTap = true; // Allow skipping by tapping screen
    
    private TextMeshProUGUI textComponent;
    private string fullText;
    private Coroutine typewriterCoroutine;
    private bool isTyping = false;
    private bool isComplete = false;
    
    public bool IsTyping => isTyping;
    public bool IsComplete => isComplete;
    
    /// <summary>
    /// Event fired when typing is complete (either finished or skipped)
    /// </summary>
    public event System.Action OnTypingComplete;
    
    private void Awake()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
        if (textComponent == null)
        {
            Debug.LogError("[TypewriterEffect] TextMeshProUGUI component not found!");
        }
    }
    
    private void Update()
    {
        // Check for screen tap to skip typing
        if (skipOnTap && isTyping && !isComplete)
        {
            if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
            {
                SkipToEnd();
            }
        }
    }
    
    /// <summary>
    /// Start typing the text with typewriter effect
    /// </summary>
    public void StartTyping(string text)
    {
        fullText = text;
        isComplete = false;
        
        // Stop any existing coroutine
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
        }
        
        typewriterCoroutine = StartCoroutine(TypeText());
    }
    
    /// <summary>
    /// Skip to the end of the text immediately
    /// </summary>
    public void SkipToEnd()
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }
        
        if (textComponent != null)
        {
            textComponent.text = fullText;
        }
        
        isTyping = false;
        isComplete = true;
        
        Debug.Log("[TypewriterEffect] Skipped to end");
        
        OnTypingComplete?.Invoke();
    }
    
    /// <summary>
    /// Coroutine that types text character by character
    /// </summary>
    private IEnumerator TypeText()
    {
        isTyping = true;
        isComplete = false;
        
        if (textComponent == null)
        {
            Debug.LogError("[TypewriterEffect] TextMeshProUGUI is null!");
            yield break;
        }
        
        textComponent.text = "";
        
        float delay = 1f / charactersPerSecond;
        
        foreach (char c in fullText)
        {
            textComponent.text += c;
            yield return new WaitForSeconds(delay);
        }
        
        isTyping = false;
        isComplete = true;
        
        Debug.Log("[TypewriterEffect] Typing complete");
        
        OnTypingComplete?.Invoke();
    }
    
    /// <summary>
    /// Reset the typewriter
    /// </summary>
    public void Reset()
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }
        
        if (textComponent != null)
        {
            textComponent.text = "";
        }
        
        isTyping = false;
        isComplete = false;
    }
}
