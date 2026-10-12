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
        // 1. PHÁT ÂM THANH BẤM NÚT TRƯỚC TIÊN
        if (!clickSoundEvent.IsNull)
        {
            // Phát tiếng bấm nút ngay tại vị trí của chiếc điều khiển (transform.position)
            RuntimeManager.PlayOneShot(clickSoundEvent, transform.position);
        }

        // 2. BẮN TIA LAZE ĐỂ KIỂM TRA TƯƠNG TÁC
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