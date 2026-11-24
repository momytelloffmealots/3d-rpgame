using UnityEngine;
using FMODUnity;
using FMOD.Studio;

/// <summary>
/// Quản lý âm thanh trung tâm của game (FMOD Bus Manager).
/// Trách nhiệm:
/// 1. Điều khiển âm lượng & trạng thái Mute cho 2 kênh chính trong FMOD: "bus:/BGM" và "bus:/SFX".
/// 2. Lưu và khôi phục cài đặt âm thanh của người chơi qua PlayerPrefs.
/// 3. Cung cấp hàm phát âm thanh 2D / 3D / Attached an toàn và tự tắt AudioListener bị trùng trong Scene.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("=== CÀI ĐẶT ÂM LƯỢNG MẶC ĐỊNH ===")]
    [Range(0.0001f, 1f)] public float defaultBgmVolume = 0.25f;
    [Range(0.0001f, 1f)] public float defaultSfxVolume = 1.0f;

    [Header("=== TỐI ƯU SCENE ===")]
    [Tooltip("Tự động giữ lại đúng 1 AudioListener trên Camera chính và tắt các AudioListener dư thừa")]
    public bool autoFixDuplicateAudioListeners = true;

    // Khóa lưu PlayerPrefs (giữ nguyên tên cũ để tương thích với MenuAudioManager)
    private const string BGM_VOL_KEY = "Setting_BGM_Volume";
    private const string SFX_VOL_KEY = "Setting_SFX_Volume";
    private const string LAST_BGM_VOL_KEY = "Setting_Last_BGM_Volume";
    private const string LAST_SFX_VOL_KEY = "Setting_Last_SFX_Volume";
    private const string BGM_MUTE_KEY = "Setting_BGM_Mute";
    private const string SFX_MUTE_KEY = "Setting_SFX_Mute";
    private const float MIN_VOL = 0.0001f;

    private class BusChannel
    {
        public string busPath;
        public string volKey;
        public string lastVolKey;
        public string muteKey;
        public Bus bus;
        public float volume;
        public float lastVolumeBeforeMute;
        public bool isMuted;
    }

    private BusChannel bgmChannel;
    private BusChannel sfxChannel;
    private bool isPlayerDead;

    public float CurrentBgmVolume => bgmChannel != null ? bgmChannel.volume : defaultBgmVolume;
    public float CurrentSfxVolume => sfxChannel != null ? sfxChannel.volume : defaultSfxVolume;
    public bool IsBgmMuted => bgmChannel != null && bgmChannel.isMuted;
    public bool IsSfxMuted => sfxChannel != null && sfxChannel.isMuted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        bgmChannel = CreateChannel("bus:/BGM", BGM_VOL_KEY, LAST_BGM_VOL_KEY, BGM_MUTE_KEY, defaultBgmVolume);
        sfxChannel = CreateChannel("bus:/SFX", SFX_VOL_KEY, LAST_SFX_VOL_KEY, SFX_MUTE_KEY, defaultSfxVolume);
        LoadSettings();
    }

    private void Start()
    {
        if (autoFixDuplicateAudioListeners)
            FixDuplicateAudioListeners();

        ReconnectBuses();
        ApplyAllVolumes();
    }

    private BusChannel CreateChannel(string path, string volKey, string lastVolKey, string muteKey, float fallbackVol)
    {
        return new BusChannel
        {
            busPath = path,
            volKey = volKey,
            lastVolKey = lastVolKey,
            muteKey = muteKey,
            volume = fallbackVol,
            lastVolumeBeforeMute = fallbackVol
        };
    }

    private void LoadSettings()
    {
        LoadChannel(bgmChannel, defaultBgmVolume);
        LoadChannel(sfxChannel, defaultSfxVolume);
    }

    private void LoadChannel(BusChannel ch, float fallback)
    {
        ch.volume = Mathf.Clamp(PlayerPrefs.GetFloat(ch.volKey, fallback), MIN_VOL, 1f);
        ch.lastVolumeBeforeMute = Mathf.Clamp(PlayerPrefs.GetFloat(ch.lastVolKey, fallback), MIN_VOL, 1f);
        ch.isMuted = PlayerPrefs.GetInt(ch.muteKey, 0) == 1;
    }

    private void ReconnectBuses()
    {
        ConnectBus(bgmChannel);
        ConnectBus(sfxChannel);
    }

    private void ConnectBus(BusChannel ch)
    {
        try
        {
            ch.bus = RuntimeManager.GetBus(ch.busPath);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[AudioManager] Chưa kết nối được {ch.busPath}: {e.Message}");
        }
    }

    // --- ĐIỀU KHIỂN ÂM LƯỢNG & MUTE ---
    public void SetBGMVolume(float volume) => SetChannelVolume(bgmChannel, volume, isPlayerDead);
    public void SetBGMMuted(bool muted) => SetChannelMuted(bgmChannel, muted, defaultBgmVolume, isPlayerDead);
    public void SetSFXVolume(float volume) => SetChannelVolume(sfxChannel, volume, false);
    public void SetSFXMuted(bool muted) => SetChannelMuted(sfxChannel, muted, defaultSfxVolume, false);

    public void SetPlayerDead(bool dead)
    {
        isPlayerDead = dead;
        ApplyChannel(bgmChannel, isPlayerDead);
    }

    public void ApplyAllVolumes()
    {
        ApplyChannel(bgmChannel, isPlayerDead);
        ApplyChannel(sfxChannel, false);
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

        LoadSettings();
        ApplyAllVolumes();
    }

    private void SetChannelVolume(BusChannel ch, float volume, bool forceMute)
    {
        if (volume <= MIN_VOL)
        {
            ch.isMuted = true;
            ch.volume = MIN_VOL;
        }
        else
        {
            ch.isMuted = false;
            ch.volume = Mathf.Clamp01(volume);
            ch.lastVolumeBeforeMute = ch.volume;
        }

        SaveChannel(ch);
        ApplyChannel(ch, forceMute);
    }

    private void SetChannelMuted(BusChannel ch, bool muted, float fallbackVol, bool forceMute)
    {
        ch.isMuted = muted;
        if (muted)
        {
            if (ch.volume > MIN_VOL) ch.lastVolumeBeforeMute = ch.volume;
            ch.volume = MIN_VOL;
        }
        else
        {
            ch.volume = ch.lastVolumeBeforeMute > MIN_VOL ? ch.lastVolumeBeforeMute : fallbackVol;
        }

        SaveChannel(ch);
        ApplyChannel(ch, forceMute);
    }

    private void SaveChannel(BusChannel ch)
    {
        PlayerPrefs.SetFloat(ch.volKey, ch.volume);
        PlayerPrefs.SetFloat(ch.lastVolKey, ch.lastVolumeBeforeMute);
        PlayerPrefs.SetInt(ch.muteKey, ch.isMuted ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void ApplyChannel(BusChannel ch, bool forceMute)
    {
        if (!ch.bus.isValid()) ConnectBus(ch);
        if (!ch.bus.isValid()) return;

        bool muteState = ch.isMuted || forceMute;
        ch.bus.setMute(muteState);
        ch.bus.setVolume(muteState ? MIN_VOL : ch.volume);
    }

    // --- HÀM PHÁT ÂM THANH FMOD AN TOÀN ---
    public void PlayOneShot2D(EventReference evt)
    {
        if (evt.IsNull) return;
        try { RuntimeManager.PlayOneShot(evt); }
        catch (System.Exception e) { Debug.LogWarning($"[AudioManager] Lỗi phát 2D Event: {e.Message}"); }
    }

    public void PlayOneShot3D(EventReference evt, Vector3 position)
    {
        if (evt.IsNull) return;
        try { RuntimeManager.PlayOneShot(evt, position); }
        catch (System.Exception e) { Debug.LogWarning($"[AudioManager] Lỗi phát 3D Event: {e.Message}"); }
    }

    public void PlayOneShotAttached(EventReference evt, GameObject target)
    {
        if (evt.IsNull || target == null) return;
        try { RuntimeManager.PlayOneShotAttached(evt, target); }
        catch (System.Exception e) { Debug.LogWarning($"[AudioManager] Lỗi phát Attached Event: {e.Message}"); }
    }

    /// <summary>
    /// Đảm bảo trong Scene chỉ có đúng 1 AudioListener đang bật để tránh cảnh báo spam trong Console.
    /// </summary>
    private void FixDuplicateAudioListeners()
    {
        AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
        AudioListener primary = Camera.main != null ? Camera.main.GetComponent<AudioListener>() : null;

        if (primary == null || !primary.enabled)
        {
            foreach (AudioListener listener in listeners)
            {
                if (listener != null && listener.enabled)
                {
                    primary = listener;
                    break;
                }
            }
        }

        foreach (AudioListener listener in listeners)
        {
            if (listener != null && listener.enabled && listener != primary)
            {
                listener.enabled = false;
                Debug.LogWarning($"[AudioManager] Đã tắt AudioListener dư thừa trên '{listener.gameObject.name}' (chỉ giữ '{primary?.gameObject.name}').");
            }
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
