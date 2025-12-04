using System.Collections;
using UnityEngine;

public class SirenBlinker : MonoBehaviour
{
    // Unity editÃ¶rÃ¼nden bu objeleri sÃ¼rÃ¼kleyip bÄ±rakacaksÄ±n
    public GameObject kirmiziLambaObjesi;
    public GameObject maviLambaObjesi;

    // Saniyede kaÃ§ kere yanÄ±p sÃ¶neceÄŸi (0.2 = hÄ±zlÄ±)
    public float yanipSonmeHizi = 0.2f;

    private Coroutine blinkCoroutine;

    private void OnEnable()
    {
        // BaÅŸlangÄ±Ã§ta lambalarÄ± kapat
        kirmiziLambaObjesi.SetActive(false);
        maviLambaObjesi.SetActive(false);

        // Event'e abone ol
        AbilityManager.OnUniversalPathfindingModeChanged += HandleUniversalPathfindingModeChanged;
    }

    private void OnDisable()
    {
        // Event'ten aboneliÄŸi kaldÄ±r
        AbilityManager.OnUniversalPathfindingModeChanged -= HandleUniversalPathfindingModeChanged;
        // Objeden Ã§Ä±karken yanÄ±p sÃ¶nmeyi durdur
        StopBlinking();
    }

    private void HandleUniversalPathfindingModeChanged(bool isActive)
    {
        if (isActive)
        {
            StartBlinking();
        }
        else
        {
            StopBlinking();
        }
    }

    public void StartBlinking()
    {
        if (blinkCoroutine == null)
        {
            kirmiziLambaObjesi.SetActive(true);
            maviLambaObjesi.SetActive(false);
            blinkCoroutine = StartCoroutine(BlinkDongusu());
        }
    }

    public void StopBlinking()
    {
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
            kirmiziLambaObjesi.SetActive(false);
            maviLambaObjesi.SetActive(false);
            SoundManager.instance.PlaySfx(SoundType.Siren,4.4f);
                    }
    }

    IEnumerator BlinkDongusu()
    {
        // Bu dÃ¶ngÃ¼ durdurulana kadar devam eder
        while (true)
        {
            // Belirlenen sÃ¼re kadar bekle
            yield return new WaitForSeconds(yanipSonmeHizi);

            // KÄ±rmÄ±zÄ±yÄ± kapat, maviyi aÃ§
            kirmiziLambaObjesi.SetActive(false);
            maviLambaObjesi.SetActive(true);

            // Tekrar bekle
            yield return new WaitForSeconds(yanipSonmeHizi);

            // Maviyi kapat, kÄ±rmÄ±zÄ±yÄ± aÃ§
            kirmiziLambaObjesi.SetActive(true);
            maviLambaObjesi.SetActive(false);
        }
    }
}
