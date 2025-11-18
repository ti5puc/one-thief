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
        SceneManager.LoadSceneAsync(1);
    }

    public void Options()
    {
        SceneManager.LoadSceneAsync(5);
    }

    public void Leaderboard()
    {
        SceneManager.LoadSceneAsync(4);
    }
}
