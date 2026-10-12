using UnityEngine;

public class TVRemote : MonoBehaviour
{
    [Header("Remote Settings")]
    public float interactDistance = 5f;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F)) UseRemote("channel");
        if (Input.GetKeyDown(KeyCode.X)) UseRemote("power");
        if (Input.GetKeyDown(KeyCode.Period)) UseRemote("volume", 0.1f);
        if (Input.GetKeyDown(KeyCode.Comma)) UseRemote("volume", -0.1f);
    }

    private void UseRemote(string action, float amount = 0f)
    {
        // Tạo tia laze từ giữa màn hình bắn thẳng ra trước
        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
        {
            Debug.Log("🎯 TIA ĐÃ BẮN TRÚNG VẬT: " + hit.collider.gameObject.name);

            TVController tv = hit.collider.GetComponent<TVController>();

            if (tv != null)
            {
                Debug.Log("✅ TÌM THẤY SCRIPT TV! Đang thực hiện lệnh: " + action);
                switch (action)
                {
                    case "channel": tv.NextChannel(); break;
                    case "power": tv.TogglePower(); break;
                    case "volume": tv.ChangeVolume(amount); break;
                }
            }
            else
            {
                Debug.LogWarning("⚠️ Bắn trúng " + hit.collider.gameObject.name + ", nhưng vật này KHÔNG GẮN script TVController!");
            }
        }
        else
        {
            Debug.Log("❌ TIA TRƯỢT: Không bắn trúng vật gì cả (Hãy lại gần hơn hoặc nhìn thẳng vào TV).");
        }
    }
}