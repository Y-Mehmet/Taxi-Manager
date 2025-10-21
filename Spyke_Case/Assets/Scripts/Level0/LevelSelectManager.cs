using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

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
    public int currentLevel = 0;
    public float levelItemHeight = 500f;

    private List<GameObject> generatedLevelItems = new List<GameObject>();
    private Coroutine adjustmentCoroutine;

    void Start()
    {
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

        int displayLimit = Mathf.Min(currentLevel + 10, totalLevels);
        for (int i = 0; i < displayLimit; i++)
        {
            GameObject levelItemGO = null;

            if (i < currentLevel)
                levelItemGO = Instantiate(lightLevelPrefab, WhelePanel);
            else if (i == currentLevel)
                levelItemGO = Instantiate(currentLevelPrefab, WhelePanel);
            else if (i > currentLevel && i <= currentLevel + 8)
                levelItemGO = Instantiate(darkLevelPrefab, WhelePanel);
            else if (i == currentLevel + 9 && i < totalLevels)
                levelItemGO = Instantiate(unlockedLevelPrefab, WhelePanel);
            else
                levelItemGO = Instantiate(darkLevelPrefab, WhelePanel);

            generatedLevelItems.Add(levelItemGO);

            Text levelNumberText = levelItemGO.GetComponentInChildren<Text>();
            if (levelNumberText != null)
                levelNumberText.text = (i + 1).ToString();

            Button levelButton = levelItemGO.GetComponent<Button>();
            if (levelButton != null)
            {
                int levelIndex = i;
                levelButton.onClick.AddListener(() => OnLevelSelected(levelIndex));
                levelButton.interactable = (i <= currentLevel || i == currentLevel + 9);
            }
        }
    }

    void OnLevelSelected(int levelIndex)
    {
        if (levelIndex <= currentLevel || levelIndex == currentLevel + 9)
        {
            if (levelIndex != currentLevel)
            {
                currentLevel = levelIndex;
                GenerateLevelItems();
                FocusOnCurrentLevel();
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
        float finalY = 3900f;
        panel.anchoredPosition = new Vector2(panel.anchoredPosition.x, finalY);
        
        Debug.Log("AYARLAMA TAMAMLANDI - 'panel' objesinin Y pozisyonu doğrudan " + finalY + " olarak ayarlandı.");
    }
}