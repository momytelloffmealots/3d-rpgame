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
    public int maxChannelIndex = 2; // chanel 3 là số 2
    public int shutdownIndex = 3;   // shutdown là số 3
    public int noiseIndex = 4;      // noise là số 4
    public float noiseDuration = 0.25f;

    private int currentChannelIndex = 0; // Bắt đầu ở chanel 1 (số 0)
    private float currentVolume = 1f;
    private bool isTVOn = true;
    private Coroutine switchRoutine;

    void Start()
    {
        tvInstance = RuntimeManager.CreateInstance(tvEvent);
        RuntimeManager.AttachInstanceToGameObject(tvInstance, transform, GetComponent<Rigidbody>());
        tvInstance.start();

        // Mới vào game thì phát kênh đầu tiên
        UpdateTVState(currentChannelIndex);
    }

    public void NextChannel()
    {
        if (!isTVOn) return;

        currentChannelIndex++;
        if (currentChannelIndex > maxChannelIndex)
        {
            currentChannelIndex = 0; // Vòng lại chanel 1
        }

        if (switchRoutine != null) StopCoroutine(switchRoutine);
        switchRoutine = StartCoroutine(SwitchWithNoise());
    }

    public void TogglePower()
    {
        isTVOn = !isTVOn;

        // Dừng các hiệu ứng chuyển kênh đang dở dang
        if (switchRoutine != null) StopCoroutine(switchRoutine);

        if (isTVOn)
        {
            // BẬT TIVI: 
            // 1. Đánh thức FMOD sống lại (vì lúc tắt xong nó đã tự ngủ đông)
            tvInstance.start();

            // 2. Chạy hiệu ứng xẹt xẹt nhiễu sóng trước khi chiếu kênh
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
        // 1. Chuyển tham số sang tiếng xẹt xẹt (Số 4)
        UpdateTVState(noiseIndex);

        yield return new WaitForSeconds(noiseDuration);

        // 2. Chuyển tham số sang kênh mới
        UpdateTVState(currentChannelIndex);
    }

    private void UpdateTVState(int targetIndex)
    {
        // Gửi chính xác số thứ tự sang FMOD
        tvInstance.setParameterByName("Chanel", targetIndex);
        tvInstance.setParameterByName("Volume", currentVolume);
    }

    void OnDestroy()
    {
        tvInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        tvInstance.release();
    }
}