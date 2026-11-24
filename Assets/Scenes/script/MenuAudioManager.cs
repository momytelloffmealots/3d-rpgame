using UnityEngine;
using UnityEngine.UI;
using FMODUnity;
using FMOD.Studio;

/// <summary>
/// Cầu nối giữa giao diện Menu (Slider / Toggle) và hệ thống âm lượng FMOD.
/// Nếu Scene có AudioManager: ủy quyền toàn bộ điều khiển Bus & PlayerPrefs cho AudioManager.
/// Nếu Scene chưa có AudioManager (như main.unity): tự kết nối trực tiếp bus:/BGM và bus:/SFX dự phòng.
/// </summary>
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
        ConnectFMODAndSync();
        HookPlayerHealth();
    }

    private void Start()
    {
        ConnectFMODAndSync();
    }

    /// <summary>
    /// Kết nối lại FMOD Bus mới nhất, gắn listener cho UI và đồng bộ âm lượng.
    /// </summary>
    public void ConnectFMODAndSync()
    {
        if (AudioManager.Instance == null)
            ReconnectFMODBuses();
        else
            SyncStateFromAudioManager();

        SetupUIListeners();
        SyncUIWithSettings();
        ApplyAllAudioStates();
    }

    public void RefreshUIAndAudio() => ConnectFMODAndSync();

    private void SyncStateFromAudioManager()
    {
        AudioManager am = AudioManager.Instance;
        currentBgmVolume = am.CurrentBgmVolume;
        currentSfxVolume = am.CurrentSfxVolume;
        isBgmMuted = am.IsBgmMuted;
        isSfxMuted = am.IsSfxMuted;
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
        BindSlider(bgmSlider, SetBGMVolume);
        BindSlider(sfxSlider, SetSFXVolume);
        BindToggle(muteBgmToggle, ToggleMuteBGM);
        BindToggle(muteSfxToggle, ToggleMuteSFX);
    }

    private void BindSlider(Slider slider, UnityEngine.Events.UnityAction<float> handler)
    {
        if (slider == null) return;
        slider.onValueChanged.RemoveListener(handler);
        slider.onValueChanged.AddListener(handler);
    }

    private void BindToggle(Toggle toggle, UnityEngine.Events.UnityAction<bool> handler)
    {
        if (toggle == null) return;
        toggle.onValueChanged.RemoveListener(handler);
        toggle.onValueChanged.AddListener(handler);
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

    // --- ĐIỀU KHIỂN NHẠC NỀN (BGM) ---
    public void SetBGMVolume(float volume)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetBGMVolume(volume);
            SyncStateFromAudioManager();
            SyncUIWithSettings();
            return;
        }

        ApplyLocalVolume(volume, ref currentBgmVolume, ref lastBgmVolumeBeforeMute, ref isBgmMuted);
        SaveSettings(BGM_VOL_KEY, LAST_BGM_VOL_KEY, BGM_MUTE_KEY, currentBgmVolume, lastBgmVolumeBeforeMute, isBgmMuted);
        SyncUIWithSettings();
        ApplyBGMVolume();
    }

    public void ToggleMuteBGM(bool isMuted)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetBGMMuted(isMuted);
            SyncStateFromAudioManager();
            SyncUIWithSettings();
            return;
        }

        ApplyLocalMute(isMuted, defaultBgmVolume, ref currentBgmVolume, ref lastBgmVolumeBeforeMute, ref isBgmMuted);
        SaveSettings(BGM_VOL_KEY, LAST_BGM_VOL_KEY, BGM_MUTE_KEY, currentBgmVolume, lastBgmVolumeBeforeMute, isBgmMuted);
        SyncUIWithSettings();
        ApplyBGMVolume();
    }

    // --- ĐIỀU KHIỂN HIỆU ỨNG (SFX) ---
    public void SetSFXVolume(float volume)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXVolume(volume);
            SyncStateFromAudioManager();
            SyncUIWithSettings();
            return;
        }

        ApplyLocalVolume(volume, ref currentSfxVolume, ref lastSfxVolumeBeforeMute, ref isSfxMuted);
        SaveSettings(SFX_VOL_KEY, LAST_SFX_VOL_KEY, SFX_MUTE_KEY, currentSfxVolume, lastSfxVolumeBeforeMute, isSfxMuted);
        SyncUIWithSettings();
        ApplySFXVolume();
    }

    public void ToggleMuteSFX(bool isMuted)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXMuted(isMuted);
            SyncStateFromAudioManager();
            SyncUIWithSettings();
            return;
        }

        ApplyLocalMute(isMuted, defaultSfxVolume, ref currentSfxVolume, ref lastSfxVolumeBeforeMute, ref isSfxMuted);
        SaveSettings(SFX_VOL_KEY, LAST_SFX_VOL_KEY, SFX_MUTE_KEY, currentSfxVolume, lastSfxVolumeBeforeMute, isSfxMuted);
        SyncUIWithSettings();
        ApplySFXVolume();
    }

    private void ApplyLocalVolume(float volume, ref float current, ref float lastBeforeMute, ref bool muted)
    {
        if (volume <= MIN_VOL)
        {
            muted = true;
            current = MIN_VOL;
        }
        else
        {
            muted = false;
            current = volume;
            lastBeforeMute = volume;
        }
    }

    private void ApplyLocalMute(bool isMuted, float fallback, ref float current, ref float lastBeforeMute, ref bool muted)
    {
        muted = isMuted;
        if (isMuted)
        {
            if (current > MIN_VOL) lastBeforeMute = current;
            current = MIN_VOL;
        }
        else
        {
            current = (lastBeforeMute > MIN_VOL) ? lastBeforeMute : fallback;
        }
    }

    private void SaveSettings(string volKey, string lastKey, string muteKey, float vol, float lastVol, bool muted)
    {
        PlayerPrefs.SetFloat(volKey, vol);
        PlayerPrefs.SetFloat(lastKey, lastVol);
        PlayerPrefs.SetInt(muteKey, muted ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void ApplyBGMVolume()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ApplyAllVolumes();
            return;
        }

        bool shouldMute = isBgmMuted || isPlayerDead;
        ApplyBus(ref bgmBus, shouldMute, currentBgmVolume);
    }

    private void ApplySFXVolume()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ApplyAllVolumes();
            return;
        }

        ApplyBus(ref sfxBus, isSfxMuted, currentSfxVolume);
    }

    private void ApplyBus(ref Bus bus, bool mute, float volume)
    {
        if (!bus.isValid()) ReconnectFMODBuses();
        if (!bus.isValid()) return;

        bus.setMute(mute);
        bus.setVolume(mute ? MIN_VOL : volume);
    }

    private void ApplyAllAudioStates()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ApplyAllVolumes();
            return;
        }

        ApplyBGMVolume();
        ApplySFXVolume();
    }

    public void OnPlayerDied()
    {
        isPlayerDead = true;
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetPlayerDead(true);
        else
            ApplyBGMVolume();
    }

    public void OnPlayerRespawned()
    {
        isPlayerDead = false;
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetPlayerDead(false);
        else
            ApplyBGMVolume();
    }

    [ContextMenu("Reset All Audio Settings To Default")]
    public void ResetToDefault()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ResetToDefault();
            SyncStateFromAudioManager();
            SyncUIWithSettings();
            return;
        }

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
        if (Instance == this) Instance = null;
        if (playerHealth != null)
        {
            playerHealth.OnDeathEvent -= OnPlayerDied;
            playerHealth.OnRespawnEvent -= OnPlayerRespawned;
        }
    }
}
