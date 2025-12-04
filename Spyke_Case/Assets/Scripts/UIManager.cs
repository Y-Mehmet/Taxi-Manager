using UnityEngine;
using System;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    public static event Action OnSpeedToggleClicked;

    [Header("Floating Text Settings")]
    [SerializeField] private Canvas mainCanvas;
    [SerializeField] private GameObject floatingTextPrefab; // Prefab reference
    [SerializeField] private float floatDuration = 1.5f;
    [SerializeField] private float floatDistance = 100f;
    [SerializeField] private float startScale = 1f;
    [SerializeField] private int initialPoolSize = 10;

    private Queue<GameObject> textPool = new Queue<GameObject>();

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

        if (mainCanvas == null)
        {
            mainCanvas = FindObjectOfType<Canvas>();
        }
    }

    private void Start()
    {
        InitializePool();
    }

    private void InitializePool()
    {
        if (mainCanvas == null || floatingTextPrefab == null) return;

        for (int i = 0; i < initialPoolSize; i++)
        {
            GameObject obj = CreateTextObject();
            if (obj != null)
            {
                obj.SetActive(false);
                textPool.Enqueue(obj);
            }
        }
    }

    private GameObject CreateTextObject()
    {
        if (floatingTextPrefab == null)
        {
            Debug.LogError("Floating Text Prefab is not assigned in UIManager!");
            return null;
        }

        GameObject textObj = Instantiate(floatingTextPrefab, mainCanvas.transform);
        return textObj;
    }

    public void SpeedToggleClicked()
    {
        OnSpeedToggleClicked?.Invoke();
    }

    public void ShowFloatingText(string message, Vector3 worldPos)
    {
        if (mainCanvas == null || floatingTextPrefab == null) return;

        GameObject textObj = GetTextFromPool();
        if (textObj == null) return;
        
        // Reset State
        textObj.SetActive(true);
        textObj.transform.SetAsLastSibling(); // Ensure it's on top

        RectTransform rectTransform = textObj.GetComponent<RectTransform>();
        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();

        if (tmp != null)
        {
            tmp.text = message;
            // Font properties (size, color, font asset) are now controlled by the Prefab itself.
            // We reset alpha to 1 just in case the prefab was saved with 0 alpha or previous fade out affected it.
            Color c = tmp.color;
            c.a = 1f;
            tmp.color = c;
        }
        
        // Position
        Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            mainCanvas.transform as RectTransform, 
            screenPos, 
            mainCanvas.worldCamera, 
            out localPos);
        
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = localPos;
            rectTransform.localScale = Vector3.one * startScale;

            // Animation
            Sequence seq = DOTween.Sequence();
            
            // Move Up
            seq.Join(rectTransform.DOAnchorPosY(rectTransform.anchoredPosition.y + floatDistance, floatDuration).SetEase(Ease.OutQuad));
            
            // Fade Out
            if (tmp != null)
            {
                seq.Join(tmp.DOFade(0f, floatDuration).SetEase(Ease.InQuad));
            }

            // Return to pool
            seq.OnComplete(() => ReturnToPool(textObj));
        }
        else
        {
            // If no RectTransform, just return to pool immediately (safety check)
            ReturnToPool(textObj);
        }
    }

    private GameObject GetTextFromPool()
    {
        if (textPool.Count > 0)
        {
            return textPool.Dequeue();
        }
        else
        {
            return CreateTextObject();
        }
    }

    private void ReturnToPool(GameObject obj)
    {
        if (obj != null)
        {
            obj.SetActive(false);
            textPool.Enqueue(obj);
        }
    }
}
