using UnityEngine;
using FMODUnity;
using FMOD.Studio;

/// <summary>
/// Điều khiển chuyển động lắc đầu khi bước (Head Bob) và phát âm thanh bước chân 3D theo bề mặt vật liệu.
/// Cơ chế FMOD Surface Footsteps (3 bước):
///   1. Bắn tia Raycast xuống dưới chân để đọc Tag bề mặt (Ground, Grass, Wood, water).
///   2. Ánh xạ Tag sang giá trị số (0..3) tương ứng với Parameter "Surface" trong FMOD Studio.
///   3. Phát EventInstance 3D tại vị trí nhân vật rồi giải phóng RAM ngay sau khi kích hoạt.
/// </summary>
public class FirstPersonHeadBob : MonoBehaviour
{
    [SerializeField] Transform head;

    [SerializeField] float headBobFrequency = 1.5f;
    [SerializeField] float headBobHeight = 0.3f;
    [SerializeField] float headBobSwayAngle = 0.5f;
    [SerializeField] float headBobSideMovement = 0.05f;
    [SerializeField] float bobHeightSpeedMultiplier = 0.3f;
    [SerializeField] float bobStrideSpeedLengthen = 0.3f;

    [SerializeField] float jumpLandMove = 3;
    [SerializeField] float jumpLandTilt = 60;

    [SerializeField] AudioClip jumpSound;
    [SerializeField] AudioClip landSound;

    FirstPersonCharacter character;
    AudioSource cachedAudioSource;
    Rigidbody cachedRigidbody;
    Vector3 originalLocalPos;

    float headBobCycle = 0;
    float headBobFade = 0;

    float springPos = 0;
    float springVelocity = 0;
    float springElastic = 1.1f;
    float springDampen = 0.8f;
    float springVelocityThreshold = 0.05f;
    float springPositionThreshold = 0.05f;

    Vector3 prevPosition;
    Vector3 prevVelocity = Vector3.zero;
    bool prevGrounded = true;

    [Header("=== FMOD FOOTSTEPS ===")]
    public EventReference footstepsound;
    public float TimeInterval = 0.3f;
    public float TimeElapse = 0f;

    void Start()
    {
        if (head != null) originalLocalPos = head.localPosition;
        character = GetComponent<FirstPersonCharacter>();
        cachedRigidbody = GetComponent<Rigidbody>();

        cachedAudioSource = GetComponent<AudioSource>();
        if (cachedAudioSource == null)
        {
            cachedAudioSource = gameObject.AddComponent<AudioSource>();
        }

        if (cachedRigidbody != null)
        {
            prevPosition = cachedRigidbody.position;
        }
    }

    void FixedUpdate()
    {
        if (cachedRigidbody == null || character == null) return;

        Vector3 velocity = (cachedRigidbody.position - prevPosition) / Time.deltaTime;
        Vector3 velocityChange = velocity - prevVelocity;
        prevPosition = cachedRigidbody.position;
        prevVelocity = velocity;

        // Mô phỏng lò xo theo phương thẳng đứng khi nhảy / tiếp đất
        springVelocity -= velocityChange.y;
        springVelocity -= springPos * springElastic;
        springVelocity *= springDampen;
        springPos += springVelocity * Time.deltaTime;
        springPos = Mathf.Clamp(springPos, -.3f, .3f);

        if (Mathf.Abs(springVelocity) < springVelocityThreshold && Mathf.Abs(springPos) < springPositionThreshold)
        {
            springVelocity = 0;
            springPos = 0;
        }

        float flatVelocity = new Vector3(velocity.x, 0, velocity.z).magnitude;
        float strideLengthen = 1 + (flatVelocity * bobStrideSpeedLengthen);
        headBobCycle += (flatVelocity / strideLengthen) * (Time.deltaTime / headBobFrequency);

        float bobFactor = Mathf.Sin(headBobCycle * Mathf.PI * 2);
        float bobSwayFactor = Mathf.Sin(headBobCycle * Mathf.PI * 2 + Mathf.PI * .5f);
        bobFactor = 1 - (bobFactor * .5f + 1);
        bobFactor *= bobFactor;

        headBobFade = Mathf.Lerp(headBobFade, flatVelocity < 0.1f ? 0f : 1f, Time.deltaTime);

        if (head != null)
        {
            float speedHeightFactor = 1 + (flatVelocity * bobHeightSpeedMultiplier);
            float xPos = -headBobSideMovement * bobSwayFactor;
            float yPos = springPos * jumpLandMove + bobFactor * headBobHeight * headBobFade * speedHeightFactor;
            float xTilt = -springPos * jumpLandTilt;
            float zTilt = bobSwayFactor * headBobSwayAngle * headBobFade;
            head.localPosition = originalLocalPos + new Vector3(xPos, yPos, 0);
            head.localRotation = Quaternion.Euler(xTilt, 0, zTilt);
        }

        // Xử lý âm thanh Nhảy / Tiếp đất / Bước chân
        if (character.grounded)
        {
            if (!prevGrounded)
            {
                if (landSound != null && cachedAudioSource != null)
                {
                    cachedAudioSource.clip = landSound;
                    cachedAudioSource.Play();
                }
            }
            else if (flatVelocity > 0.1f)
            {
                TimeElapse -= Time.deltaTime;
                if (TimeElapse <= 0f)
                {
                    PlayFootstepWithSurface();
                    TimeElapse = TimeInterval;
                }
            }
            else
            {
                TimeElapse = 0f;
            }
            prevGrounded = true;
        }
        else
        {
            if (prevGrounded && jumpSound != null && cachedAudioSource != null)
            {
                cachedAudioSource.clip = jumpSound;
                cachedAudioSource.Play();
            }
            prevGrounded = false;
        }
    }

    /// <summary>
    /// Phát âm thanh bước chân FMOD tự động đổi chất liệu theo bề mặt dưới chân nhân vật.
    /// </summary>
    void PlayFootstepWithSurface()
    {
        if (footstepsound.IsNull) return;

        // Bước 1: Bắn tia Raycast thẳng xuống để kiểm tra bề mặt đang đứng
        float surfaceValue = 0f; // 0 = Carpet / Ground mặc định trong FMOD
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 2.0f))
        {
            // Bước 2: Quy đổi Tag Unity -> giá trị Parameter "Surface" của FMOD Studio
            switch (hit.collider.tag)
            {
                case "Grass":
                    surfaceValue = 1f;
                    break;
                case "Wood":
                    surfaceValue = 2f;
                    break;
                case "water":
                    surfaceValue = 3f;
                    break;
                case "Ground":
                default:
                    surfaceValue = 0f;
                    break;
            }
        }

        // Bước 3: Tạo EventInstance 3D tại vị trí nhân vật, truyền tham số Surface rồi phát
        EventInstance footstepInst = RuntimeManager.CreateInstance(footstepsound);
        footstepInst.set3DAttributes(RuntimeUtils.To3DAttributes(gameObject));
        footstepInst.setParameterByName("Surface", surfaceValue);
        footstepInst.start();
        footstepInst.release();
    }
}
