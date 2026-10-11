using UnityEngine;
using HealthSystem;

/// <summary>
/// Cơ chế tính toán sát thương rơi từ trên cao (Fall Damage).
/// Tự động theo dõi độ cao khi người chơi ở trên không và áp dụng sát thương khi tiếp đất.
/// </summary>
public class PlayerFallDamage : MonoBehaviour
{
    [Header("=== CẤU HÌNH SÁT THƯƠNG RƠI ===")]
    [Tooltip("Độ cao rơi tối thiểu bắt đầu tính sát thương (mét). Nhảy thông thường (~1.3m) sẽ không bị trừ máu.")]
    [SerializeField] private float minDamageFallDistance = 3.5f;

    [Tooltip("Lượng sát thương trên mỗi mét rơi vượt quá ngưỡng an toàn.")]
    [SerializeField] private float damagePerMeter = 12.0f;

    [Tooltip("Độ cao rơi gây chết ngay lập tức (mét).")]
    [SerializeField] private float fatalFallDistance = 14.0f;

    [Header("=== THAM CHIẾU ===")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private FirstPersonCharacter fpCharacter;

    private bool wasInAir = false;
    private float highestY = 0f;

    private void Awake()
    {
        if (playerHealth == null) playerHealth = GetComponent<PlayerHealth>();
        if (fpCharacter == null) fpCharacter = GetComponent<FirstPersonCharacter>();
    }

    private void Start()
    {
        highestY = transform.position.y;
        if (playerHealth != null)
        {
            playerHealth.OnRespawnEvent += OnPlayerRespawn;
        }
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnRespawnEvent -= OnPlayerRespawn;
        }
    }

    private void OnPlayerRespawn()
    {
        wasInAir = false;
        highestY = transform.position.y;
    }

    private void FixedUpdate()
    {
        if (playerHealth != null && playerHealth.IsDead) return;

        bool isGrounded = CheckIsGrounded();

        if (!isGrounded)
        {
            // Bắt đầu hoặc đang trong giai đoạn rơi
            if (!wasInAir)
            {
                wasInAir = true;
                highestY = transform.position.y;
            }
            else
            {
                // Cập nhật điểm cao nhất đạt được (ví dụ khi nhảy lên cao rồi mới rơi xuống)
                if (transform.position.y > highestY)
                {
                    highestY = transform.position.y;
                }
            }
        }
        else
        {
            // Vừa tiếp đất từ trên không
            if (wasInAir)
            {
                wasInAir = false;
                float fallDistance = highestY - transform.position.y;

                if (fallDistance > minDamageFallDistance)
                {
                    float excessDistance = fallDistance - minDamageFallDistance;
                    float damage = excessDistance * damagePerMeter;

                    // Nếu rơi từ độ cao tử thần
                    if (fallDistance >= fatalFallDistance && playerHealth != null)
                    {
                        damage = playerHealth.maxHealth;
                    }

                    if (playerHealth != null)
                    {
                        playerHealth.TakeDamage(damage, DamageSource.Fall);
                        Debug.Log($"[FallDamage] Tiếp đất từ độ cao {fallDistance:F1}m! Nhận {damage:F1} sát thương rơi.");
                    }
                }

                highestY = transform.position.y;
            }
            else
            {
                highestY = transform.position.y;
            }
        }
    }

    private bool CheckIsGrounded()
    {
        if (fpCharacter != null)
        {
            return fpCharacter.grounded;
        }

        // Dự phòng bằng Raycast nếu không có script FirstPersonCharacter
        return Physics.Raycast(transform.position, Vector3.down, 1.2f);
    }
}
