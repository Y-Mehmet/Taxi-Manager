using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Settings manager for sound, music volume, and push notifications
/// Works with ResourceManager and SaveGameData
/// </summary>
public class SettingManager : MonoBehaviour
{
    public static SettingManager Instance { get; private set; }

    [Header("UI Elements")]
    [SerializeField] private Slider soundFxSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Button pushAlarmToggle;

    public float MusicVolume { get; private set; }
    public float SoundFxVolume { get; private set; }
    public bool IsPushAlarmEnabled { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        // Add listeners
        if (soundFxSlider != null)
        {
            soundFxSlider.onValueChanged.AddListener(OnSoundFxSliderChanged);
        }
        
        if (musicSlider != null)
        {
            musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
        }

        if (pushAlarmToggle != null)
        {
            pushAlarmToggle.onClick.AddListener(OnPushAlarmToggleChanged);
        }

        // Load settings
        LoadSettings();
    }

    private void OnDisable()
    {
        // Remove listeners
        if (soundFxSlider != null)
        {
            soundFxSlider.onValueChanged.RemoveListener(OnSoundFxSliderChanged);
        }
        
        if (musicSlider != null)
        {
            musicSlider.onValueChanged.RemoveListener(OnMusicSliderChanged);
        }

        if (pushAlarmToggle != null)
        {
            pushAlarmToggle.onClick.RemoveListener(OnPushAlarmToggleChanged);
        }
    }

    /// <summary>
    /// Load settings from SaveGameData
    /// </summary>
    private void LoadSettings()
    {
        if (GameDataManager.Instance != null && GameDataManager.Instance.GetSaveData() != null)
        {
            var data = GameDataManager.Instance.GetSaveData();
            
            SoundFxVolume = data.soundFxVolume;
            MusicVolume = data.musicVolume;
            IsPushAlarmEnabled = data.isPushNotificationEnabled;

            // Update ResourceManager
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.soundFxVolume = SoundFxVolume;
                ResourceManager.Instance.musicVolume = MusicVolume;
            }

            // Update UI
            if (soundFxSlider != null)
            {
                soundFxSlider.value = SoundFxVolume;
            }
            
            if (musicSlider != null)
            {
                musicSlider.value = MusicVolume;
            }

            if (pushAlarmToggle != null)
            {
                MoveHandle(pushAlarmToggle.transform, IsPushAlarmEnabled);
            }

            // Apply to SoundManager
            if (SoundManager.instance != null)
            {
                SoundManager.instance.SetSfxVolume(SoundFxVolume);
                SoundManager.instance.SetBgmVolume(MusicVolume);
            }

            /* Debug.Log($"[SettingManager] Settings loaded: SFX={SoundFxVolume}, Music={MusicVolume}, PushAlarm={IsPushAlarmEnabled}"); */
        }
    }

    /// <summary>
    /// Save settings to SaveGameData
    /// </summary>
    private void SaveSettings()
    {
        if (GameDataManager.Instance != null && GameDataManager.Instance.GetSaveData() != null)
        {
            var data = GameDataManager.Instance.GetSaveData();
            
            data.soundFxVolume = SoundFxVolume;
            data.musicVolume = MusicVolume;
            data.isPushNotificationEnabled = IsPushAlarmEnabled;
            
            GameDataManager.Instance.SaveGame();

            // Update ResourceManager
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.soundFxVolume = SoundFxVolume;
                ResourceManager.Instance.musicVolume = MusicVolume;
            }

            /* Debug.Log($"[SettingManager] Settings saved: SFX={SoundFxVolume}, Music={MusicVolume}, PushAlarm={IsPushAlarmEnabled}"); */
        }
    }

    public void OnSoundFxSliderChanged(float value)
    {
        SoundFxVolume = value;

        // Apply to SoundManager
        if (SoundManager.instance != null)
        {
            SoundManager.instance.SetSfxVolume(value);
        }

        SaveSettings();
    }

    public void OnMusicSliderChanged(float value)
    {
        MusicVolume = value;

        // Apply to SoundManager
        if (SoundManager.instance != null)
        {
            SoundManager.instance.SetBgmVolume(value);
        }

        SaveSettings();
    }

    public void OnPushAlarmToggleChanged()
    {
        IsPushAlarmEnabled = !IsPushAlarmEnabled;
        
        if (pushAlarmToggle != null)
        {
            MoveHandle(pushAlarmToggle.transform, IsPushAlarmEnabled);
        }

        // TODO: Enable/disable push notifications in your notification system
        /* Debug.Log($"[SettingManager] Push notifications {(IsPushAlarmEnabled ? "enabled" : "disabled")}"); */

        SaveSettings();
    }

    /// <summary>
    /// Animate toggle handle position and opacity
    /// </summary>
    private void MoveHandle(Transform toggleTransform, bool isOn)
    {
        if (toggleTransform.childCount == 0) return;

        Transform childTransform = toggleTransform.GetChild(0);
        RectTransform childRectTransform = childTransform.GetComponent<RectTransform>();

        if (childRectTransform != null)
        {
            // Move handle
            Vector2 newPosition = childRectTransform.anchoredPosition;
            newPosition.x = isOn ? 50 : -50;
            childRectTransform.anchoredPosition = newPosition;

            // Change opacity
            Image toggleImage = toggleTransform.GetComponent<Image>();
            if (toggleImage != null)
            {
                Color tempColor = toggleImage.color;
                tempColor.a = isOn ? 1f : 0.05f;
                toggleImage.color = tempColor;
            }
        }
    }
}
