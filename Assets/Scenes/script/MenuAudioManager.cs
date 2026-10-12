using UnityEngine;
using UnityEngine.UI;
using FMODUnity;
using FMOD.Studio;

public class MenuAudioManager : MonoBehaviour
{
    public static MenuAudioManager Instance { get; private set; }

    [Header("Default Settings")]
    [Range(0.0001f, 1f)] public float defaultBgmVolume = 0.25f;
    [Range(0.0001f, 1f)] public float defaultSfxVolume = 1.0f;

    [Header("UI References")]
    public Slider bgmSlider;
    public Slider sfxSlider;
    public Toggle muteBgmToggle;
    public Toggle muteSfxToggle;

    private Bus bgmBus;
    private Bus sfxBus;

    private float currentBgmVolume;
    private float lastBgmVolumeBeforeMute;
    private float currentSfxVolume;
    private float lastSfxVolumeBeforeMute;

    private bool isBgmMuted;
    private bool isSfxMuted;
    private bool isPlayerDead;

    private PlayerHealth playerHealth;

    private const string BGM_VOL_KEY = "Setting_BGM_Volume";
    private const string SFX_VOL_KEY = "Setting_SFX_Volume";
    private const string LAST_BGM_VOL_KEY = "Setting_Last_BGM_Volume";
    private const string LAST_SFX_VOL_KEY = "Setting_Last_SFX_Volume";
    private const string BGM_MUTE_KEY = "Setting_BGM_Mute";
    private const string SFX_MUTE_KEY = "Setting_SFX_Mute";

    private const float MIN_VOL = 0.0001f;

    private void Awake()
    {
        Instance = this;
        LoadSavedSettings();
    }

    private void OnEnable()
    {
        // Khi Menu bật lên: Luôn luôn kết nối lại FMOD và đồng bộ UI
        ConnectFMODAndSync();
        HookPlayerHealth();
    }

    private void Start()
    {
        ConnectFMODAndSync();
    }

    /// <summary>
    /// KẾT NỐI TRỰC TIẾP VỚI FMOD MỖI LẦN MỞ MENU
    /// Không cache cố định biến Bus, luôn lấy Bus mới nhất từ FMOD RuntimeManager
    /// </summary>
    public void ConnectFMODAndSync()
    {
        // 1. Luôn kết nối lại FMOD Bus mới nhất (kể cả sau khi restart Play Mode)
        ReconnectFMODBuses();

        // 2. Gắn sự kiện cho các Slider/Toggle
        SetupUIListeners();

        // 3. Đưa Slider/Toggle về đúng vị trí đã lưu
        SyncUIWithSettings();

        // 4. Bơm ngay âm lượng và trạng thái mute vào FMOD và Unity
        ApplyAllAudioStates();
    }

    public void RefreshUIAndAudio()
    {
        ConnectFMODAndSync();
    }

    private void ReconnectFMODBuses()
    {
        try
        {
            bgmBus = RuntimeManager.GetBus("bus:/BGM");
            sfxBus = RuntimeManager.GetBus("bus:/SFX");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[MenuAudioManager] Không kết nối được Bus: {e.Message}");
        }
    }

    private void LoadSavedSettings()
    {
        currentBgmVolume = Mathf.Clamp(PlayerPrefs.GetFloat(BGM_VOL_KEY, defaultBgmVolume), MIN_VOL, 1f);
        lastBgmVolumeBeforeMute = Mathf.Clamp(PlayerPrefs.GetFloat(LAST_BGM_VOL_KEY, defaultBgmVolume), MIN_VOL, 1f);

        currentSfxVolume = Mathf.Clamp(PlayerPrefs.GetFloat(SFX_VOL_KEY, defaultSfxVolume), MIN_VOL, 1f);
        lastSfxVolumeBeforeMute = Mathf.Clamp(PlayerPrefs.GetFloat(LAST_SFX_VOL_KEY, defaultSfxVolume), MIN_VOL, 1f);

        isBgmMuted = PlayerPrefs.GetInt(BGM_MUTE_KEY, 0) == 1;
        isSfxMuted = PlayerPrefs.GetInt(SFX_MUTE_KEY, 0) == 1;
    }

