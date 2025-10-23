using UnityEngine;
using TMPro; // E�er TextMeshPro kullan�yorsan�z bu sat�r� ekleyin
using System.Collections;

public class FBSCounter : MonoBehaviour
{
    // FPS de�erini g�sterece�imiz UI metin eleman�
    // Inspector'dan atayabilirsiniz.
    public TextMeshProUGUI fpsText; // TextMeshPro i�in
    // public Text fpsText; // Unity'nin kendi UI Text bile�eni i�in

    public float hudRefreshRate = 1f; // FPS sayac�n� ka� saniyede bir g�ncelleyece�imiz

    private float _accumulatedTime = 0; // Ge�en zaman� biriktirir
    private int _frames = 0; // Bu s�rede render edilen kare say�s�
    private float _timeUntilUpdate = 0; // Bir sonraki g�ncellemeye kalan s�re

    void Start()
    {
        // E�er fpsText atanmam��sa hata veririz.
        if (fpsText == null)
        {
            Debug.LogError("FPS Text bile�eni atanmad�! L�tfen Inspector'dan atay�n.");
            enabled = false; // Beti�i devre d��� b�rak
            return;
        }

        _timeUntilUpdate = hudRefreshRate; // �lk g�ncelleme zaman�n� ayarla
    }

    void Update()
    {
        // Ge�en zaman� ve kare say�s�n� biriktir
        _accumulatedTime += Time.deltaTime;
        _frames++;
        _timeUntilUpdate -= Time.deltaTime;

        // Belirlenen g�ncelleme s�resi doldu�unda
        if (_timeUntilUpdate <= 0)
        {
            // FPS'i hesapla (kare say�s� / ge�en s�re)
            float fps = _frames / _accumulatedTime;

            // Hesaplanan FPS de�erini UI metnine yaz
            fpsText.text = $"FPS: {Mathf.Round(fps)}";

            // De�i�kenleri s�f�rla ve bir sonraki g�ncelleme zaman�n� ayarla
            _accumulatedTime = 0;
            _frames = 0;
            _timeUntilUpdate = hudRefreshRate;
        }
    }
}