using System;
using System.Collections;
using UnityEngine;
using FMODUnity;
using HealthSystem;

/// <summary>
/// Hệ thống máu của người chơi (Player Health).
/// Kế thừa từ Health để tương thích hoàn toàn với Easy Weapons (Weapon, Projectile, Explosion).
/// Hỗ trợ: FMOD Sound (Hurt/Die), Fallback Audio, Death Camera, Respawn, Debug hotkeys.
/// </summary>
public class PlayerHealth : Health
{
    [Header("=== FMOD AUDIO (PLACEHOLDERS) ===")]
    [Tooltip("Kéo FMOD Event cho Hurt Sound vào đây (ví dụ: event:/Character/Player Hurt)")]
    public EventReference hurtSoundFMOD;

    [Tooltip("Kéo FMOD Event cho Die Sound vào đây (ví dụ: event:/Character/Player Die)")]
    public EventReference dieSoundFMOD;

    [Header("=== UNITY AUDIO FALLBACK ===")]
    [Tooltip("AudioClip phát dự phòng nếu chưa gán FMOD Event")]
    public AudioClip hurtSoundFallback;
    [Tooltip("AudioClip phát dự phòng nếu chưa gán FMOD Event")]
    public AudioClip dieSoundFallback;
    [Tooltip("Cooldown giữa các lần phát hurt sound (tránh spam khi trúng đòn liên tục)")]
    public float hurtSoundCooldown = 0.15f;

    [Header("=== RESPAWN & SETTINGS ===")]
    [Tooltip("Điểm hồi sinh khi chết (để trống sẽ dùng vị trí xuất phát ban đầu)")]
    public Transform respawnPoint;

    [Header("=== DEBUG / TEST HOTKEYS ===")]
    [Tooltip("Bật các phím test nhanh: H (mất máu), J (hồi máu), K (tự sát), R (hồi sinh)")]
    public bool enableDebugKeys = true;

    // Events
    public event Action<float, float> OnHealthChanged;            // current, max
    public event Action<float, DamageSource> OnDamageTaken;       // damage, source
    public event Action<float> OnHeal;                            // amount
    public event Action OnDeathEvent;
    public event Action OnRespawnEvent;

    // Internal references
    private AudioSource audioSource;
    private FirstPersonCharacter fpCharacter;
    private MouseRotator[] mouseRotators;
    private WeaponSystem weaponSystem;
    private Transform cameraTransform;

    private Vector3 initialSpawnPosition;
    private Quaternion initialSpawnRotation;
    private Vector3 cameraInitialLocalPos;
    private Quaternion cameraInitialLocalRot;

    private float lastHurtSoundTime = -1f;
    private Coroutine deathCameraCoroutine;

    public override void Start()
    {
        base.Start();

        // Ghi nhận vị trí xuất phát
        initialSpawnPosition = transform.position;
        initialSpawnRotation = transform.rotation;

        // Tìm các thành phần điều khiển
        fpCharacter = GetComponent<FirstPersonCharacter>();
        mouseRotators = GetComponentsInChildren<MouseRotator>(true);
        weaponSystem = GetComponentInChildren<WeaponSystem>(true);

        Camera cam = GetComponentInChildren<Camera>();
        if (cam != null)
        {
            cameraTransform = cam.transform;
            cameraInitialLocalPos = cameraTransform.localPosition;
            cameraInitialLocalRot = cameraTransform.localRotation;
        }

        // Đảm bảo có AudioSource dự phòng
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D cho âm thanh của chính người chơi
        }

        // Báo cho UI lượng máu ban đầu
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Update()
    {
        if (enableDebugKeys)
        {
            HandleDebugKeys();
        }
    }

    private void HandleDebugKeys()
    {
        // [H]: Mất 15 máu (Bullet / Generic)
        if (Input.GetKeyDown(KeyCode.H) && !dead)
        {
            Debug.Log("[DEBUG] Phím H: Trừ 15 HP");
            TakeDamage(15f, DamageSource.Generic);
        }

        // [J]: Hồi 25 máu
        if (Input.GetKeyDown(KeyCode.J) && !dead)
        {
            Debug.Log("[DEBUG] Phím J: Hồi 25 HP");
            Heal(25f);
        }

        // [K]: Tự sát
        if (Input.GetKeyDown(KeyCode.K) && !dead)
        {
            Debug.Log("[DEBUG] Phím K: Tự sát");
            TakeDamage(currentHealth, DamageSource.Generic);
        }

        // [R]: Hồi sinh khi đã chết
        if (Input.GetKeyDown(KeyCode.R) && dead)
        {
            Debug.Log("[DEBUG] Phím R: Hồi sinh");
            Respawn();
        }
    }

    /// <summary>
    /// Nhận gọi từ Easy Weapons (Weapon, Projectile, Explosion) qua SendMessageUpwards("ChangeHealth", amount)
    /// </summary>
    public override void ChangeHealth(float amount)
    {
        if (amount < 0)
        {
            TakeDamage(-amount, DamageSource.Generic);
        }
        else if (amount > 0)
        {
            Heal(amount);
        }
    }