    private void SetupUIListeners()
    {
        if (bgmSlider != null)
        {
            bgmSlider.onValueChanged.RemoveListener(SetBGMVolume);
            bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        }
        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveListener(SetSFXVolume);
            sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        }
        if (muteBgmToggle != null)
        {
            muteBgmToggle.onValueChanged.RemoveListener(ToggleMuteBGM);
            muteBgmToggle.onValueChanged.AddListener(ToggleMuteBGM);
        }
        if (muteSfxToggle != null)
        {
            muteSfxToggle.onValueChanged.RemoveListener(ToggleMuteSFX);
            muteSfxToggle.onValueChanged.AddListener(ToggleMuteSFX);
        }
    }

    private void SyncUIWithSettings()
    {
        if (bgmSlider != null)
            bgmSlider.SetValueWithoutNotify(isBgmMuted ? 0f : currentBgmVolume);

        if (sfxSlider != null)
            sfxSlider.SetValueWithoutNotify(isSfxMuted ? 0f : currentSfxVolume);

        if (muteBgmToggle != null)
            muteBgmToggle.SetIsOnWithoutNotify(isBgmMuted);

        if (muteSfxToggle != null)
            muteSfxToggle.SetIsOnWithoutNotify(isSfxMuted);
    }

    private void HookPlayerHealth()
    {
        if (playerHealth != null) return;
        playerHealth = Object.FindAnyObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.OnDeathEvent -= OnPlayerDied;
            playerHealth.OnDeathEvent += OnPlayerDied;
            playerHealth.OnRespawnEvent -= OnPlayerRespawned;
            playerHealth.OnRespawnEvent += OnPlayerRespawned;
        }
    }

    // --- BGM LOGIC ---
    public void SetBGMVolume(float volume)
    {
        if (volume <= MIN_VOL)
        {
            isBgmMuted = true;
            currentBgmVolume = MIN_VOL;
        }
        else
        {
            isBgmMuted = false;
            currentBgmVolume = volume;
            lastBgmVolumeBeforeMute = volume;
        }

        SaveBGMSettings();
        SyncUIWithSettings();
        ApplyBGMVolume();
    }

    public void ToggleMuteBGM(bool isMuted)
    {
        isBgmMuted = isMuted;

        if (isMuted)
        {
            if (currentBgmVolume > MIN_VOL) lastBgmVolumeBeforeMute = currentBgmVolume;
            currentBgmVolume = MIN_VOL;
        }
        else
        {
            currentBgmVolume = (lastBgmVolumeBeforeMute > MIN_VOL) ? lastBgmVolumeBeforeMute : defaultBgmVolume;
        }

        SaveBGMSettings();
        SyncUIWithSettings();
        ApplyBGMVolume();
    }

    private void SaveBGMSettings()
    {
        PlayerPrefs.SetFloat(BGM_VOL_KEY, currentBgmVolume);
        PlayerPrefs.SetFloat(LAST_BGM_VOL_KEY, lastBgmVolumeBeforeMute);
        PlayerPrefs.SetInt(BGM_MUTE_KEY, isBgmMuted ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void ApplyBGMVolume()
    {
        bool shouldMute = isBgmMuted || isPlayerDead;
        float finalVol = shouldMute ? MIN_VOL : currentBgmVolume;

        // 1. Luôn kết nối lại Bus nếu mất hiệu lực
        if (!bgmBus.isValid())
        {
            ReconnectFMODBuses();
        }

        // 2. Gửi lệnh vào FMOD Bus
        if (bgmBus.isValid())
        {
            bgmBus.setMute(shouldMute);
            bgmBus.setVolume(finalVol);
        }

        // 3. Đồng bộ với BGM của LightingManager (Day/Night BGM)
        //LightingManager lm = Object.FindAnyObjectByType<LightingManager>();
        //if (lm != null)
        //{
        //    lm.SetBGMPaused(shouldMute);
        //    lm.SetBGMVolume(finalVol);
        //}

    }

    // --- SFX LOGIC ---
    public void SetSFXVolume(float volume)
    {
        if (volume <= MIN_VOL)
        {
            isSfxMuted = true;
            currentSfxVolume = MIN_VOL;
        }
        else
        {
            isSfxMuted = false;
            currentSfxVolume = volume;
            lastSfxVolumeBeforeMute = volume;
        }

        SaveSFXSettings();
        SyncUIWithSettings();
        ApplySFXVolume();
    }

    public void ToggleMuteSFX(bool isMuted)
    {
        isSfxMuted = isMuted;

        if (isMuted)
        {
            if (currentSfxVolume > MIN_VOL) lastSfxVolumeBeforeMute = currentSfxVolume;
            currentSfxVolume = MIN_VOL;
        }
        else
        {
            currentSfxVolume = (lastSfxVolumeBeforeMute > MIN_VOL) ? lastSfxVolumeBeforeMute : defaultSfxVolume;
        }

        SaveSFXSettings();
        SyncUIWithSettings();
        ApplySFXVolume();
    }

    private void SaveSFXSettings()
    {
        PlayerPrefs.SetFloat(SFX_VOL_KEY, currentSfxVolume);
        PlayerPrefs.SetFloat(LAST_SFX_VOL_KEY, lastSfxVolumeBeforeMute);
        PlayerPrefs.SetInt(SFX_MUTE_KEY, isSfxMuted ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void ApplySFXVolume()
    {
        float finalVol = isSfxMuted ? 0f : currentSfxVolume;

        // 1. Luôn kết nối lại Bus nếu mất hiệu lực
        if (!sfxBus.isValid())
        {
            ReconnectFMODBuses();
        }

        // 2. Gửi lệnh vào FMOD Bus
        if (sfxBus.isValid())
        {
            sfxBus.setMute(isSfxMuted);
            sfxBus.setVolume(isSfxMuted ? MIN_VOL : currentSfxVolume);
        }

        // 3. Unity AudioListener volume
        AudioListener.volume = finalVol;
    }

    private void ApplyAllAudioStates()
    {
        ApplyBGMVolume();
        ApplySFXVolume();
    }

    public void OnPlayerDied() 
    { 
        isPlayerDead = true; 
        ApplyBGMVolume(); 
    }

    public void OnPlayerRespawned() 
    { 
        isPlayerDead = false; 
        ApplyBGMVolume(); 
    }

    [ContextMenu("Reset All Audio Settings To Default")]
    public void ResetToDefault()
    {
        PlayerPrefs.DeleteKey(BGM_VOL_KEY);
        PlayerPrefs.DeleteKey(SFX_VOL_KEY);
        PlayerPrefs.DeleteKey(LAST_BGM_VOL_KEY);
        PlayerPrefs.DeleteKey(LAST_SFX_VOL_KEY);
        PlayerPrefs.DeleteKey(BGM_MUTE_KEY);
        PlayerPrefs.DeleteKey(SFX_MUTE_KEY);
        PlayerPrefs.Save();

        LoadSavedSettings();
        ConnectFMODAndSync();
        Debug.Log("[MenuAudioManager] Đã khôi phục cài đặt âm thanh về mặc định!");
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        if (playerHealth != null)
        {
            playerHealth.OnDeathEvent -= OnPlayerDied;
            playerHealth.OnRespawnEvent -= OnPlayerRespawned;
        }
    }
}