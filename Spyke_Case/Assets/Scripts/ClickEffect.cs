using UnityEngine;
using System.Collections;

[RequireComponent(typeof(ParticleSystem))]
[RequireComponent(typeof(RectTransform))] // UI elementi olduÄŸu iÃ§in RectTransform eklenmeli
public class ClickEffect : MonoBehaviour
{
    private ParticleSystem ps;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
      //  Debug.LogWarning($"[{name}] AWAKE. ParticleSystem component is: {(ps == null ? "NULL" : "Assigned")}");
        
        var main = ps.main; // 'main' modÃ¼lÃ¼nÃ¼ burada alÄ±yoruz

        // Loop ayarÄ±nÄ±n kapalÄ± olduÄŸundan emin ol
        if (main.loop)
        {
            //Debug.LogWarning($"[{name}] Loop should be disabled for pooled one-shot effects.");
            main.loop = false;
        }
        
        // ParticleSystem'in "Play On Awake" (POA) ayarÄ±nÄ± kapatÄ±n. 
        if (main.playOnAwake)
        {
           // Debug.LogWarning($"[{name}] Disabling 'Play On Awake'.");
            main.playOnAwake = false;
        }

        // YENÄ° KONTROL: StopAction (Durdurma Eylemi)
        // Havuzlama (pooling) yaparken, StopAction'Ä±n 'None' olmasÄ± gerekir.
        // EÄŸer 'Disable' veya 'Destroy' ise, script'in kontrolÃ¼yle Ã§akÄ±ÅŸÄ±r.
        if (main.stopAction != ParticleSystemStopAction.None)
        {
//             Debug.LogWarning($"[{name}] PREFAB UYARISI: 'Stop Action' ayarÄ± '{main.stopAction}' olarak ayarlanmÄ±ÅŸ. Havuzlama iÃ§in 'None' olmalÄ±dÄ±r. 'None' olarak ayarlanÄ±yor.");
            main.stopAction = ParticleSystemStopAction.None;
        }

        // YENÄ° KONTROL: Culling Mode (GÃ¶rÃ¼nmezse Duraklatma)
        // EÄŸer Culling Mode 'Automatic' veya 'Pause' ise,
        // UI Canvas'ta SetActive(false) yapÄ±ldÄ±ÄŸÄ±nda simÃ¼lasyonu duraklatabilir ve tekrar baÅŸladÄ±ÄŸÄ±nda gÃ¶rÃ¼nmez olabilir.
        if (main.cullingMode != ParticleSystemCullingMode.AlwaysSimulate)
        {
          //  Debug.LogWarning($"[{name}] PREFAB UYARISI: 'Culling Mode' ayarÄ± '{main.cullingMode}'. 'AlwaysSimulate' olarak ayarlanmasÄ±, havuzlanan UI efektlerinin gÃ¶rÃ¼nmez olma sorununu Ã§Ã¶zebilir. Ayar 'AlwaysSimulate' olarak deÄŸiÅŸtiriliyor.");
            // Culling mode'u koddan zorla (en gÃ¼venli yÃ¶ntem)
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        }
    }

    /// <summary>
    /// Efekti oynatÄ±r ve bitince havuza geri dÃ¶nmesi iÃ§in coroutine baÅŸlatÄ±r.
    /// </summary>
    public void Play()
    {
        
      
        StopAllCoroutines(); 
        
      
      
        StopAndClear();
        
        
      
        ps.Play(); 
        
       
        StartCoroutine(PlayAndReturnToPool());
    }

    /// <summary>
    /// Efekti oynatÄ±r ancak coroutine baÅŸlatmaz. Sadece havuz baÅŸlangÄ±Ã§ inisiyalizasyonu iÃ§in kullanÄ±lÄ±r.
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
        // SÃ¼re, particle sisteminin ÅŸu anki (runtime) ayarlarÄ±ndan dinamik olarak hesaplanÄ±r.
        float currentTotalDuration = ps.main.duration + ps.main.startLifetime.constantMax;
        
        //Debug.LogWarning($"[{name}] Coroutine: Calculated duration: {currentTotalDuration} seconds.");

        if (currentTotalDuration <= 0)
        {
             // SÃ¼re sÄ±fÄ±rsa, prefab ayarlarÄ±nÄ±n kontrol edilmesi gerekir.
             Debug.LogError($"[{name}] Coroutine: Particle system duration is zero or negative! Check prefab settings.");
             yield return null; // Bir kare bekle ve havuza dÃ¶nmeye Ã§alÄ±ÅŸ
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
            // Fallback: YÃ¶netici yoksa kendini yok et
         //   Debug.LogWarning($"[{name}] Coroutine: ClickEffectManager.Instance is NULL. Destroying self.");
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Particle sistemini anÄ±nda durdurur ve var olan tÃ¼m parÃ§acÄ±klarÄ± temizler.
    /// Havuza iade edilmeden Ã¶nce veya Play() sÄ±rasÄ±nda temiz bir baÅŸlangÄ±Ã§ iÃ§in Ã§aÄŸrÄ±lÄ±r.
    /// </summary>
    public void StopAndClear()
    {
      //  Debug.LogWarning($"[{name}] StopAndClear() called.");
        // StopEmittingAndClear: YayÄ±lan parÃ§acÄ±klarÄ± durdurur ve sahnede kalan parÃ§acÄ±klarÄ± temizler.
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}

