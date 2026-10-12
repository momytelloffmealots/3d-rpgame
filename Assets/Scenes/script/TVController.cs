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

    [Header("Audio Occlusion (Hệ thống cản âm)")]
    public Transform playerTransform; // Kéo Main Camera hoặc Player vào đây
    public LayerMask obstacleLayer;   // Chọn Layer của Tường / Vật cản
    public float filterTransitionSpeed = 8f; // Tốc độ làm mượt tiếng (càng cao đổi càng nhanh)

    private int currentChannelIndex = 0;
    private float currentVolume = 1f;
    private bool isTVOn = false;
    private Coroutine switchRoutine;

    // Biến dùng để làm mượt âm thanh
    private float targetFilter = 0f;
    private float currentFilter = 0f;

    void Start()
    {
        tvInstance = RuntimeManager.CreateInstance(tvEvent);
        RuntimeManager.AttachInstanceToGameObject(tvInstance, transform, GetComponent<Rigidbody>());
        UpdateTVState(shutdownIndex);
    }

    void Update()
    {
        // ÉP FMOD PHẢI BÁM CHẶT LẤY VỊ TRÍ CỦA TV MỖI KHUNG HÌNH (DÙ BỊ NÉM ĐI)
        tvInstance.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(gameObject));
        // Hệ thống cản âm chỉ chạy khi TV đang bật và đã gắn Player
        if (isTVOn && playerTransform != null)
        {
            CheckOcclusion();
        }
    }

    private void CheckOcclusion()
    {
        // Bắn 1 tia Linecast từ tâm TV đến tâm của Người chơi
        // Nó chỉ tương tác với những vật mang Layer được chỉ định trong obstacleLayer
        if (Physics.Linecast(transform.position, playerTransform.position, obstacleLayer))
        {
            targetFilter = 1f; // Bị tường chắn -> Cài mục tiêu là 1 (Đục tiếng)
        }
        else
        {
            targetFilter = 0f; // Khong bị chắn -> Cài mục tiêu là 0 (Trong trẻo)
        }

        // LÀM MƯỢT: Thay vì đổi đột ngột từ 0 sang 1, hàm Lerp sẽ trượt từ từ
        if (Mathf.Abs(currentFilter - targetFilter) > 0.01f)
        {
            currentFilter = Mathf.Lerp(currentFilter, targetFilter, Time.deltaTime * filterTransitionSpeed);
            tvInstance.setParameterByName("Filter", currentFilter);
        }
    }

    // ... (CÁC HÀM BÊN DƯỚI GIỮ NGUYÊN NHƯ CŨ) ...

    public void NextChannel()
    {
        if (!isTVOn) return;
        currentChannelIndex++;
        if (currentChannelIndex > maxChannelIndex) currentChannelIndex = 0;

        if (switchRoutine != null) StopCoroutine(switchRoutine);
        switchRoutine = StartCoroutine(SwitchWithNoise());
    }

    public void TogglePower()
    {
        isTVOn = !isTVOn;
        if (switchRoutine != null) StopCoroutine(switchRoutine);

        if (isTVOn)
        {
            tvInstance.start();
            switchRoutine = StartCoroutine(SwitchWithNoise());
        }
        else
        {
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