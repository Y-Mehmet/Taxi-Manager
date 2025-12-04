using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;

    [Header("Ses Veritabanï¿½")]
    [SerializeField] private SoundDatabaseSO soundDatabase;

    [Header("Audio Source'lar")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    // Sesleri hï¿½zlï¿½ca bulmak iï¿½in bir sï¿½zlï¿½k (dictionary) kullanï¿½yoruz.
    private Dictionary<SoundType, AudioClip> soundDictionary;

    private void Awake()
    {
        // Singleton Deseni
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Veritabanï¿½ndaki sesleri sï¿½zlï¿½ï¿½e yï¿½kle
        InitializeSounds();
    }
    public bool  CheckPlayingClip(SoundType soundType)
    {
        if (soundDictionary.TryGetValue(soundType, out AudioClip clip))
        {
            if (sfxSource.clip == clip && sfxSource.isPlaying)
            {
                return true;
            }
        }else
        {
            sfxSource.Stop();
            return false;
        }
        return false;
           
    }
    private void Start()
    {

        bgmSource.volume = ResourceManager.Instance.musicVolume;
        sfxSource.volume = ResourceManager.Instance.soundFxVolume;
        PlayBgm(SoundType.Bg);
    }
    private void InitializeSounds()
    {
        soundDictionary = new Dictionary<SoundType, AudioClip>();
        foreach (var sound in soundDatabase.sounds)
        {
            if (!soundDictionary.ContainsKey(sound.soundType))
            {
                soundDictionary.Add(sound.soundType, sound.audioClip);
            }
            else
            {
//                 Debug.LogWarning("SoundManager: '" + sound.soundType + "' iï¿½in zaten bir ses klibi mevcut!");
            }
        }
        foreach (var sound in soundDatabase.correctSounds)
        {
            if (!soundDictionary.ContainsKey(sound.soundType))
            {
                soundDictionary.Add(sound.soundType, sound.audioClip);
            }
            else
            {
//                 Debug.LogWarning("SoundManager: '" + sound.soundType + "' iï¿½in zaten bir ses klibi mevcut!");
            }
        }


    }

    // Arka plan mï¿½ziï¿½ini ï¿½almak iï¿½in
    public void PlayBgm(SoundType soundType)
    {
        if (soundDictionary.TryGetValue(soundType, out AudioClip clip))
        {
            bgmSource.clip = clip;
            bgmSource.loop = true;
            bgmSource.Play();
        }
        else
        {
//             Debug.LogWarning("SoundManager: '" + soundType + "' isimli BGM bulunamadï¿½!");
        }
    }

    // Ses efektlerini ï¿½almak iï¿½in
    public void PlaySfx(SoundType soundType, float startTime = 0f, bool PlayOneShot=false, float playbackSpeed = 1.0f)
    {
        if (soundDictionary.TryGetValue(soundType, out AudioClip clip))
        {
            sfxSource.pitch= Mathf.Max(0.1f, playbackSpeed);
            if (startTime <= 0f && PlayOneShot)
            {
                sfxSource.PlayOneShot(clip);
            }
            // Eï¿½er belirli bir saniyeden baï¿½lamasï¿½ isteniyorsa:
            else
            {
                // Dï¿½KKAT: Bu yï¿½ntem sfxSource'da ï¿½alan mevcut sesi durdurur!
                sfxSource.clip = clip;      // 1. Klibi ata
                sfxSource.time = startTime; // 2. Baï¿½langï¿½ï¿½ saniyesini ayarla
                sfxSource.Play();           // 3. Oynat
            }
        }
        else
        {
//             Debug.LogWarning("SoundManager: '" + soundType + "' isimli SFX bulunamadï¿½!");
        }
    }
    public void PlayBtnClick()
    {
        if (soundDictionary.TryGetValue(SoundType.btnClick, out AudioClip clip))
        {
            sfxSource.PlayOneShot(clip);
        }
        else
        {
//             Debug.LogWarning("SoundManager: btn isimli SFX bulunamadï¿½!");
        }
    }
    public void StopBG()
    {
        bgmSource.Stop();
    }
    public void StopClip(SoundType soundType)
    {
        if (soundDictionary.TryGetValue(soundType, out AudioClip clip))
        {
            if (sfxSource.clip == clip)
                sfxSource.Stop();
        }
    }

    // Ses ayarlarï¿½ iï¿½in fonksiyonlar (opsiyonel, ï¿½nceki ï¿½rnekteki gibi eklenebilir)
    public void SetBgmVolume(float volume)
    {
        bgmSource.volume = volume;
        ResourceManager.Instance.musicVolume = volume;
         ResourceManager.Instance.SaveData(GameDataManager.Instance.GetSaveData());
    }

    public void SetSfxVolume(float volume)
    {
        sfxSource.volume = volume;
        ResourceManager.Instance.soundFxVolume = volume;
          ResourceManager.Instance.SaveData(GameDataManager.Instance.GetSaveData());
        
    }
    // SoundManager.cs iï¿½inde
    // ... Diï¿½er metodlarï¿½n altï¿½nda
    public void PlaySfxSequentially(SoundType firstSound, SoundType secondSound)
    {
        StartCoroutine(PlaySoundsInOrder(firstSound, secondSound));
    }

    private IEnumerator PlaySoundsInOrder(SoundType firstSound, SoundType secondSound)
    {
        // Birinci sesi ï¿½al
        if (soundDictionary.TryGetValue(firstSound, out AudioClip firstClip))
        {
            sfxSource.PlayOneShot(firstClip);
            // Birinci sesin bitmesini bekle
            yield return new WaitForSeconds(firstClip.length/2);
        }
        else
        {
//             Debug.LogWarning("SoundManager: '" + firstSound + "' isimli SFX bulunamadï¿½!");
        }

        // ï¿½kinci sesi ï¿½al
        if (soundDictionary.TryGetValue(secondSound, out AudioClip secondClip))
        {
            sfxSource.PlayOneShot(secondClip);
        }
        else
        {
//             Debug.LogWarning("SoundManager: '" + secondSound + "' isimli SFX bulunamadï¿½!");
        }
    }

}
public enum SoundType
{
    Bg,
    btnClick,
    Crush,
    Corna,
    EarnCoin,
    BuyCoin,
    Slurp,
    Siren,



}
