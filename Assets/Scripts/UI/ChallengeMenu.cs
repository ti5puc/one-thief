using UnityEngine;
using UnityEngine.SceneManagement;

public class ChallengeMenu : MonoBehaviour
{
    public void Return()
    {
        SceneManager.LoadSceneAsync(1);
    }

    public void PlayStage1()
    {
        GameManager.SetCanEnterBuildMode(false);
        GameManager.ChangeGameStateToExploring();
        
        SceneManager.LoadSceneAsync(3);
    }

    public void PlayStage2()
    {
        GameManager.SetCanEnterBuildMode(false);
        GameManager.ChangeGameStateToExploring();

        SceneManager.LoadSceneAsync(4);
    }

    public void PlayStage3()
    {
        GameManager.SetCanEnterBuildMode(false);
        GameManager.ChangeGameStateToExploring();

        SceneManager.LoadSceneAsync(5);
    }
}
