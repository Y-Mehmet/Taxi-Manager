using UnityEngine;
using TMPro; // Eğer TextMeshPro kullanıyorsanız bu satırı ekleyin
using System.Collections;

public class FBSCounter : MonoBehaviour
{
    // FPS değerini göstereceğimiz UI metin elemanı
    // Inspector'dan atayabilirsiniz.
    public TextMeshProUGUI fpsText; // TextMeshPro için
    // public Text fpsText; // Unity'nin kendi UI Text bileşeni için

    public float hudRefreshRate = 1f; // FPS sayacını kaç saniyede bir güncelleyeceğimiz

    private float _accumulatedTime = 0; // Geçen zamanı biriktirir
    private int _frames = 0; // Bu sürede render edilen kare sayısı
    private float _timeUntilUpdate = 0; // Bir sonraki güncellemeye kalan süre

    void Start()
    {
        // Eğer fpsText atanmamışsa hata veririz.
        if (fpsText == null)
        {
            Debug.LogError("FPS Text bileşeni atanmadı! Lütfen Inspector'dan atayın.");
            enabled = false; // Betiği devre dışı bırak
            return;
        }

        _timeUntilUpdate = hudRefreshRate; // İlk güncelleme zamanını ayarla
    }

    void Update()
    {
        // Geçen zamanı ve kare sayısını biriktir
        _accumulatedTime += Time.deltaTime;
        _frames++;
        _timeUntilUpdate -= Time.deltaTime;

        // Belirlenen güncelleme süresi dolduğunda
        if (_timeUntilUpdate <= 0)
        {
            // FPS'i hesapla (kare sayısı / geçen süre)
            float fps = _frames / _accumulatedTime;

            // Hesaplanan FPS değerini UI metnine yaz
            fpsText.text = $"FPS: {Mathf.Round(fps)}";

            // Değişkenleri sıfırla ve bir sonraki güncelleme zamanını ayarla
            _accumulatedTime = 0;
            _frames = 0;
            _timeUntilUpdate = hudRefreshRate;
        }
    }
}
