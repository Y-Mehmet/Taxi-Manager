using UnityEngine;

/// <summary>
/// Merkezi ability unlock yönetim sistemi.
/// SOLID Principles:
/// - Single Responsibility: Sadece ability unlock kontrolünden sorumlu
/// - Open/Closed: Yeni ability'ler için extend edilebilir
/// - Dependency Inversion: Interface yerine SaveGameData'ya bağımlı (data-driven)
/// </summary>
public class AbilityUnlockManager : MonoBehaviour
{
    public static AbilityUnlockManager Instance { get; private set; }
    
    [Header("Unlock Icon")]
    [SerializeField] private Sprite lockedIconSprite; // Locked durumunda gösterilecek icon
    
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
    
    /// <summary>
    /// Belirtilen ability'nin unlock olup olmadığını kontrol eder
    /// </summary>
    public bool IsAbilityUnlocked(AbilityType abilityType)
    {
        if (GameDataManager.Instance == null || GameDataManager.Instance.GetSaveData() == null)
        {
            Debug.LogWarning("[AbilityUnlockManager] GameDataManager or SaveData is null!");
            return false;
        }
        
        SaveGameData saveData = GameDataManager.Instance.GetSaveData();
        int currentMaxLevel = saveData.maxOpenedLevel;
        int unlockLevel = GetAbilityUnlockLevel(abilityType);
        
        bool isUnlocked = currentMaxLevel >= unlockLevel;
        
        Debug.Log($"[AbilityUnlockManager] {abilityType} - MaxLevel: {currentMaxLevel}, UnlockLevel: {unlockLevel}, IsUnlocked: {isUnlocked}");
        
        return isUnlocked;
    }
    
    /// <summary>
    /// Belirtilen ability'nin unlock level'ını döndürür
    /// </summary>
    public int GetAbilityUnlockLevel(AbilityType abilityType)
    {
        if (GameDataManager.Instance == null || GameDataManager.Instance.GetSaveData() == null)
        {
            Debug.LogWarning("[AbilityUnlockManager] GameDataManager or SaveData is null!");
            return 999; // Very high level to prevent unlock
        }
        
        SaveGameData saveData = GameDataManager.Instance.GetSaveData();
        
        switch (abilityType)
        {
            case AbilityType.AddNewStop:
                return saveData.abilityAddNewStopUnlockLevel;
            
            case AbilityType.UniversalPathfinding:
                return saveData.abilityUniversalPathfindingUnlockLevel;
            
            case AbilityType.RemoveWagons:
                return saveData.abilityRemoveWagonsUnlockLevel;
            
            case AbilityType.ShuffleWagonColors:
                return saveData.abilityShuffleWagonColorsUnlockLevel;
            
            default:
                Debug.LogWarning($"[AbilityUnlockManager] Unknown ability type: {abilityType}");
                return 0; // Default: unlocked from start
        }
    }
    
    /// <summary>
    /// Locked icon sprite'ını döndürür
    /// </summary>
    public Sprite GetLockedIconSprite()
    {
        return lockedIconSprite;
    }
    
    /// <summary>
    /// Ability'nin unlock olacağı level'ı formatlanmış string olarak döndürür
    /// Örnek: "Level 5" (index 4 için)
    /// </summary>
    public string GetUnlockLevelText(AbilityType abilityType)
    {
        int unlockLevel = GetAbilityUnlockLevel(abilityType);
        // Level index'i 0'dan başlar, ama kullanıcıya 1'den başlayarak gösteririz
        return $"Level {unlockLevel + 1}";
    }
}
