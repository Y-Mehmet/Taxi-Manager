using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using System;
using DG.Tweening;

/// <summary>
/// Combined manager for level selection - handles both wheel display and level selection logic
/// Singleton pattern for easy access
/// </summary>
public class LevelSelectManager : MonoBehaviour
{
    public static LevelSelectManager Instance { get; private set; }

    // Event fired when selected level changes
    public static event Action<int> OnLevelSelected;

    [Header("UI Element Referansları")]
    public RectTransform WhelePanel; // Bu panele dokunulmayacak
    public RectTransform panel;      // Y Pozisyonu 3400 yapılacak olan panel

    [Header("Level Prefab'ları")]
    public GameObject darkLevelPrefab;
    public GameObject currentLevelPrefab;
    public GameObject lightLevelPrefab;
    public GameObject unlockedLevelPrefab;

    [Header("Level Veri Ayarları")]
    public int totalLevels = 100;
    public int currentLevel = 0; // Currently playing level
    public int maxOpenedLevel = 0; // Highest level ever unlocked
    public float levelItemHeight = 500f;

    private List<GameObject> generatedLevelItems = new List<GameObject>();
    private Coroutine adjustmentCoroutine;
    private int selectedLevelIndex = 0;
    private bool isInitializing = false;

    public int SelectedLevelIndex => selectedLevelIndex;

    void Awake()
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

    void Start()
    {
        StartCoroutine(InitializeOnEnable());
    }

    void OnEnable()
    {
        // Only run on subsequent enables (after first Start)
        // On first enable, generatedLevelItems will be empty - this is normal
        if (Instance != null && generatedLevelItems.Count > 0)
        {
            StartCoroutine(InitializeOnEnable());
        }
    }

    private IEnumerator InitializeOnEnable()
    {
      
        if (isInitializing)
        {
            Debug.LogWarning("[LevelSelectManager] InitializeOnEnable called while already initializing");
            yield break;
        }
        isInitializing = true;

        // Wait for end of frame to ensure ResourceManager and all managers are ready
        yield return new WaitForEndOfFrame();

        // Kayıtlı seviyeyi ResourceManager'dan al
        if (ResourceManager.Instance != null)
        {
            currentLevel = ResourceManager.Instance.CurrentLevel;
            maxOpenedLevel = ResourceManager.Instance.MaxOpenedLevel;
            
            // Always select the max opened level when entering main menu
            selectedLevelIndex = maxOpenedLevel;
            
            Debug.Log($"[LevelSelectManager] CurrentLevel: {currentLevel}, MaxOpenedLevel: {maxOpenedLevel}, Selected: {selectedLevelIndex}");
        }

        GenerateLevelItems();
        FocusOnCurrentLevel();
        
        // Notify listeners (this will trigger wheel rotation and play button update)
        OnLevelSelected?.Invoke(selectedLevelIndex);

        isInitializing = false;
    }

    void GenerateLevelItems()
    {
        foreach (Transform child in WhelePanel)
        {
            Destroy(child.gameObject);
        }
        generatedLevelItems.Clear();

        // IMPORTANT: Use maxOpenedLevel to determine which levels are unlocked
        // This ensures previously unlocked levels stay unlocked
        int displayLimit = Mathf.Min(maxOpenedLevel + 10, totalLevels);
        for (int i = 0; i < displayLimit; i++)
        {
            GameObject levelItemGO = null;

            // i < maxOpenedLevel: Previously completed levels (light)
            // i == maxOpenedLevel: Highest unlocked level (can be current or not)
            // i > maxOpenedLevel: Locked levels (dark)
            
            if (i < maxOpenedLevel)
                levelItemGO = Instantiate(lightLevelPrefab, WhelePanel);
            else if (i == maxOpenedLevel)
                levelItemGO = Instantiate(currentLevelPrefab, WhelePanel);
            else if (i > maxOpenedLevel && i <= maxOpenedLevel + 8)
                levelItemGO = Instantiate(darkLevelPrefab, WhelePanel);
            else if (i == maxOpenedLevel + 9 && i < totalLevels)
                levelItemGO = Instantiate(unlockedLevelPrefab, WhelePanel);
            else
                levelItemGO = Instantiate(darkLevelPrefab, WhelePanel);

            generatedLevelItems.Add(levelItemGO);
            TMP_Text levelText = levelItemGO.GetComponentInChildren<TMP_Text>();

            // 2. Eğer TextMeshPro bileşeni bulunduysa işlemleri yap.
            if (levelText != null)
            {
                // 3. Bu level, "unlocked" prefabı mı diye kontrol et.
                // Bu koşul, hangi prefab'ın "unlocked" olduğunu belirleyen koşul ile aynı.
                if (i == maxOpenedLevel + 9 && i < totalLevels)
                {
                    // Evet, bu en sondaki özel level. Metnini "Unlocked" yap.
                    levelText.text = "Unlocked";
                }
                else
                {
                    // Hayır, bu normal bir level. Metnini (index + 1) olarak ayarla.
                    levelText.text = (i + 1).ToString();
                }
            }

            Text levelNumberText = levelItemGO.GetComponentInChildren<Text>();
            if (levelNumberText != null)
                levelNumberText.text = (i + 1).ToString();

            Button levelButton = levelItemGO.GetComponent<Button>();
            if (levelButton != null)
            {
                int levelIndex = i;
                
                // IMPORTANT: Use maxOpenedLevel to determine if button is interactable
                // All levels up to maxOpenedLevel are unlocked and playable
                levelButton.interactable = (i <= maxOpenedLevel || i == maxOpenedLevel + 9);
            }
        }
    }

