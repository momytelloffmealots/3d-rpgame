using System.Collections;
using UnityEngine;
using HealthSystem;

/// <summary>
/// Trụ súng bắn đạn thử nghiệm (Shooting Turret / Target Shooter).
/// Tự động xoay và bắn đạn về phía người chơi theo chu kỳ để người chơi thử nghiệm nhận sát thương từ đạn.
/// </summary>
public class ShootingTurret : MonoBehaviour
{
    [Header("=== CẤU HÌNH BẮN ĐẠN ===")]
    [Tooltip("Bật/tắt chế độ tự động bắn")]
    public bool autoFire = true;

    [Tooltip("Thời gian giữa mỗi phát bắn (giây)")]
    public float fireRate = 2.0f;

    [Tooltip("Lượng sát thương của mỗi viên đạn")]
    public float bulletDamage = 15.0f;

    [Tooltip("Tầm bắn tối đa (mét)")]
    public float attackRange = 25.0f;

    [Tooltip("Phím tắt để bật / tắt trụ súng nhanh")]
    public KeyCode toggleKey = KeyCode.T;

    [Header("=== HIỆU ỨNG HÌNH ẢNH / ÂM THANH ===")]
    [Tooltip("Điểm nòng súng nơi viên đạn bắn ra")]
    public Transform firePoint;

    [Tooltip("Prefab tia lửa trúng đích (Sparks)")]
    public GameObject hitSparkPrefab;

    public AudioClip shootSoundClip;

    private PlayerHealth playerTarget;
    private float nextFireTime = 0f;
    private AudioSource audioSource;
    private LineRenderer lineRenderer;

    private void Start()
    {
        playerTarget = Object.FindAnyObjectByType<PlayerHealth>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f; // Âm thanh 3D
        }

        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.startWidth = 0.05f;
            lineRenderer.endWidth = 0.02f;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = new Color(1f, 0.9f, 0.3f, 0.8f);
            lineRenderer.endColor = new Color(1f, 0.3f, 0.1f, 0.3f);
            lineRenderer.positionCount = 2;
            lineRenderer.enabled = false;
        }

        if (firePoint == null)
        {
            firePoint = transform;
        }
    }

    private void Update()
    {
        // Phím T bật/tắt trụ súng
        if (Input.GetKeyDown(toggleKey))
        {
            autoFire = !autoFire;
            Debug.Log($"[ShootingTurret] Trạng thái trụ súng: {(autoFire ? "BẬT (Đang bắn)" : "TẮT (Tạm ngưng)")}");
        }

        if (!autoFire || playerTarget == null || playerTarget.IsDead) return;

        Vector3 targetPos = playerTarget.transform.position + Vector3.up * 0.8f; // Nhắm vào thân người chơi
        float distance = Vector3.Distance(transform.position, targetPos);

        if (distance <= attackRange)
        {
            // Quay nhẹ về phía người chơi
            Vector3 lookDirection = (targetPos - transform.position).normalized;
            if (lookDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDirection), Time.deltaTime * 5f);
            }

            if (Time.time >= nextFireTime)
            {
                nextFireTime = Time.time + fireRate;
                ShootBullet(targetPos);
            }
        }
    }

    private void ShootBullet(Vector3 targetPos)
    {
        Vector3 origin = (firePoint != null) ? firePoint.position : transform.position;
        Vector3 dir = (targetPos - origin).normalized;

        RaycastHit hit;
        Vector3 hitPoint = origin + dir * attackRange;

        if (Physics.Raycast(origin, dir, out hit, attackRange))
        {
            hitPoint = hit.point;

            // Kiểm tra trúng người chơi
            PlayerHealth hitHealth = hit.collider.GetComponentInParent<PlayerHealth>();
            if (hitHealth != null)
            {
                hitHealth.TakeDamage(bulletDamage, DamageSource.Bullet);
                Debug.Log($"[ShootingTurret] Đạn bắn trúng người chơi! Gây {bulletDamage} sát thương.");
            }
            else
            {
                hit.collider.gameObject.SendMessageUpwards("ChangeHealth", -bulletDamage, SendMessageOptions.DontRequireReceiver);
            }

            // Sinh hiệu ứng tia lửa
            if (hitSparkPrefab != null)
            {
                Instantiate(hitSparkPrefab, hitPoint, Quaternion.LookRotation(hit.normal));
            }
        }

        // Vẽ vệt đạn tracer
        StartCoroutine(ShowTracer(origin, hitPoint));

        // Phát âm thanh bắn
        if (shootSoundClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(shootSoundClip);
        }
    }

    private IEnumerator ShowTracer(Vector3 start, Vector3 end)
    {
        if (lineRenderer == null) yield break;

        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
        lineRenderer.enabled = true;

        yield return new WaitForSeconds(0.08f);

        lineRenderer.enabled = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
