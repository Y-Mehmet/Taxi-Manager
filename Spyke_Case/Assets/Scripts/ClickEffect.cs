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
      //  Debug.LogWarning($"[{name}] AWAKE. ParticleSystem component is: {(ps == null ? "NULL" : "Assigned")}");
        
        var main = ps.main; // 'main' modülünü burada alıyoruz

        // Loop ayarının kapalı olduğundan emin ol
        if (main.loop)
        {
            //Debug.LogWarning($"[{name}] Loop should be disabled for pooled one-shot effects.");
            main.loop = false;
        }
        
        // ParticleSystem'in "Play On Awake" (POA) ayarını kapatın. 
        if (main.playOnAwake)
        {
           // Debug.LogWarning($"[{name}] Disabling 'Play On Awake'.");
            main.playOnAwake = false;
        }

        // YENİ KONTROL: StopAction (Durdurma Eylemi)
        // Havuzlama (pooling) yaparken, StopAction'ın 'None' olması gerekir.
        // Eğer 'Disable' veya 'Destroy' ise, script'in kontrolüyle çakışır.
        if (main.stopAction != ParticleSystemStopAction.None)
        {
            Debug.LogWarning($"[{name}] PREFAB UYARISI: 'Stop Action' ayarı '{main.stopAction}' olarak ayarlanmış. Havuzlama için 'None' olmalıdır. 'None' olarak ayarlanıyor.");
            main.stopAction = ParticleSystemStopAction.None;
        }

        // YENİ KONTROL: Culling Mode (Görünmezse Duraklatma)
        // Eğer Culling Mode 'Automatic' veya 'Pause' ise,
        // UI Canvas'ta SetActive(false) yapıldığında simülasyonu duraklatabilir ve tekrar başladığında görünmez olabilir.
        if (main.cullingMode != ParticleSystemCullingMode.AlwaysSimulate)
        {
          //  Debug.LogWarning($"[{name}] PREFAB UYARISI: 'Culling Mode' ayarı '{main.cullingMode}'. 'AlwaysSimulate' olarak ayarlanması, havuzlanan UI efektlerinin görünmez olma sorununu çözebilir. Ayar 'AlwaysSimulate' olarak değiştiriliyor.");
            // Culling mode'u koddan zorla (en güvenli yöntem)
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        }
    }

    /// <summary>
    /// Efekti oynatır ve bitince havuza geri dönmesi için coroutine başlatır.
    /// </summary>
    public void Play()
    {
        
      
        StopAllCoroutines(); 
        
      
      
        StopAndClear();
        
        
      
        ps.Play(); 
        
       
        StartCoroutine(PlayAndReturnToPool());
    }

    /// <summary>
    /// Efekti oynatır ancak coroutine başlatmaz. Sadece havuz başlangıç inisiyalizasyonu için kullanılır.
    /// </summary>
    public void PlayInstant()
    {
       // Debug.LogWarning($"[{name}] --- PLAY INSTANT (for pooling) called ---");
        ps.Play();
    }

    private IEnumerator PlayAndReturnToPool()
    {
      //  Debug.LogWarning($"[{name}] Coroutine: PlayAndReturnToPool started.");
        
        // Particle sisteminin bitmesini bekler.
        // Süre, particle sisteminin şu anki (runtime) ayarlarından dinamik olarak hesaplanır.
        float currentTotalDuration = ps.main.duration + ps.main.startLifetime.constantMax;
        
        //Debug.LogWarning($"[{name}] Coroutine: Calculated duration: {currentTotalDuration} seconds.");

        if (currentTotalDuration <= 0)
        {
             // Süre sıfırsa, prefab ayarlarının kontrol edilmesi gerekir.
             Debug.LogError($"[{name}] Coroutine: Particle system duration is zero or negative! Check prefab settings.");
             yield return null; // Bir kare bekle ve havuza dönmeye çalış
        }
        else
        {
        //    Debug.LogWarning($"[{name}] Coroutine: Waiting for {currentTotalDuration} seconds.");
            yield return new WaitForSeconds(currentTotalDuration);
          //  Debug.LogWarning($"[{name}] Coroutine: Wait finished.");
        }

        // Efekti havuza iade et
      //  Debug.LogWarning($"[{name}] Coroutine: Attempting to return to pool.");
        if (ClickEffectManager.Instance != null)
        {
            ClickEffectManager.Instance.ReturnToPool(this);
        }
        else
        {
            // Fallback: Yönetici yoksa kendini yok et
         //   Debug.LogWarning($"[{name}] Coroutine: ClickEffectManager.Instance is NULL. Destroying self.");
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Particle sistemini anında durdurur ve var olan tüm parçacıkları temizler.
    /// Havuza iade edilmeden önce veya Play() sırasında temiz bir başlangıç için çağrılır.
    /// </summary>
    public void StopAndClear()
    {
      //  Debug.LogWarning($"[{name}] StopAndClear() called.");
        // StopEmittingAndClear: Yayılan parçacıkları durdurur ve sahnede kalan parçacıkları temizler.
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}

