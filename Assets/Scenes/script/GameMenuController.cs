using UnityEngine;
using System.Collections.Generic;

public class GameMenuController : MonoBehaviour
{
    [Header("UI Menu")]
    public GameObject menuCanvas;

    [Header("Player & Camera Controls")]
    [Tooltip("Kéo toàn bộ script điều khiển (ví dụ: FirstPersonCharacter, MouseRotator, WeaponSystem) vào đây để tắt khi mở Menu")]
    public List<MonoBehaviour> scriptsToDisable = new List<MonoBehaviour>();

    [Header("Pause Settings")]
    [Tooltip("Tích chọn nếu muốn dừng thời gian game (Time.timeScale = 0). Bỏ tích nếu muốn menu không làm đứng game.")]
    public bool pauseGameTime = false;

    private bool isMenuOpen = false;

    void Start()
    {
        if (menuCanvas != null && menuCanvas.activeSelf)
        {
            menuCanvas.SetActive(false);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleMenu();
        }

        // BẢO VỆ CHUỘT: Khi menu đang mở, luôn giữ chuột mở tự do.
        // Ngăn chặn các script khác (như FirstPersonCharacter bắt MouseButtonUp) cướp lại và giấu chuột đi.
        if (isMenuOpen)
        {
            if (Cursor.lockState != CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.None;
            }
            if (!Cursor.visible)
            {
                Cursor.visible = true;
            }
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

            if (pauseGameTime)
            {
                Time.timeScale = 0f;
            }

            // Tắt các script điều khiển
            foreach (var script in scriptsToDisable)
            {
                if (script != null)
                    script.enabled = false;
            }
        }
        else
        {
            // ĐÓNG MENU:
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (pauseGameTime)
            {
                Time.timeScale = 1f;
            }

            // Bật lại các script
            foreach (var script in scriptsToDisable)
            {
                if (script != null)
                    script.enabled = true;
            }
        }
    }

    public void CloseMenu()
    {
        if (isMenuOpen)
        {
            ToggleMenu();
        }
    }
}