using UnityEngine;
using FMODUnity;
using System.Collections;

public class RollingDoor : MonoBehaviour
{
    [Header("FMOD Audio")]
    [Tooltip("Kéo Event âm thanh kéo cửa cuốn vào đây")]
    public EventReference doorSoundEvent;

    [Header("Cài đặt Cửa")]
    [Tooltip("Khoảng cách cửa sẽ trượt lên cao bao nhiêu mét")]
    public float moveDistance = 2f;
    public float animationSpeed = 5f;  // Tốc độ cửa cuộn lên/xuống

    private bool isOpen = false;
    private Vector3 closedPosition;
    private Vector3 openPosition;
    private Coroutine doorCoroutine;

    void Start()
    {
        closedPosition = transform.position;
        openPosition = new Vector3(closedPosition.x, closedPosition.y + moveDistance, closedPosition.z);
    }

    public void ToggleDoor()
    {
        isOpen = !isOpen;

        // Phát âm thanh
        if (!doorSoundEvent.IsNull)
        {
            RuntimeManager.PlayOneShotAttached(doorSoundEvent, gameObject);
        }

        // Chạy hiệu ứng trượt
        if (doorCoroutine != null) StopCoroutine(doorCoroutine);

        if (isOpen)
            doorCoroutine = StartCoroutine(AnimateDoor(openPosition));
        else
            doorCoroutine = StartCoroutine(AnimateDoor(closedPosition));
    }

    private IEnumerator AnimateDoor(Vector3 destinationPosition)
    {
        // Vòng lặp an toàn tuyệt đối
        while (true)
        {
            // BẢO VỆ 1: Nếu cửa bị xóa giữa chừng -> Thoát ngay lập tức
            if (this == null || gameObject == null) yield break;

            // BẢO VỆ 2: Nếu đã đến đích -> Thoát vòng lặp
            if (Vector3.Distance(transform.position, destinationPosition) <= 0.01f) break;

            // Trượt cửa
            transform.position = Vector3.Lerp(transform.position, destinationPosition, Time.deltaTime * animationSpeed);
            yield return null;
        }

        // Chốt tọa độ đích (Chỉ chạy khi cửa vẫn còn tồn tại)
        if (this != null && gameObject != null)
        {
            transform.position = destinationPosition;
        }
    }

    // BẢO VỆ 3: Khi tắt game hoặc chuyển scene, tự động bóp cò hủy mọi Coroutine đang chạy ngầm
    void OnDestroy()
    {
        StopAllCoroutines();
    }
}