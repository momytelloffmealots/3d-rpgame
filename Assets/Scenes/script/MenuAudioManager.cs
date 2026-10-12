using UnityEngine;
using UnityEngine.UI;
using FMODUnity;
using FMOD.Studio;
using Sydewa;

public class MenuAudioManager : MonoBehaviour
{
    public static MenuAudioManager Instance { get; private set; }

    [Header("Default Audio Settings")]
    [Tooltip("Mức âm lượng BGM mặc định ban đầu (0.25 = 25%)")]
    [Range(0f, 1f)] public float defaultBgmVolume = 0.25f;

    [Tooltip("Mức âm lượng SFX mặc định ban đầu (1.0 = 100%)")]
    [Range(0f, 1f)] public float defaultSfxVolume = 1.0f;

    [Header("UI Slider")]
    public Slider bgmSlider;
    public Slider sfxSlider;

    [Header("UI Mute Toggle (Tách riêng BGM & SFX)")]
    public Toggle muteBgmToggle; // Nút Toggle tắt tiếng Nhạc nền
    public Toggle muteSfxToggle; // Nút Toggle tắt tiếng Hiệu ứng

    // Khai báo các kênh (Bus) của FMOD
    private Bus bgmBus;
    private Bus sfxBus;

    // Biến lưu trữ lại mức âm lượng
    private float currentBgmVolume = 0.25f;
    private float savedUnitySfxVolume = 1f;
    private bool isPlayerDead = false;
    private bool isInitialized = false;

    private PlayerHealth subscribedPlayerHealth;

