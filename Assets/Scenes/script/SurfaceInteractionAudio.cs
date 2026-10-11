using UnityEngine;

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

    private string currentSurfaceTag = "Untagged";

    private void Update()
    {
        // Chỉ dò đường liên tục nếu dùng cho nhân vật (Giúp tối ưu FPS cho vỏ đạn)
        if (useRaycast)
        {
            DetectSurface();
        }
    }

    private void DetectSurface()
    {
        Ray ray = new Ray(transform.position, Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance))
        {
            currentSurfaceTag = hit.collider.tag;
        }
        else
        {
            currentSurfaceTag = "Untagged";
        }
    }

    // Tự động kích hoạt khi vật thể vật lý (vỏ đạn) đập vào vật khác
    private void OnCollisionEnter(Collision collision)
    {
        currentSurfaceTag = collision.gameObject.tag;
        PlayInteraction();
    }

    public void PlayInteraction()
    {
        if (audioSource == null) return;

        SurfaceAudio data = GetSurfaceAudio(currentSurfaceTag);

        // Bỏ qua nếu không có dữ liệu
        if (data == null || data.clips == null || data.clips.Length == 0) return;

        AudioClip clip = data.clips[Random.Range(0, data.clips.Length)];
        if (clip == null) return;

        // Random cao độ và phát tiếng
        audioSource.pitch = Random.Range(data.minPitch, data.maxPitch);
        audioSource.PlayOneShot(clip, data.volume);
    }

    private SurfaceAudio GetSurfaceAudio(string tag)
    {
        // 1. Ưu tiên tìm chính xác Tag va chạm (VD: Ground)
        foreach (SurfaceAudio surface in surfaces)
        {
            if (surface.targetTag == tag) return surface;
        }

        // 2. Dự phòng: Nếu không có Tag đó, tìm mục có chữ "Any"
        foreach (SurfaceAudio surface in surfaces)
        {
            if (surface.targetTag == "Any") return surface;
        }

        // 3. Dự phòng cuối: Nếu không có Any, tìm "Untagged"
        foreach (SurfaceAudio surface in surfaces)
        {
            if (surface.targetTag == "Untagged") return surface;
        }

        return null;
    }
}