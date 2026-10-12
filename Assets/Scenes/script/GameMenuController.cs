using UnityEngine;

public class GameMenuController : MonoBehaviour
{
    [Header("UI Menu")]
    public GameObject menuCanvas;

    [Header("Player Control")]
    [Tooltip("Kéo Script điều khiển Nhân vật / Camera vào đây để tắt khi mở Menu")]
    public MonoBehaviour playerControllerScript;

    private bool isMenuOpen = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleMenu();
        }
    }

    public void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;

        if (menuCanvas != null)
        {
            menuCanvas.SetActive(isMenuOpen);
        }

        if (isMenuOpen)
        {
            // MỞ MENU:
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 0f;

            // Tắt script điều khiển nhân vật để không nhận click chuột
            if (playerControllerScript != null)
                playerControllerScript.enabled = false;
        }
        else
        {
            // ĐÓNG MENU:
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Time.timeScale = 1f;

            // Bật lại script điều khiển nhân vật
            if (playerControllerScript != null)
                playerControllerScript.enabled = true;
        }
    }
}