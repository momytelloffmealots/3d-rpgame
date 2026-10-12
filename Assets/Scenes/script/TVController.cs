using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using System.Collections;

public class TVController : MonoBehaviour
{
    public static TVController Instance { get; private set; }

    // Sự kiện máu TV và sự kiện Chiến Thắng
    public static event System.Action<float, float> OnTVHealthChanged; // currentHealth, maxHealth
    public static event System.Action OnTVDestroyed;                    // Kích hoạt khi TV hết máu (0 HP)

    [Header("=== MÁU & CHIẾN THẮNG (TV TARGET) ===")]
    [Tooltip("Máu tối đa của TV (mặc định 200 HP)")]
    public float maxHealth = 200f;
    public float currentHealth = 200f;
    public bool isDestroyed = false;

    [Tooltip("Prefab hiệu ứng vụ nổ khi phá hủy TV (tự động tìm Explosion nếu để trống)")]
    public GameObject destroyExplosionPrefab;

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
    private Coroutine flashRoutine;

    // Biến dùng để làm mượt âm thanh
    private float targetFilter = 0f;
    private float currentFilter = 0f;

    // Lưu trữ vật liệu để nhấp nháy đỏ khi bị bắn
    private Renderer[] tvRenderers;
    private Color[] originalColors;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        currentHealth = maxHealth;
    }

    void Start()
    {
        // 1. Tự động tìm Player nếu chưa gán
        if (playerTransform == null)
        {
            PlayerHealth ph = Object.FindAnyObjectByType<PlayerHealth>();
            if (ph != null) playerTransform = ph.transform;
            else if (Camera.main != null) playerTransform = Camera.main.transform;
        }

        // 2. Đảm bảo TV có Collider để đạn từ vũ khí (Easy Weapons) có thể bắn trúng
        EnsureColliderExists();

        // 3. Lưu màu sắc ban đầu của TV để nhấp nháy đỏ khi trúng đạn
        tvRenderers = GetComponentsInChildren<Renderer>();
        if (tvRenderers != null && tvRenderers.Length > 0)
        {
            originalColors = new Color[tvRenderers.Length];
            for (int i = 0; i < tvRenderers.Length; i++)
            {
                if (tvRenderers[i].material != null && tvRenderers[i].material.HasProperty("_Color"))
                {
                    originalColors[i] = tvRenderers[i].material.color;
                }
                else if (tvRenderers[i].material != null && tvRenderers[i].material.HasProperty("_BaseColor"))
                {
                    originalColors[i] = tvRenderers[i].material.GetColor("_BaseColor");
                }
            }
        }

        // 4. Khởi tạo âm thanh FMOD
        tvInstance = RuntimeManager.CreateInstance(tvEvent);
        RuntimeManager.AttachInstanceToGameObject(tvInstance, gameObject, GetComponent<Rigidbody>());
        UpdateTVState(shutdownIndex);
    }

    private void EnsureColliderExists()
    {
        Collider col = GetComponentInChildren<Collider>();
        if (col == null)
        {
            BoxCollider bc = gameObject.AddComponent<BoxCollider>();
            Renderer rend = GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                bc.center = transform.InverseTransformPoint(rend.bounds.center);
                bc.size = transform.InverseTransformVector(rend.bounds.size);
            }
            else
            {
                bc.size = new Vector3(1.5f, 1.0f, 0.3f);
            }
        }
    }

    void Update()
    {
        if (isDestroyed) return;

        // ÉP FMOD PHẢI BÁM CHẶT LẤY VỊ TRÍ CỦA TV MỖI KHUNG HÌNH (DÙ BỊ NÉM ĐI)
        tvInstance.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(gameObject));
        // Hệ thống cản âm chỉ chạy khi TV đang bật và đã gắn Player
        if (isTVOn && playerTransform != null)
        {
            CheckOcclusion();
        }
    }

    // ==========================================
    // XỬ LÝ NHẬN SÁT THƯƠNG TỪ SÚNG (EASY WEAPONS)
    // ==========================================

    /// <summary>
    /// Nhận gọi từ Easy Weapons (Raycast, Projectile, Explosion) qua SendMessageUpwards("ChangeHealth", -damage)
    /// </summary>
    public void ChangeHealth(float amount)
    {
        if (isDestroyed) return;

        // amount là số âm khi chịu sát thương
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        Debug.Log($"[TVController] TV trúng đạn! Lượng máu còn: {currentHealth:F0}/{maxHealth:F0} HP");
        OnTVHealthChanged?.Invoke(currentHealth, maxHealth);

        // Hiệu ứng chớp đỏ khi trúng đạn
        if (gameObject.activeInHierarchy)
        {
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(FlashDamageVisual());
        }

        // Kiểm tra điều kiện tử trận -> Chiến thắng
        if (currentHealth <= 0f)
        {
            DestroyTV();
        }
    }

    private IEnumerator FlashDamageVisual()
    {
        if (tvRenderers == null) yield break;

        // Đổi màu đỏ
        for (int i = 0; i < tvRenderers.Length; i++)
        {
            if (tvRenderers[i] != null && tvRenderers[i].material != null)
            {
                if (tvRenderers[i].material.HasProperty("_Color"))
                    tvRenderers[i].material.color = Color.red;
                else if (tvRenderers[i].material.HasProperty("_BaseColor"))
                    tvRenderers[i].material.SetColor("_BaseColor", Color.red);
            }
        }

        yield return new WaitForSeconds(0.08f);

        // Khôi phục màu gốc
        for (int i = 0; i < tvRenderers.Length; i++)
        {
            if (tvRenderers[i] != null && tvRenderers[i].material != null && originalColors != null && i < originalColors.Length)
            {
                if (tvRenderers[i].material.HasProperty("_Color"))
                    tvRenderers[i].material.color = originalColors[i];
                else if (tvRenderers[i].material.HasProperty("_BaseColor"))
                    tvRenderers[i].material.SetColor("_BaseColor", originalColors[i]);
            }
        }
    }

    /// <summary>
    /// Kích hoạt khi TV hết máu: nổ tung, tắt tiếng và kích hoạt Victory Screen
    /// </summary>
    private void DestroyTV()
    {
        if (isDestroyed) return;
        isDestroyed = true;

        Debug.Log("[TVController] TV ĐÃ BỊ PHÁ HỦY HOÀN TOÀN (0/200 HP)! KÍCH HOẠT CHIẾN THẮNG (VICTORY)!");

        // 1. Tắt âm thanh FMOD của TV
        if (tvInstance.isValid())
        {
            tvInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            tvInstance.release();
        }
        isTVOn = false;

        // 2. Sinh hiệu ứng nổ
        if (destroyExplosionPrefab != null)
        {
            Instantiate(destroyExplosionPrefab, transform.position, transform.rotation);
        }
        else
        {
            // Dự phòng: Tìm prefab vụ nổ có sẵn trong Easy Weapons
            Explosion defaultExp = Object.FindAnyObjectByType<Explosion>();
            if (defaultExp != null)
            {
                Instantiate(defaultExp.gameObject, transform.position, Quaternion.identity);
            }
        }

        // 3. Tác động lực đẩy văng nếu có Rigidbody
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.AddForce(Vector3.up * 4f + UnityEngine.Random.insideUnitSphere * 3f, ForceMode.Impulse);
            rb.AddTorque(UnityEngine.Random.insideUnitSphere * 10f, ForceMode.Impulse);
        }

        // 4. Phát sự kiện Chiến Thắng cho UI HUD
        OnTVDestroyed?.Invoke();
    }

    // ==========================================
    // CÁC HÀM ĐIỀU KHIỂN BẰNG REMOTE & CẢN ÂM
    // ==========================================

    private void CheckOcclusion()
    {
        if (Physics.Linecast(transform.position, playerTransform.position, obstacleLayer))
        {
            targetFilter = 1f; // Bị tường chắn -> Đục tiếng
        }
        else
        {
            targetFilter = 0f; // Không bị chắn -> Trong trẻo
        }

        if (Mathf.Abs(currentFilter - targetFilter) > 0.01f)
        {
            currentFilter = Mathf.Lerp(currentFilter, targetFilter, Time.deltaTime * filterTransitionSpeed);
            tvInstance.setParameterByName("Filter", currentFilter);
        }
    }

    public void NextChannel()
    {
        if (!isTVOn || isDestroyed) return;
        currentChannelIndex++;
        if (currentChannelIndex > maxChannelIndex) currentChannelIndex = 0;

        if (switchRoutine != null) StopCoroutine(switchRoutine);
        switchRoutine = StartCoroutine(SwitchWithNoise());
    }

    public void TogglePower()
    {
        if (isDestroyed) return;
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
        if (!isTVOn || isDestroyed) return;
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
        if (isDestroyed) return;
        tvInstance.setParameterByName("Chanel", targetIndex);
        tvInstance.setParameterByName("Volume", currentVolume);
    }

    void OnDestroy()
    {
        if (tvInstance.isValid())
        {
            tvInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            tvInstance.release();
        }
    }
}