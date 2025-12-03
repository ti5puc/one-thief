using UnityEngine;
using UnityEngine.SceneManagement;

public class OptionsMenu : MonoBehaviour
{
    public void Return()
    {
        SceneManager.LoadSceneAsync("Main_Menu");
    }
}
