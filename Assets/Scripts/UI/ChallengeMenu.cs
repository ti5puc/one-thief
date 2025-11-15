using UnityEngine;
using UnityEngine.SceneManagement;

public class ChallengeMenu : MonoBehaviour
{
    [SerializeField] private string firstSaveName = "challenge1";
    [SerializeField] private string secondSaveName = "challenge2";
    [SerializeField] private string thirdSaveName = "challenge3";
    
    public void Return()
    {
        SceneManager.LoadSceneAsync(1);
    }

    public void PlayStage1()
    {
        GameManager.SetCanEnterBuildMode(false);
        GameManager.ChangeGameStateToExploring();
        
        GameManager.NextLayoutIndex = 0;
        SaveSystem.NextSaveToLoad = firstSaveName;
        
        SceneManager.LoadSceneAsync(6);
    }

    public void PlayStage2()
    {
        GameManager.SetCanEnterBuildMode(false);
        GameManager.ChangeGameStateToExploring();

        GameManager.NextLayoutIndex = 1;
        SaveSystem.NextSaveToLoad = secondSaveName;
        
        SceneManager.LoadSceneAsync(6);
    }

    public void PlayStage3()
    {
        GameManager.SetCanEnterBuildMode(false);
        GameManager.ChangeGameStateToExploring();
        
        GameManager.NextLayoutIndex = 2;
        SaveSystem.NextSaveToLoad = thirdSaveName;

        SceneManager.LoadSceneAsync(6);
    }
}
