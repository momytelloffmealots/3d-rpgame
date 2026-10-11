using System.Collections;
using UnityEngine;
using HealthSystem;

/// <summary>
/// Thùng thuốc nổ / Bom thử nghiệm trong màn chơi.
/// Có thể phát nổ khi bị bắn (nhận sát thương từ súng đạn) hoặc khi người chơi lại gần nhấn phím [E].
/// Khi nổ sẽ tạo hiệu ứng nổ và gây sát thương diện rộng cho người chơi.
/// </summary>
public class ExplosiveBarrel : MonoBehaviour
{
    [Header("=== THÔNG SỐ BOM / THÙNG NỔ ===")]
    [Tooltip("Lượng máu của thùng (bắn đạn trúng sẽ trừ máu, về 0 sẽ nổ)")]
    public float health = 25f;

    [Tooltip("Sát thương nổ gây ra tại tâm vụ nổ")]
    public float explosionDamage = 75f;

    [Tooltip("Bán kính sát thương nổ (mét)")]
    public float explosionRadius = 8f;

    [Tooltip("Lực nổ tác động lên vật thể vật lý Rigidbody")]
    public float explosionForce = 15f;

    [Tooltip("Prefab hiệu ứng nổ (ví dụ: Explosion 1 của Easy Weapons)")]
    public GameObject explosionPrefab;

    [Header("=== TƯƠNG TÁC THỬ NGHIỆM ===")]
    [Tooltip("Cho phép kích nổ bằng phím E khi người chơi đứng gần")]
    public bool allowKeyDetonation = true;
    public KeyCode detonateKey = KeyCode.E;
    public float interactionDistance = 4f;

    private bool exploded = false;
    private Transform playerTransform;

    private void Start()
    {
        // Tự động tìm Player
        PlayerHealth player = Object.FindAnyObjectByType<PlayerHealth>();
        if (player != null)
        {
            playerTransform = player.transform;
        }

        // Tự gán màu đỏ cho thùng để dễ nhận biết nếu chưa có vật liệu riêng
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr != null && mr.material != null)
        {
            // Tạo bản sao material và nhuộm đỏ nhạt để báo hiệu vật phát nổ
            Material mat = new Material(mr.material);
            mat.color = new Color(0.9f, 0.25f, 0.2f, 1f);
            mr.material = mat;
        }
    }

    private void Update()
    {
        if (exploded || !allowKeyDetonation || playerTransform == null) return;

        // Kiểm tra khoảng cách người chơi để bấm E kích nổ
        float dist = Vector3.Distance(transform.position, playerTransform.position);
        if (dist <= interactionDistance && Input.GetKeyDown(detonateKey))
        {
            Debug.Log($"[ExplosiveBarrel] Người chơi bấm [{detonateKey}] kích nổ thùng bom!");
            Explode();
        }
    }

    /// <summary>
    /// Nhận sát thương khi bị đạn bắn trúng (từ Easy Weapons Weapon/Projectile)
    /// </summary>
    public void ChangeHealth(float amount)
    {
        if (exploded) return;

        health += amount;
        Debug.Log($"[ExplosiveBarrel] Thùng trúng đạn! Máu còn: {Mathf.Max(0, health):F1}");

        if (health <= 0)
        {
            Explode();
        }
    }

    /// <summary>
    /// Kích nổ thùng bom
    /// </summary>
    public void Explode()
    {
        if (exploded) return;
        exploded = true;

        Debug.Log($"[ExplosiveBarrel] BÙM! Bom nổ tại {transform.position}");

        // 1. Sinh Prefab hiệu ứng nổ nếu có
        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }
        else
        {
            // 2. Dự phòng: tự tính sát thương diện rộng và lực nổ nếu không có Prefab
            Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
            foreach (Collider col in colliders)
            {
                float dist = Vector3.Distance(transform.position, col.transform.position);
                float factor = 1f - Mathf.Clamp01(dist / explosionRadius);
                float finalDamage = explosionDamage * factor;

                // Gây sát thương nếu là người chơi
                PlayerHealth pHealth = col.GetComponentInParent<PlayerHealth>();
                if (pHealth != null)
                {
                    pHealth.TakeDamage(finalDamage, DamageSource.Explosion);
                }
                else
                {
                    col.gameObject.SendMessageUpwards("ChangeHealth", -finalDamage, SendMessageOptions.DontRequireReceiver);
                }

                // Tác động lực đẩy nổ
                Rigidbody rb = col.attachedRigidbody;
                if (rb != null)
                {
                    rb.AddExplosionForce(explosionForce, transform.position, explosionRadius, 1f, ForceMode.Impulse);
                }
            }
        }

        // Tắt hiển thị và va chạm của thùng
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        foreach (var r in renderers) r.enabled = false;

        Collider[] cols = GetComponentsInChildren<Collider>();
        foreach (var c in cols) c.enabled = false;

        // Xóa đối tượng sau 1 giây
        Destroy(gameObject, 1.5f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
