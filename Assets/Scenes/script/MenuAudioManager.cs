using UnityEngine;
using UnityEngine.UI;
using FMODUnity;
using FMOD.Studio;

public class MenuAudioManager : MonoBehaviour
{
    [Header("UI Slider")]
    public Slider bgmSlider;
    public Slider sfxSlider;

    [Header("UI Mute Toggle (Tách riêng BGM & SFX)")]
    public Toggle muteBgmToggle; // Nút Toggle tắt tiếng Nhạc nền
    public Toggle muteSfxToggle; // Nút Toggle tắt tiếng Hiệu ứng

    // Khai báo các kênh (Bus) của FMOD
    private Bus bgmBus;
    private Bus sfxBus;

    // Biến lưu trữ lại mức âm lượng SFX của Unity trước khi bị Mute
    private float savedUnitySfxVolume = 1f;
    private bool isInitialized = false;

    void Awake()
    {
        InitializeAudio();
    }

    void OnEnable()
    {
        InitializeAudio();
    }

    private void InitializeAudio()
    {
        if (isInitialized) return;

        // 1. KẾT NỐI VỚI CÁC KÊNH TRONG FMOD
        bgmBus = RuntimeManager.GetBus("bus:/BGM");
        sfxBus = RuntimeManager.GetBus("bus:/SFX");

        // 2. GẮN SỰ KIỆN CHO BGM
        if (bgmSlider != null)
        {
            bgmSlider.onValueChanged.RemoveListener(SetBGMVolume);
            bgmSlider.value = 1f;
            bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        }

        if (muteBgmToggle != null)
        {
            muteBgmToggle.onValueChanged.RemoveListener(ToggleMuteBGM);
            muteBgmToggle.isOn = false;
            muteBgmToggle.onValueChanged.AddListener(ToggleMuteBGM);
        }

        // 3. GẮN SỰ KIỆN CHO SFX
        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveListener(SetSFXVolume);
            sfxSlider.value = 1f;
            sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        }

        if (muteSfxToggle != null)
        {
            muteSfxToggle.onValueChanged.RemoveListener(ToggleMuteSFX);
            muteSfxToggle.isOn = false;
            muteSfxToggle.onValueChanged.AddListener(ToggleMuteSFX);
        }

        isInitialized = true;
    }

    // --- XỬ LÝ NHẠC NỀN (BGM) ---
    public void SetBGMVolume(float volume)
    {
        bgmBus.setVolume(volume);
    }

    public void ToggleMuteBGM(bool isMuted)
    {
        bgmBus.setMute(isMuted);
    }

    // --- XỬ LÝ HIỆU ỨNG (SFX) ---
    public void SetSFXVolume(float volume)
    {
        sfxBus.setVolume(volume);
        savedUnitySfxVolume = volume;

        if (muteSfxToggle != null && !muteSfxToggle.isOn)
        {
            AudioListener.volume = volume;
        }
    }

    public void ToggleMuteSFX(bool isMuted)
    {
        sfxBus.setMute(isMuted);

        if (isMuted)
        {
            AudioListener.volume = 0f;
        }
        else
        {
            AudioListener.volume = savedUnitySfxVolume;
        }
    }
}