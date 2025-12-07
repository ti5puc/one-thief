using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayMenu : MonoBehaviour
{
    public void Return()
    {
        SceneManager.LoadSceneAsync("Main_Menu");
    }

    public void Challenge()
    {
        SceneManager.LoadSceneAsync("Challenge_Menu");
    }

    public void Build()
    {
        SceneManager.LoadSceneAsync("Build_Menu");
    }
}
