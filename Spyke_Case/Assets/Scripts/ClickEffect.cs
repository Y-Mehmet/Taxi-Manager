using UnityEngine;
using System.Collections;

[RequireComponent(typeof(ParticleSystem))]
[RequireComponent(typeof(RectTransform))] // UI elementi olduğu için RectTransform eklenmeli
public class ClickEffect : MonoBehaviour
{
    private ParticleSystem ps;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        Debug.LogWarning($"[{name}] AWAKE. ParticleSystem component is: {(ps == null ? "NULL" : "Assigned")}");
        
        // Loop ayarının kapalı olduğundan emin ol
        var main = ps.main;
        if (main.loop)
        {
            Debug.LogWarning($"[{name}] Loop should be disabled for pooled one-shot effects.");
            main.loop = false;
        }
        
        // ParticleSystem'in "Play On Awake" (POA) ayarını kapatın. 
        if (ps.playOnAwake)
        {
            Debug.LogWarning($"[{name}] Disabling 'Play On Awake'.");
            ps.playOnAwake = false;
        }
    }

    /// <summary>
    /// Efekti oynatır ve bitince havuza geri dönmesi için coroutine başlatır.
    /// </summary>
    public void Play()
    {
        Debug.LogWarning($"[{name}] --- PLAY called ---");
        
        // Önceki coroutine'leri durdur.
        Debug.LogWarning($"[{name}] Stopping all coroutines.");
        StopAllCoroutines(); 
        
        // **KESİN TEMİZLİK:** Havuzdan çıkan efektin sıfırlandığından emin olmak için Play'den önce temizle.
        Debug.LogWarning($"[{name}] Calling StopAndClear() before playing.");
        StopAndClear();
        
        // Particle sistemini sıfırdan oynat
        Debug.LogWarning($"[{name}] Calling ps.Play().");
        ps.Play(); 
        
        Debug.LogWarning($"[{name}] Starting PlayAndReturnToPool coroutine.");
        StartCoroutine(PlayAndReturnToPool());
    }

    /// <summary>
    /// Efekti oynatır ancak coroutine başlatmaz. Sadece havuz başlangıç inisiyalizasyonu için kullanılır.
    /// </summary>
    public void PlayInstant()
    {
        Debug.LogWarning($"[{name}] --- PLAY INSTANT (for pooling) called ---");
        ps.Play();
    }

    private IEnumerator PlayAndReturnToPool()
    {
        Debug.LogWarning($"[{name}] Coroutine: PlayAndReturnToPool started.");
        
        // Particle sisteminin bitmesini bekler.
        // Süre, particle sisteminin şu anki (runtime) ayarlarından dinamik olarak hesaplanır.
        float currentTotalDuration = ps.main.duration + ps.main.startLifetime.constantMax;
        
        Debug.LogWarning($"[{name}] Coroutine: Calculated duration: {currentTotalDuration} seconds.");

        if (currentTotalDuration <= 0)
        {
             // Süre sıfırsa, prefab ayarlarının kontrol edilmesi gerekir.
             Debug.LogError($"[{name}] Coroutine: Particle system duration is zero or negative! Check prefab settings.");
             yield return null; // Bir kare bekle ve havuza dönmeye çalış
        }
        else
        {
            Debug.LogWarning($"[{name}] Coroutine: Waiting for {currentTotalDuration} seconds.");
            yield return new WaitForSeconds(currentTotalDuration);
            Debug.LogWarning($"[{name}] Coroutine: Wait finished.");
        }

        // Efekti havuza iade et
        Debug.LogWarning($"[{name}] Coroutine: Attempting to return to pool.");
        if (ClickEffectManager.Instance != null)
        {
            ClickEffectManager.Instance.ReturnToPool(this);
        }
        else
        {
            // Fallback: Yönetici yoksa kendini yok et
            Debug.LogWarning($"[{name}] Coroutine: ClickEffectManager.Instance is NULL. Destroying self.");
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Particle sistemini anında durdurur ve var olan tüm parçacıkları temizler.
    /// Havuza iade edilmeden önce veya Play() sırasında temiz bir başlangıç için çağrılır.
    /// </summary>
    public void StopAndClear()
    {
        Debug.LogWarning($"[{name}] StopAndClear() called.");
        // StopEmittingAndClear: Yayılan parçacıkları durdurur ve sahnede kalan parçacıkları temizler.
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}

