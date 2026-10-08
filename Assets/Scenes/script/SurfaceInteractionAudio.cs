using UnityEngine;

public class SurfaceInteractionAudio : MonoBehaviour
{
    public enum SurfaceType
    {
        Default,
        Grass,
        Stone,
        Wood,
        Metal,
        Water,
        Sand,
        Snow
    }

    [System.Serializable]
    public class SurfaceAudio
    {
        public SurfaceType type;
        public AudioClip[] clips;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.8f, 1.2f)] public float minPitch = 0.95f;
        [Range(0.8f, 1.2f)] public float maxPitch = 1.05f;
    }

    [Header("Detection")]
    [SerializeField] private LayerMask surfaceMask;
    [SerializeField] private float rayDistance = 2f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private SurfaceAudio[] surfaces;

    private SurfaceType currentSurface = SurfaceType.Default;

    private void Update()
    {
        DetectSurface();
    }

    private void DetectSurface()
    {
        Ray ray = new Ray(transform.position, Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, surfaceMask))
        {
            currentSurface = GetSurfaceType(hit.collider.gameObject);
        }
        else
        {
            currentSurface = SurfaceType.Default;
        }
    }

    private SurfaceType GetSurfaceType(GameObject obj)
    {
        // Ví dụ đơn giản: kiểm tra layer
        if (obj.layer == LayerMask.NameToLayer("Grass"))
            return SurfaceType.Grass;

        if (obj.layer == LayerMask.NameToLayer("Stone"))
            return SurfaceType.Stone;

        if (obj.layer == LayerMask.NameToLayer("Wood"))
            return SurfaceType.Wood;

        if (obj.layer == LayerMask.NameToLayer("Metal"))
            return SurfaceType.Metal;

        if (obj.layer == LayerMask.NameToLayer("Water"))
            return SurfaceType.Water;

        if (obj.layer == LayerMask.NameToLayer("Sand"))
            return SurfaceType.Sand;

        if (obj.layer == LayerMask.NameToLayer("Snow"))
            return SurfaceType.Snow;

        return SurfaceType.Default;
    }

    public void PlayInteraction()
    {
        SurfaceAudio data = GetSurfaceAudio(currentSurface);

        if (data == null || data.clips == null || data.clips.Length == 0)
            return;

        AudioClip clip = data.clips[Random.Range(0, data.clips.Length)];

        audioSource.pitch = Random.Range(
            data.minPitch,
            data.maxPitch
        );

        audioSource.PlayOneShot(
            clip,
            data.volume
        );
    }

    private SurfaceAudio GetSurfaceAudio(SurfaceType type)
    {
        foreach (SurfaceAudio surface in surfaces)
        {
            if (surface.type == type)
                return surface;
        }

        return null;
    }

    public SurfaceType GetCurrentSurface()
    {
        return currentSurface;
    }
}