    /// <summary>
    /// Gây sát thương cho người chơi theo nguồn chỉ định
    /// </summary>
    public void TakeDamage(float damage, DamageSource source = DamageSource.Generic)
    {
        if (dead || damage <= 0) return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        Debug.Log($"[PlayerHealth] Người chơi nhận {damage:F1} sát thương từ [{source}]. Còn lại: {currentHealth:F1}/{maxHealth:F1} HP");

        // Gọi sự kiện
        OnDamageTaken?.Invoke(damage, source);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0 && canDie)
        {
            Die();
        }
        else
        {
            PlayHurtSound();
        }
    }

    /// <summary>
    /// Hồi máu cho người chơi
    /// </summary>
    public void Heal(float amount)
    {
        if (dead || amount <= 0) return;

        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;

        Debug.Log($"[PlayerHealth] Hồi {amount:F1} máu. Máu hiện tại: {currentHealth:F1}/{maxHealth:F1} HP");

        OnHeal?.Invoke(amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// Xử lý người chơi tử trận
    /// </summary>
    public override void Die()
    {
        if (dead) return;
        dead = true;

        Debug.Log("[PlayerHealth] Người chơi ĐÃ TỬ TRẬN!");

        // Phát âm thanh khi chết
        PlayDieSound();

        // Vô hiệu hóa điều khiển người chơi
        if (fpCharacter != null) fpCharacter.enabled = false;
        foreach (var rot in mouseRotators)
        {
            if (rot != null) rot.enabled = false;
        }
        if (weaponSystem != null) weaponSystem.enabled = false;

        // Dừng quán tính Rigidbody
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Mở khóa chuột
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Hiệu ứng ngã gục camera
        if (deathCameraCoroutine != null) StopCoroutine(deathCameraCoroutine);
        deathCameraCoroutine = StartCoroutine(AnimateDeathCamera());

        // Kích hoạt sự kiện chết
        OnDeathEvent?.Invoke();
    }

    private IEnumerator AnimateDeathCamera()
    {
        if (cameraTransform == null) yield break;

        Quaternion startRot = cameraTransform.localRotation;
        Quaternion targetRot = startRot * Quaternion.Euler(20f, 0f, 35f); // Nghiêng người khi gục ngã

        Vector3 startPos = cameraTransform.localPosition;
        Vector3 targetPos = startPos + new Vector3(0, -0.4f, 0); // Hạ thấp tầm nhìn

        float elapsed = 0f;
        float duration = 0.8f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / duration);
            cameraTransform.localRotation = Quaternion.Slerp(startRot, targetRot, t);
            cameraTransform.localPosition = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }
    }

    /// <summary>
    /// Hồi sinh người chơi và thiết lập lại toàn bộ trạng thái
    /// </summary>
    public void Respawn()
    {
        if (!dead) return;

        if (deathCameraCoroutine != null)
        {
            StopCoroutine(deathCameraCoroutine);
            deathCameraCoroutine = null;
        }

        // Đặt lại máu
        currentHealth = maxHealth;
        dead = false;

        // Khôi phục vị trí
        Vector3 spawnPos = (respawnPoint != null) ? respawnPoint.position : initialSpawnPosition;
        Quaternion spawnRot = (respawnPoint != null) ? respawnPoint.rotation : initialSpawnRotation;
        transform.position = spawnPos;
        transform.rotation = spawnRot;

        // Khôi phục camera
        if (cameraTransform != null)
        {
            cameraTransform.localPosition = cameraInitialLocalPos;
            cameraTransform.localRotation = cameraInitialLocalRot;
        }

        // Khôi phục các script điều khiển
        if (fpCharacter != null) fpCharacter.enabled = true;
        foreach (var rot in mouseRotators)
        {
            if (rot != null) rot.enabled = true;
        }
        if (weaponSystem != null) weaponSystem.enabled = true;

        // Khóa lại con trỏ chuột
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("[PlayerHealth] Người chơi đã HỒI SINH!");

        OnRespawnEvent?.Invoke();
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    // ==========================================
    // ÂM THANH FMOD / FALLBACK
    // ==========================================

    private void PlayHurtSound()
    {
        if (Time.time < lastHurtSoundTime + hurtSoundCooldown)
            return;
        lastHurtSoundTime = Time.time;

        // 1. Ưu tiên FMOD Event
        if (!hurtSoundFMOD.IsNull)
        {
            FMOD.Studio.EventInstance inst = RuntimeManager.CreateInstance(hurtSoundFMOD);
            inst.set3DAttributes(RuntimeUtils.To3DAttributes(gameObject));
            inst.start();
            inst.release();
        }
        // 2. Dự phòng AudioClip của Unity
        else if (hurtSoundFallback != null && audioSource != null)
        {
            audioSource.PlayOneShot(hurtSoundFallback);
        }
        // 3. Thông báo console nếu chưa gán
        else
        {
            Debug.Log("[PlayerHealth] >> HURT SOUND TRIGGERED (FMOD Placeholder: Hãy kéo Event vào ô hurtSoundFMOD)");
        }
    }

    private void PlayDieSound()
    {
        // 1. Ưu tiên FMOD Event
        if (!dieSoundFMOD.IsNull)
        {
            FMOD.Studio.EventInstance inst = RuntimeManager.CreateInstance(dieSoundFMOD);
            inst.set3DAttributes(RuntimeUtils.To3DAttributes(gameObject));
            inst.start();
            inst.release();
        }
        // 2. Dự phòng AudioClip của Unity
        else if (dieSoundFallback != null && audioSource != null)
        {
            audioSource.PlayOneShot(dieSoundFallback);
        }
        // 3. Thông báo console nếu chưa gán
        else
        {
            Debug.Log("[PlayerHealth] >> DIE SOUND TRIGGERED (FMOD Placeholder: Hãy kéo Event vào ô dieSoundFMOD)");
        }
    }
}
