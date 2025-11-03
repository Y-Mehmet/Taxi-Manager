using System.Collections;
using UnityEngine;

public class SirenBlinker : MonoBehaviour
{
    // Unity editöründen bu objeleri sürükleyip bırakacaksın
    public GameObject kirmiziLambaObjesi;
    public GameObject maviLambaObjesi;

    // Saniyede kaç kere yanıp söneceği (0.2 = hızlı)
    public float yanipSonmeHizi = 0.2f;

    // Script başladığında çalışır
    void Start()
    {
        // Başlangıçta kırmızı yansın, mavi sönsün
        kirmiziLambaObjesi.SetActive(true);
        maviLambaObjesi.SetActive(false);

        // Yanıp sönme döngüsünü başlat
        StartCoroutine(BlinkDongusu());
    }

    IEnumerator BlinkDongusu()
    {
        // Bu döngü oyun durana kadar devam eder
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