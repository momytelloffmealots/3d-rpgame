using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuControl : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "main";

    // Gắn vào On Click của nút Play
    public void PlayGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    // Gắn vào On Click của nút Quit
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // thoát chế độ Play trong Editor
#else
        Application.Quit(); // thoát game khi đã build
#endif
    }
}