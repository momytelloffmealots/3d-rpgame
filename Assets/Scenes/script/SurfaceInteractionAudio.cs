using UnityEngine;

/// <summary>
/// Phát âm thanh va chạm 3D theo chất liệu bề mặt (dùng cho vỏ đạn rơi / vết đạn).
/// Cơ chế:
///   1. Đọc Tag của vật thể vừa va chạm (ưu tiên Tag khớp chính xác -> "Any" -> "Untagged").
///   2. Chọn ngẫu nhiên 1 AudioClip trong danh sách của bề mặt đó và random nhẹ Pitch để tránh lặp âm đơn điệu.
///   3. Có cooldown ngắn để tránh phát dồn nhiều lần khi vỏ đạn nảy liên tiếp trên sàn.
/// </summary>
public class SurfaceInteractionAudio : MonoBehaviour
{
    [System.Serializable]
    public class SurfaceAudio
    {
        [Tooltip("Nhập tên Tag (VD: Ground, Wood). Nhập 'Any' để phát khi chạm bất kỳ bề mặt nào.")]
        public string targetTag = "Any";

        public AudioClip[] clips;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(-1.2f, 1.2f)] public float minPitch = 0.95f;
        [Range(0, 1.2f)] public float maxPitch = 1.05f;
    }

    [Header("Detection (Bật tick này nếu dùng cho Nhân vật)")]
    public bool useRaycast = false;
    public float rayDistance = 2f;

    [Header("Audio")]
    public AudioSource audioSource;
    public SurfaceAudio[] surfaces;
    [Tooltip("Thời gian nghỉ tối thiểu giữa 2 lần phát khi vật thể nảy liên tiếp")]
    public float collisionCooldown = 0.05f;

    private string currentSurfaceTag = "Untagged";
    private float lastPlayTime = -1f;

    private void Update()
    {
        // Chỉ dò đường liên tục nếu dùng cho nhân vật (giúp tối ưu FPS cho vỏ đạn)
        if (useRaycast)
        {
            DetectSurface();
        }
    }

    private void DetectSurface()
    {
        Ray ray = new Ray(transform.position, Vector3.down);
        currentSurfaceTag = Physics.Raycast(ray, out RaycastHit hit, rayDistance)
            ? hit.collider.tag
            : "Untagged";
    }

    // Tự động kích hoạt khi vật thể vật lý (ví dụ: vỏ đạn) đập vào bề mặt khác
    private void OnCollisionEnter(Collision collision)
    {
        if (Time.time < lastPlayTime + collisionCooldown) return;
        lastPlayTime = Time.time;

        currentSurfaceTag = collision.gameObject.tag;
        PlayInteraction();
    }

    public void PlayInteraction()
    {
        if (audioSource == null) return;

        SurfaceAudio data = GetSurfaceAudio(currentSurfaceTag);
        if (data == null || data.clips == null || data.clips.Length == 0) return;

        AudioClip clip = data.clips[Random.Range(0, data.clips.Length)];
        if (clip == null) return;

        // Random nhẹ cao độ (Pitch) giúp tiếng va chạm tự nhiên hơn
        audioSource.pitch = Random.Range(data.minPitch, data.maxPitch);
        audioSource.PlayOneShot(clip, data.volume);
    }

    private SurfaceAudio GetSurfaceAudio(string tag)
    {
        // 1. Ưu tiên tìm đúng Tag va chạm (VD: Ground, Wood)
        foreach (SurfaceAudio surface in surfaces)
        {
            if (surface.targetTag == tag) return surface;
        }

        // 2. Dự phòng: mục "Any"
        foreach (SurfaceAudio surface in surfaces)
        {
            if (surface.targetTag == "Any") return surface;
        }

        // 3. Dự phòng cuối: mục "Untagged"
        foreach (SurfaceAudio surface in surfaces)
        {
            if (surface.targetTag == "Untagged") return surface;
        }

        return null;
    }
}
