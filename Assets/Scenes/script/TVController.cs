using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using System.Collections;

public class TVController : MonoBehaviour
{
    [Header("FMOD Settings")]
    public EventReference tvEvent;
    private EventInstance tvInstance;

    [Header("Cấu hình số đếm (Đã khớp với FMOD)")]
    public int maxChannelIndex = 2;
    public int shutdownIndex = 3;
    public int noiseIndex = 4;
    public float noiseDuration = 0.25f;

    private int currentChannelIndex = 0;
    private float currentVolume = 1f;

    // 1. Sửa chỗ này thành false để mặc định TV đang tắt
    private bool isTVOn = false;

    private Coroutine switchRoutine;

    void Start()
    {
        tvInstance = RuntimeManager.CreateInstance(tvEvent);
        RuntimeManager.AttachInstanceToGameObject(tvInstance, transform, GetComponent<Rigidbody>());

        // 2. Chuyển tham số FMOD sang mốc tắt để chuẩn bị sẵn
        UpdateTVState(shutdownIndex);

        // Đã xóa dòng tvInstance.start() ở đây để TV hoàn toàn im lặng lúc mới vào game
    }

    public void NextChannel()
    {
        if (!isTVOn) return;

        currentChannelIndex++;
        if (currentChannelIndex > maxChannelIndex)
        {
            currentChannelIndex = 0;
        }

        if (switchRoutine != null) StopCoroutine(switchRoutine);
        switchRoutine = StartCoroutine(SwitchWithNoise());
    }

    public void TogglePower()
    {
        isTVOn = !isTVOn;

        if (switchRoutine != null) StopCoroutine(switchRoutine);

        if (isTVOn)
        {
            // BẬT TIVI: 
            tvInstance.start();
            switchRoutine = StartCoroutine(SwitchWithNoise());
        }
        else
        {
            // TẮT TIVI:
            UpdateTVState(shutdownIndex);
        }
    }

    public void ChangeVolume(float amount)
    {
        if (!isTVOn) return;
        currentVolume = Mathf.Clamp(currentVolume + amount, 0f, 1f);
        tvInstance.setParameterByName("Volume", currentVolume);
    }

    private IEnumerator SwitchWithNoise()
    {
        UpdateTVState(noiseIndex);
        yield return new WaitForSeconds(noiseDuration);
        UpdateTVState(currentChannelIndex);
    }

    private void UpdateTVState(int targetIndex)
    {
        tvInstance.setParameterByName("Chanel", targetIndex);
        tvInstance.setParameterByName("Volume", currentVolume);
    }

    void OnDestroy()
    {
        tvInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        tvInstance.release();
    }
}