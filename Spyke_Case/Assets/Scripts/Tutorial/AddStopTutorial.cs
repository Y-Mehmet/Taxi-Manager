using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Add Stop ability tutorial implementation.
/// Stop panellerini otomatik olarak parent'ın child'larından alır.
/// İlk child her zaman aktif, diğerleri buton tıklandıkça aktif olur.
/// </summary>
public class AddStopTutorial : MonoBehaviour, IAbilityTutorial
{
    [Header("Stop Container")]
    [SerializeField] private Transform stopContainer; // Stop panellerinin parent'ı
    
    [Header("Cost Display")]
    [SerializeField] private TextMeshProUGUI costText; // Harcanan parayı gösteren text (ortak)
    
    [Header("Settings")]
    [SerializeField] private AbilityType abilityType = AbilityType.AddNewStop; // Ability tipi
    [SerializeField] private string abilityName = "Add Stop";
    [SerializeField] private string description = "Add a new stop to the map. This allows passengers to board faster and reduces traffic congestion.\n\n💰 Cost increases with each use:\n1st use: 100 Coins\n2nd use: 200 Coins\n3rd use: 400 Coins\n4th use: 800 Coins";
    
    private StopPanelData[] stopPanels; // Otomatik olarak doldurulacak
    private int currentStopIndex = 1; // İlk stop (index 0) zaten aktif, 1'den başlıyoruz
    
    public bool IsCompleted => currentStopIndex >= stopPanels.Length;
    
    private void Start()
    {
        AutoFillStopPanels();
        InitializePanels();
        UpdateCostDisplay();
    }
    
    /// <summary>
    /// Stop container'ın child'larından otomatik olarak stop panellerini doldurur
    /// </summary>
    private void AutoFillStopPanels()
    {
        if (stopContainer == null)
        {
            Debug.LogError("[AddStopTutorial] Stop container not assigned!");
            return;
        }
        
        int childCount = stopContainer.childCount;
        if (childCount == 0)
        {
            Debug.LogError("[AddStopTutorial] Stop container has no children!");
            return;
        }
        
        stopPanels = new StopPanelData[childCount];
        
        for (int i = 0; i < childCount; i++)
        {
            Transform child = stopContainer.GetChild(i);
            stopPanels[i] = new StopPanelData();
            
            // Panel object
            stopPanels[i].panelObject = child.gameObject;
            
            // Child'ların child'larını al (InactiveIndicator, ActiveIndicator, StopIcon, StopText)
            if (child.childCount >= 2)
            {
                // İlk iki child indicator'lar olmalı
                stopPanels[i].inactiveIndicator = child.GetChild(0).gameObject;
                stopPanels[i].activeIndicator = child.GetChild(1).gameObject;
            }
            
            // StopIcon ve StopText'i bul (Image ve TextMeshProUGUI component'lerine göre)
            Image[] images = child.GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                // Indicator olmayan ilk image'i StopIcon olarak al
                if (img.gameObject != stopPanels[i].inactiveIndicator && 
                    img.gameObject != stopPanels[i].activeIndicator)
                {
                    stopPanels[i].stopImage = img;
                    break;
                }
            }
            
            // TextMeshProUGUI'yi bul
            TextMeshProUGUI[] texts = child.GetComponentsInChildren<TextMeshProUGUI>(true);
            if (texts.Length > 0)
            {
                stopPanels[i].stopText = texts[0];
            }
        }
        
