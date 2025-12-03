using UnityEngine;
using UnityEngine.SceneManagement;

public class BuildMenu : MonoBehaviour
{
    public void Return()
    {
        SceneManager.LoadSceneAsync("Play_Menu");
    }

    public void PlayStage1()
    {
        GameManager.SetCanEnterBuildMode(true);
        GameManager.ChangeGameStateToTestingBuild();
        
        GameManager.NextLayoutIndex = 0;
        SaveSystem.NextSaveToLoad = string.Empty;
        
        SceneManager.LoadSceneAsync("Gameplay");
    }

    public void PlayStage2()
    {
        GameManager.SetCanEnterBuildMode(true);
        GameManager.ChangeGameStateToTestingBuild();

        GameManager.NextLayoutIndex = 1;
        SaveSystem.NextSaveToLoad = string.Empty;
        
        SceneManager.LoadSceneAsync("Gameplay");
    }

    public void PlayStage3()
    {
        GameManager.SetCanEnterBuildMode(true);
        GameManager.ChangeGameStateToTestingBuild();

        GameManager.NextLayoutIndex = 2;
        SaveSystem.NextSaveToLoad = string.Empty;
        
        SceneManager.LoadSceneAsync("Gameplay");
    }
}
