using UnityEngine;
using TMPro; // Eðer TextMeshPro kullanýyorsanýz bu satýrý ekleyin
using System.Collections;

public class FBSCounter : MonoBehaviour
{
    // FPS deðerini göstereceðimiz UI metin elemaný
    // Inspector'dan atayabilirsiniz.
    public TextMeshProUGUI fpsText; // TextMeshPro için
    // public Text fpsText; // Unity'nin kendi UI Text bileþeni için

    public float hudRefreshRate = 1f; // FPS sayacýný kaç saniyede bir güncelleyeceðimiz

    private float _accumulatedTime = 0; // Geçen zamaný biriktirir
    private int _frames = 0; // Bu sürede render edilen kare sayýsý
    private float _timeUntilUpdate = 0; // Bir sonraki güncellemeye kalan süre

    void Start()
    {
        // Eðer fpsText atanmamýþsa hata veririz.
        if (fpsText == null)
        {
            Debug.LogError("FPS Text bileþeni atanmadý! Lütfen Inspector'dan atayýn.");
            enabled = false; // Betiði devre dýþý býrak
            return;
        }

        _timeUntilUpdate = hudRefreshRate; // Ýlk güncelleme zamanýný ayarla
    }

    void Update()
    {
        // Geçen zamaný ve kare sayýsýný biriktir
        _accumulatedTime += Time.deltaTime;
        _frames++;
        _timeUntilUpdate -= Time.deltaTime;

        // Belirlenen güncelleme süresi dolduðunda
        if (_timeUntilUpdate <= 0)
        {
            // FPS'i hesapla (kare sayýsý / geçen süre)
            float fps = _frames / _accumulatedTime;

            // Hesaplanan FPS deðerini UI metnine yaz
            fpsText.text = $"FPS: {Mathf.Round(fps)}";

            // Deðiþkenleri sýfýrla ve bir sonraki güncelleme zamanýný ayarla
            _accumulatedTime = 0;
            _frames = 0;
            _timeUntilUpdate = hudRefreshRate;
        }
    }
}