    // Keys lưu trữ PlayerPrefs
    private const string BGM_VOL_KEY = "Setting_BGM_Volume";
    private const string SFX_VOL_KEY = "Setting_SFX_Volume";
    private const string BGM_MUTE_KEY = "Setting_BGM_Mute";
    private const string SFX_MUTE_KEY = "Setting_SFX_Mute";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        InitializeAudio();
    }

    void OnEnable()
    {
        InitializeAudio();
        HookPlayerHealth();
    }

    void Start()
    {
        HookPlayerHealth();
    }

    private void InitializeAudio()
    {
        if (isInitialized) return;

        // 1. KẾT NỐI VỚI CÁC KÊNH TRONG FMOD
        bgmBus = RuntimeManager.GetBus("bus:/BGM");
        sfxBus = RuntimeManager.GetBus("bus:/SFX");

        // 2. TẢI CÀI ĐẶT ĐÃ LƯU HOẶC MẶC ĐỊNH (BGM mặc định 25%)
        currentBgmVolume = PlayerPrefs.GetFloat(BGM_VOL_KEY, defaultBgmVolume);
        float loadedSfxVolume = PlayerPrefs.GetFloat(SFX_VOL_KEY, defaultSfxVolume);
        bool isBgmMuted = PlayerPrefs.GetInt(BGM_MUTE_KEY, 0) == 1;
        bool isSfxMuted = PlayerPrefs.GetInt(SFX_MUTE_KEY, 0) == 1;

        // 3. GẮN SỰ KIỆN CHO BGM
        if (bgmSlider != null)
        {
            bgmSlider.onValueChanged.RemoveListener(SetBGMVolume);
            bgmSlider.value = currentBgmVolume;
            bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        }

        if (muteBgmToggle != null)
        {
            muteBgmToggle.onValueChanged.RemoveListener(ToggleMuteBGM);
            muteBgmToggle.isOn = isBgmMuted;
            muteBgmToggle.onValueChanged.AddListener(ToggleMuteBGM);
        }

        // 4. GẮN SỰ KIỆN CHO SFX
        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveListener(SetSFXVolume);
            sfxSlider.value = loadedSfxVolume;
            sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        }

        if (muteSfxToggle != null)
        {
            muteSfxToggle.onValueChanged.RemoveListener(ToggleMuteSFX);
            muteSfxToggle.isOn = isSfxMuted;
            muteSfxToggle.onValueChanged.AddListener(ToggleMuteSFX);
        }

        // Áp dụng âm lượng BGM ban đầu (25%)
        ApplyBGMVolume();

        // Áp dụng âm lượng SFX ban đầu
        SetSFXVolume(loadedSfxVolume);
        if (isSfxMuted)
        {
            ToggleMuteSFX(true);
        }

        isInitialized = true;
    }

    private void HookPlayerHealth()
    {
        if (subscribedPlayerHealth == null)
        {
            subscribedPlayerHealth = Object.FindAnyObjectByType<PlayerHealth>();
            if (subscribedPlayerHealth != null)
            {
                subscribedPlayerHealth.OnDeathEvent -= OnPlayerDied;
                subscribedPlayerHealth.OnDeathEvent += OnPlayerDied;
                subscribedPlayerHealth.OnRespawnEvent -= OnPlayerRespawned;
                subscribedPlayerHealth.OnRespawnEvent += OnPlayerRespawned;
            }
        }
    }

    // --- XỬ LÝ NHẠC NỀN (BGM) ---
    public void SetBGMVolume(float volume)
    {
        currentBgmVolume = volume;
        PlayerPrefs.SetFloat(BGM_VOL_KEY, volume);
        PlayerPrefs.Save();

        ApplyBGMVolume();
    }

    public void ToggleMuteBGM(bool isMuted)
    {
        PlayerPrefs.SetInt(BGM_MUTE_KEY, isMuted ? 1 : 0);
        PlayerPrefs.Save();

        ApplyBGMVolume();
    }

    private void ApplyBGMVolume()
    {
        // 1. Nếu Player đã chết -> Tắt hoàn toàn nhạc nền
        if (isPlayerDead)
        {
            bgmBus.setMute(true);
            bgmBus.setPaused(true);
            bgmBus.setVolume(0f);

            LightingManager lm = Object.FindAnyObjectByType<LightingManager>();
            if (lm != null)
            {
                lm.SetBGMPaused(true);
            }
            return;
        }

        // 2. Nếu người dùng chọn Mute trong setting
        bool userMuted = (muteBgmToggle != null && muteBgmToggle.isOn);
        bgmBus.setMute(userMuted);
        bgmBus.setPaused(userMuted);

        float finalVolume = userMuted ? 0f : currentBgmVolume;
        bgmBus.setVolume(finalVolume);

        LightingManager lightingMgr = Object.FindAnyObjectByType<LightingManager>();
        if (lightingMgr != null)
        {
            lightingMgr.SetBGMPaused(userMuted);
            lightingMgr.SetBGMVolume(finalVolume);
        }
    }

    /// <summary>
    /// Gọi khi Player tử trận: Tắt background music
    /// </summary>
    public void OnPlayerDied()
    {
        isPlayerDead = true;
        ApplyBGMVolume();
        Debug.Log("[MenuAudioManager] Player tử trận -> Đã TẮT Background Music.");
    }

    /// <summary>
    /// Gọi khi Player hồi sinh: Bật lại background music với âm lượng đã lưu
    /// </summary>
    public void OnPlayerRespawned()
    {
        isPlayerDead = false;
        ApplyBGMVolume();
        Debug.Log($"[MenuAudioManager] Player hồi sinh -> Đã BẬT LẠI Background Music ({currentBgmVolume * 100f:F0}%).");
    }

    // --- XỬ LÝ HIỆU ỨNG (SFX) ---
    public void SetSFXVolume(float volume)
    {
        sfxBus.setVolume(volume);
        savedUnitySfxVolume = volume;
        PlayerPrefs.SetFloat(SFX_VOL_KEY, volume);
        PlayerPrefs.Save();

        if (muteSfxToggle != null && !muteSfxToggle.isOn)
        {
            AudioListener.volume = volume;
        }
    }

    public void ToggleMuteSFX(bool isMuted)
    {
        sfxBus.setMute(isMuted);
        PlayerPrefs.SetInt(SFX_MUTE_KEY, isMuted ? 1 : 0);
        PlayerPrefs.Save();

        if (isMuted)
        {
            AudioListener.volume = 0f;
        }
        else
        {
            AudioListener.volume = savedUnitySfxVolume;
        }
    }

    private void OnDestroy()
    {
        if (subscribedPlayerHealth != null)
        {
            subscribedPlayerHealth.OnDeathEvent -= OnPlayerDied;
            subscribedPlayerHealth.OnRespawnEvent -= OnPlayerRespawned;
        }
    }
}