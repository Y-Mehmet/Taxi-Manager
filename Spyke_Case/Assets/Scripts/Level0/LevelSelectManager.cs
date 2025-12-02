using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class LevelSelectManager : MonoBehaviour
{
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

    void Start()
    {
        // Kayıtlı seviyeyi ResourceManager'dan al
        if (ResourceManager.Instance != null)
        {
            currentLevel = ResourceManager.Instance.CurrentLevel;
            maxOpenedLevel = ResourceManager.Instance.MaxOpenedLevel;
            Debug.Log($"[LevelSelectManager] CurrentLevel: {currentLevel}, MaxOpenedLevel: {maxOpenedLevel}");
        }

        GenerateLevelItems();
        FocusOnCurrentLevel();
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
            else
            {
                Debug.LogWarning("Level item prefab does not have a TextMeshProUGUI component in its children.");
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

        // İsteğiniz üzerine, 'panel' objesinin Y pozisyonunu doğrudan 3400 yapıyoruz.
        // 'WhelePanel'e dokunulmuyor.
        float finalY = 4075f;
        panel.anchoredPosition = new Vector2(panel.anchoredPosition.x, finalY);
        
        Debug.Log("AYARLAMA TAMAMLANDI - 'panel' objesinin Y pozisyonu doğrudan " + finalY + " olarak ayarlandı.");
    }
}