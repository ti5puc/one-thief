using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    private void Awake()
    {
        GameManager.ShowCursor();
    }

    public void PlayGame()
    {
        SceneManager.LoadSceneAsync("Play_Menu");
    }

    public void Options()
    {
        SceneManager.LoadSceneAsync("Options_Menu");
    }

    public void Leaderboard()
    {
        SceneManager.LoadSceneAsync("Leaderboard_Screen");
    }
    
    public void QuitGame()
    {
        Application.Quit();
    }
}