    public void FocusOnCurrentLevel()
    {
        if (adjustmentCoroutine != null)
            StopCoroutine(adjustmentCoroutine);
        adjustmentCoroutine = StartCoroutine(AdjustPositionCoroutine());
    }

    // --- SADECE 'panel' OBJESİNİN Y POZİSYONUNU DEĞİŞTİREN FONKSİYON ---
    private IEnumerator AdjustPositionCoroutine()
    {
        // UI elemanları oluşturulduktan sonra işlem yapmak için bekle.
        yield return new WaitForEndOfFrame();
        
        // Bir frame daha bekle - Layout Group'ların pozisyonları güncellemesi için
        yield return new WaitForEndOfFrame();

        // ScrollRect component'ini bul
        ScrollRect scrollRect = panel.parent.GetComponent<ScrollRect>();
        
        if (scrollRect != null && selectedLevelIndex < generatedLevelItems.Count && generatedLevelItems[selectedLevelIndex] != null)
        {
            GameObject selectedWheel = generatedLevelItems[selectedLevelIndex];
            RectTransform wheelRect = selectedWheel.GetComponent<RectTransform>();
            
            if (wheelRect != null && scrollRect.content != null)
            {
                // Content'in toplam yüksekliği
                float contentHeight = scrollRect.content.rect.height;
                // Viewport yüksekliği
                float viewportHeight = scrollRect.viewport.rect.height;
                // Scroll edilebilir alan
                float scrollableHeight = contentHeight - viewportHeight;
                
                if (scrollableHeight > 0)
                {
                    // Wheel'in pozisyonu (negatif)
                    float wheelY = wheelRect.anchoredPosition.y;
                    // Wheel'in content'in en üstünden uzaklığı
                    float distanceFromTop = -wheelY;
                    
                    // Bottom padding - wheel'i biraz daha yukarıda göster
                    float bottomPadding = 200f;
                    
                    // Normalized position hesapla
                    float normalizedPos = Mathf.Clamp01((distanceFromTop - viewportHeight + bottomPadding) / scrollableHeight);
                    float targetScrollPos = 1f - normalizedPos;
                    
                    // Eğer seçili level 5 veya daha yukarıdaysa, animasyon yap
                    if (selectedLevelIndex >= 5)
                    {
                        // 4 level geriden başla
                        int startLevelIndex = Mathf.Max(0, selectedLevelIndex - 4);
                        
                        // Başlangıç wheel'ini bul
                        if (startLevelIndex < generatedLevelItems.Count && generatedLevelItems[startLevelIndex] != null)
                        {
                            GameObject startWheel = generatedLevelItems[startLevelIndex];
                            RectTransform startWheelRect = startWheel.GetComponent<RectTransform>();
                            
                            if (startWheelRect != null)
                            {
                                float startWheelY = startWheelRect.anchoredPosition.y;
                                float startDistanceFromTop = -startWheelY;
                                float startNormalizedPos = Mathf.Clamp01((startDistanceFromTop - viewportHeight + bottomPadding) / scrollableHeight);
                                float startScrollPos = 1f - startNormalizedPos;
                                
                                // Başlangıç pozisyonunu ayarla
                                scrollRect.verticalNormalizedPosition = startScrollPos;
                                
                                // 3 saniyede hedef pozisyona animasyon yap
                                scrollRect.DOVerticalNormalizedPos(targetScrollPos, 3f)
                                    .SetEase(Ease.OutCubic)
                                    .SetUpdate(true);
                                
                                Debug.Log($"AYARLAMA (ANİMASYONLU) - Level {startLevelIndex + 1} → Level {selectedLevelIndex + 1} (3 saniye)");
                            }
                        }
                    }
                    else
                    {
                        // Direkt pozisyona git (animasyon yok)
                        scrollRect.verticalNormalizedPosition = targetScrollPos;
                        
                        Debug.Log($"AYARLAMA (DİREKT) - Level {selectedLevelIndex + 1}, ScrollPos: {targetScrollPos}");
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("ScrollRect veya seçili wheel bulunamadı!");
        }
    }

    /// <summary>
    /// Select a level (called by LevelButton)
    /// </summary>
    public void SelectLevel(int levelIndex)
    {
        selectedLevelIndex = levelIndex;
        Debug.Log($"[LevelSelectManager] Level {levelIndex} selected");
        
        // Notify all listeners (PlayButton will update its text, wheels will rotate)
        OnLevelSelected?.Invoke(selectedLevelIndex);
    }

    /// <summary>
    /// Play the currently selected level (called by PlayButton)
    /// </summary>
    public void PlaySelectedLevel()
    {
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.CurrentLevel = selectedLevelIndex;
            Debug.Log($"[LevelSelectManager] Playing level {selectedLevelIndex}");
        }

        if (SceneManager.Instance != null)
        {
            SceneManager.Instance.LoadLevelSceene();
        }
    }
}
