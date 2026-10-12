using UnityEngine;
using System.Collections.Generic;

public class GameMenuController : MonoBehaviour
{
    [Header("UI Menu")]
    public GameObject menuCanvas;

    [Header("Player & Camera Controls")]
    [Tooltip("Danh sách script cần vô hiệu hóa khi mở Menu (FirstPersonCharacter, MouseRotator...)")]
    public List<MonoBehaviour> scriptsToDisable = new List<MonoBehaviour>();

    [Header("Pause Settings")]
    public bool pauseGameTime = false;

    public static bool IsMenuOpen { get; private set; } = false;

    private void Start()
    {
        SetMenuState(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleMenu();
        }
    }

    public void ToggleMenu()
    {
        SetMenuState(!IsMenuOpen);
    }

    public void CloseMenu()
    {
        if (IsMenuOpen) SetMenuState(false);
    }

    private void SetMenuState(bool open)
    {
        IsMenuOpen = open;

        if (menuCanvas != null)
        {
            menuCanvas.SetActive(IsMenuOpen);
        }

        // Đồng bộ trạng thái Chuột & Thời gian
        Cursor.lockState = IsMenuOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = IsMenuOpen;

        if (pauseGameTime)
        {
            Time.timeScale = IsMenuOpen ? 0f : 1f;
        }

        // Bật/Tắt các script điều khiển
        foreach (var script in scriptsToDisable)
        {
            if (script != null) script.enabled = !IsMenuOpen;
        }

        // Khi mở Menu -> Gọi làm tươi giao diện Slider/Toggle âm thanh
        if (IsMenuOpen && MenuAudioManager.Instance != null)
        {
            MenuAudioManager.Instance.RefreshUIAndAudio();
        }
    }
}