using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayMenu : MonoBehaviour
{
    public void Return()
    {
        SceneManager.LoadSceneAsync(0);
    }

    public void Challenge()
    {
        SceneManager.LoadSceneAsync(2);
    }

    public void Build()
    {
        SceneManager.LoadSceneAsync(6);
    }
}
