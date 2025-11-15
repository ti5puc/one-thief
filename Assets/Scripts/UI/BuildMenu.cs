using UnityEngine;
using UnityEngine.SceneManagement;

public class BuildMenu : MonoBehaviour
{
    public void Return()
    {
        SceneManager.LoadSceneAsync(1);
    }

    public void PlayStage1()
    {
        GameManager.SetCanEnterBuildMode(true);
        GameManager.ChangeGameStateToTestingBuild();
        
        GameManager.NextLayoutIndex = 0;
        
        SceneManager.LoadSceneAsync(6);
    }

    public void PlayStage2()
    {
        GameManager.SetCanEnterBuildMode(true);
        GameManager.ChangeGameStateToTestingBuild();

        GameManager.NextLayoutIndex = 1;
        
        SceneManager.LoadSceneAsync(6);
    }

    public void PlayStage3()
    {
        GameManager.SetCanEnterBuildMode(true);
        GameManager.ChangeGameStateToTestingBuild();

        GameManager.NextLayoutIndex = 2;
        
        SceneManager.LoadSceneAsync(6);
    }
}
