using UnityEngine;
using UnityEngine.SceneManagement;

public class LeaderboardMenu : MonoBehaviour
{
    public void Return()
    {
        SceneManager.LoadSceneAsync("Main_Menu");
    }
}
