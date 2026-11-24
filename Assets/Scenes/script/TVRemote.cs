using UnityEngine;
using FMODUnity; // Nhớ phải có dòng này để dùng FMOD

public class TVRemote : MonoBehaviour
{
    [Header("Remote Settings")]
    public float interactDistance = 10f; // Tầm bắn laze

    [Header("FMOD Audio")]
    [Tooltip("Kéo Event âm thanh bấm nút điều khiển vào đây")]
    public EventReference clickSoundEvent; // Biến chứa âm thanh bấm nút

    void Update()
    {
        // Các phím bấm giữ nguyên
        if (Input.GetKeyDown(KeyCode.F)) UseRemote("channel");
        if (Input.GetKeyDown(KeyCode.X)) UseRemote("power");
        if (Input.GetKeyDown(KeyCode.Period)) UseRemote("volume", 0.1f);
        if (Input.GetKeyDown(KeyCode.Comma)) UseRemote("volume", -0.1f);
    }

    private void UseRemote(string action, float amount = 0f)
    {
        // 1. PlayOneShot: Phát âm thanh bấm nút 1 lần tại vị trí chiếc điều khiển trong không gian 3D
        if (!clickSoundEvent.IsNull)
        {
            RuntimeManager.PlayOneShot(clickSoundEvent, transform.position);
        }

        // 2. Bắn tia Raycast từ tâm Camera để điều khiển TV hoặc Cửa cuốn
        if (Camera.main == null) return;
        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        Debug.DrawRay(ray.origin, ray.direction * interactDistance, Color.red, 2f);

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
        {
            TVController tv = hit.collider.GetComponent<TVController>();
            RollingDoor door = hit.collider.GetComponent<RollingDoor>();

            if (tv != null)
            {
                switch (action)
                {
                    case "channel": tv.NextChannel(); break;
                    case "power": tv.TogglePower(); break;
                    case "volume": tv.ChangeVolume(amount); break;
                }
            }
            else if (door != null && action == "power")
            {
                Debug.Log("🚪 Đã bắn trúng Cửa cuốn! Đang cuộn cửa...");
                door.ToggleDoor();
            }
        }
    }
}