using UnityEngine;

/// <summary>
/// Ability tutorial davranışlarını tanımlayan interface.
/// Her ability tutorial'ı bu interface'i implement eder.
/// </summary>
public interface IAbilityTutorial
{
    /// <summary>
    /// Ability kullanıldığında çağrılır
    /// </summary>
    void OnAbilityUsed();
    
    /// <summary>
    /// Tutorial'ı sıfırlar (başa döner)
    /// </summary>
    void ResetTutorial();
    
    /// <summary>
    /// Tutorial tamamlandı mı?
    /// </summary>
    bool IsCompleted { get; }
    
    /// <summary>
    /// Ability'nin maliyetini döndürür
    /// </summary>
    int GetCost();
    
    /// <summary>
    /// Ability'nin adını döndürür
    /// </summary>
    string GetAbilityName();
    
    /// <summary>
    /// Ability'nin açıklamasını döndürür (İngilizce)
    /// </summary>
    string GetDescription();
}
