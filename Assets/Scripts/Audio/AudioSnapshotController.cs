using UnityEngine;
using FMODUnity;
using FMOD.Studio;

/// <summary>
/// Điều khiển Mixing động (Dynamic Mixing) thông qua Snapshot & Persistent Event có sẵn trong FMOD Studio:
/// 1. snapshot:/Pause         -> Giảm nhỏ âm thanh nền & hiệu ứng khi người chơi bấm Esc mở Menu.
/// 2. event:/Character/Health -> Event liên tục tự động bật tiếng tim đập + snapshot "Health Low" (lọc Lowpass) khi máu < 25%.
/// </summary>
public class AudioSnapshotController : MonoBehaviour
{
    public static AudioSnapshotController Instance { get; private set; }

    [Header("=== ĐƯỜNG DẪN FMOD SNAPSHOT & EVENT ===")]
    public string pauseSnapshotPath = "snapshot:/Pause";
    public string healthEventPath = "event:/Character/Health";

    [Header("=== TÙY CHỌN ===")]
    [Tooltip("Tự động phát tiếng nhịp tim & làm đục âm thanh khi máu nhân vật xuống dưới 25%")]
    public bool enableHealthHeartbeat = true;

    private EventInstance pauseSnapshot;
    private EventInstance healthInstance;

    private bool isPaused;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Khởi tạo Persistent Event theo dõi máu (trong FMOD, Health < 25 sẽ tự kích hoạt Heartbeat + Health Low snapshot)
        if (enableHealthHeartbeat && !string.IsNullOrEmpty(healthEventPath))
        {
            try
            {
                healthInstance = RuntimeManager.CreateInstance(healthEventPath);
                healthInstance.setParameterByName("Health", 100f);
                healthInstance.start();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[AudioSnapshotController] Không khởi tạo được '{healthEventPath}': {e.Message}");
            }
        }
    }

    /// <summary>
    /// Bật/tắt trạng thái âm thanh khi mở Pause Menu.
    /// </summary>
    public void SetPaused(bool paused)
    {
        if (isPaused == paused) return;
        isPaused = paused;
        SetSnapshotActive(ref pauseSnapshot, pauseSnapshotPath, paused);
    }

    /// <summary>
    /// Cập nhật % máu cho FMOD Event "event:/Character/Health".
    /// Khi máu dưới 25% và chưa chết, FMOD tự động phát nhịp tim + lọc âm "Health Low".
    /// </summary>
    public void UpdatePlayerHealth(float currentHealth, float maxHealth, bool isDead)
    {
        if (!healthInstance.isValid()) return;

        // Khi nhân vật đã tử trận, trả tham số Health về 100 để tắt tiếng nhịp tim
        float hpPercent = isDead
            ? 100f
            : Mathf.Clamp((currentHealth / Mathf.Max(1f, maxHealth)) * 100f, 0f, 100f);

        healthInstance.setParameterByName("Health", hpPercent);
    }

    private void SetSnapshotActive(ref EventInstance instance, string snapshotPath, bool active)
    {
        if (string.IsNullOrEmpty(snapshotPath)) return;

        if (active)
        {
            if (instance.isValid()) return;
            try
            {
                instance = RuntimeManager.CreateInstance(snapshotPath);
                instance.start();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[AudioSnapshotController] Không bật được '{snapshotPath}': {e.Message}");
            }
        }
        else
        {
            ReleaseInstance(ref instance, FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        }
    }

    private void ReleaseInstance(ref EventInstance instance, FMOD.Studio.STOP_MODE stopMode)
    {
        if (!instance.isValid()) return;
        instance.stop(stopMode);
        instance.release();
        instance.clearHandle();
    }

    private void OnDestroy()
    {
        ReleaseInstance(ref pauseSnapshot, FMOD.Studio.STOP_MODE.IMMEDIATE);
        ReleaseInstance(ref healthInstance, FMOD.Studio.STOP_MODE.IMMEDIATE);

        if (Instance == this) Instance = null;
    }
}