        /* Debug.Log($"[AddStopTutorial] Auto-filled {stopPanels.Length} stop panels from children"); */
    }
    
    /// <summary>
    /// Panelleri başlangıç durumuna getirir
    /// </summary>
    private void InitializePanels()
    {
        if (stopPanels == null || stopPanels.Length == 0)
        {
            Debug.LogError("[AddStopTutorial] No stop panels found!");
            return;
        }
        
        for (int i = 0; i < stopPanels.Length; i++)
        {
            if (i == 0)
            {
                // İlk panel her zaman aktif ve açık
                stopPanels[i].SetActive(true);
                stopPanels[i].SetStopActive(true);
                stopPanels[i].SetText($"Stop {i + 1}");
            }
            else
            {
                // Diğer paneller başlangıçta kapalı
                stopPanels[i].SetActive(true);
                stopPanels[i].SetStopActive(false);
                stopPanels[i].SetText($"Stop {i + 1}");
            }
        }
        
        /* Debug.Log($"[AddStopTutorial] Initialized {stopPanels.Length} stop panels"); */
    }
    
    /// <summary>
    /// Ability kullanıldığında çağrılır (buton tıklandığında)
    /// </summary>
    public void OnAbilityUsed()
    {
        if (IsCompleted)
        {
            /* Debug.Log("[AddStopTutorial] Tutorial already completed!"); */
            return;
        }
        
        // Mevcut stop'u aktif et
        ActivateStop(currentStopIndex);
        
        // Sonraki stop'a geç
        currentStopIndex++;
        
        // UI'ı güncelle (BİR SONRAKİ kullanım maliyetini göster)
        // currentStopIndex zaten artırıldı, bu yüzden bir sonraki kullanım maliyetini gösterir
        UpdateCostDisplay();
        
        /* Debug.Log($"[AddStopTutorial] Activated stop {currentStopIndex}, Next cost: {GetCost()}"); */
        
        if (IsCompleted)
        {
            /* Debug.Log("[AddStopTutorial] Tutorial completed!"); */
        }
    }
    
    /// <summary>
    /// Belirtilen index'teki stop'u aktif eder
    /// </summary>
    private void ActivateStop(int index)
    {
        if (index < 0 || index >= stopPanels.Length)
        {
            /* Debug.LogWarning($"[AddStopTutorial] Invalid stop index: {index}"); */
            return;
        }
        
        StopPanelData panel = stopPanels[index];
        
        // İnaktif göstergeyi kapat, aktif göstergeyi aç
        panel.SetStopActive(true);
        
        /* Debug.Log($"[AddStopTutorial] Stop {index + 1} activated"); */
    }
    
    /// <summary>
    /// Maliyet metnini günceller (bir sonraki kullanım maliyetini gösterir)
    /// </summary>
    private void UpdateCostDisplay()
    {
        if (costText != null)
        {
            if (IsCompleted)
            {
                costText.text = "Completed!";
            }
            else
            {
                int nextCost = GetCost();
                costText.text = $"{nextCost} Coin";
            }
        }
    }
    
    /// <summary>
    /// Tutorial'ı sıfırlar
    /// </summary>
    public void ResetTutorial()
    {
        currentStopIndex = 1;
        InitializePanels();
        UpdateCostDisplay();
        
        /* Debug.Log("[AddStopTutorial] Tutorial reset"); */
    }
    
    /// <summary>
    /// Ability'nin maliyetini döndürür (AbilityUsageTracker'dan)
    /// </summary>
    public int GetCost()
    {
        if (AbilityUsageTracker.Instance != null)
        {
            // currentStopIndex - 1 çünkü:
            // currentStopIndex = 1 → 0 kullanım → 100 * 2^0 = 100
            // currentStopIndex = 2 → 1 kullanım → 100 * 2^1 = 200
            // currentStopIndex = 3 → 2 kullanım → 100 * 2^2 = 400
            // currentStopIndex = 4 → 3 kullanım → 100 * 2^3 = 800
            int usageCount = currentStopIndex - 1;
            int baseCost = 100; // AbilityUsageTracker.BASE_COST
            int cost = baseCost * (int)Mathf.Pow(2, usageCount);
            
            /* Debug.Log($"[AddStopTutorial] GetCost: currentStopIndex={currentStopIndex}, usageCount={usageCount}, cost={cost}"); */
            
            return cost;
        }
        
        // Fallback
        return 100;
    }
    
    /// <summary>
    /// Ability'nin adını döndürür
    /// </summary>
    public string GetAbilityName()
    {
        return abilityName;
    }
    
    /// <summary>
    /// Ability'nin açıklamasını döndürür
    /// </summary>
    public string GetDescription()
    {
        return description;
    }
}
