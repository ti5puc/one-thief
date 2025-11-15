using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    private void Awake()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void PlayGame()
    {
        SceneManager.LoadSceneAsync(1);
    }

    public void Options()
    {
        SceneManager.LoadSceneAsync(8);
    }

    public void Leaderboard()
    {
        SceneManager.LoadSceneAsync(7);
    }
}
