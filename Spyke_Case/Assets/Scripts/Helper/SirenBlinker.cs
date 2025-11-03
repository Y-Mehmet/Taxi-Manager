using System.Collections;
using UnityEngine;

public class SirenBlinker : MonoBehaviour
{
    // Unity editöründen bu objeleri sürükleyip bırakacaksın
    public GameObject kirmiziLambaObjesi;
    public GameObject maviLambaObjesi;

    // Saniyede kaç kere yanıp söneceği (0.2 = hızlı)
    public float yanipSonmeHizi = 0.2f;

    private Coroutine blinkCoroutine;

    private void OnEnable()
    {
        // Başlangıçta lambaları kapat
        kirmiziLambaObjesi.SetActive(false);
        maviLambaObjesi.SetActive(false);

        // Event'e abone ol
        AbilityManager.OnUniversalPathfindingModeChanged += HandleUniversalPathfindingModeChanged;
    }

    private void OnDisable()
    {
        // Event'ten aboneliği kaldır
        AbilityManager.OnUniversalPathfindingModeChanged -= HandleUniversalPathfindingModeChanged;
        // Objeden çıkarken yanıp sönmeyi durdur
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
            SoundManager.Instance.PlaySfx(SoundType.Siren);
                    }
    }

    IEnumerator BlinkDongusu()
    {
        // Bu döngü durdurulana kadar devam eder
        while (true)
        {
            // Belirlenen süre kadar bekle
            yield return new WaitForSeconds(yanipSonmeHizi);

            // Kırmızıyı kapat, maviyi aç
            kirmiziLambaObjesi.SetActive(false);
            maviLambaObjesi.SetActive(true);

            // Tekrar bekle
            yield return new WaitForSeconds(yanipSonmeHizi);

            // Maviyi kapat, kırmızıyı aç
            kirmiziLambaObjesi.SetActive(true);
            maviLambaObjesi.SetActive(false);
        }
    }
}