using UnityEngine;
using UnityEngine.SceneManagement;

public class ChallengeMenu : MonoBehaviour
{
    [SerializeField] private string firstSaveName = "challenge1";
    [SerializeField] private string secondSaveName = "challenge2";
    [SerializeField] private string thirdSaveName = "challenge3";
    
    public void Return()
    {
        SceneManager.LoadSceneAsync("Play_Menu");
    }

    public async void PlayStage1()
    {
        GameManager.SetCanEnterBuildMode(false);
        GameManager.ChangeGameStateToExploring();
        
        GameManager.NextLayoutIndex = 0;
        
        // Try to load a random Firebase level
        string firebaseSaveId = await SaveSystem.LoadRandomFirebaseLevel("firebase_challenge1");
        
        if (firebaseSaveId != null)
        {
            // Successfully loaded from Firebase
            SaveSystem.NextSaveToLoad = firebaseSaveId;
        }
        else
        {
            // Fallback to default challenge level
            SaveSystem.NextSaveToLoad = firstSaveName;
        }
        
        SceneManager.LoadSceneAsync("Gameplay");
    }

    public void PlayStage2()
    {
        GameManager.SetCanEnterBuildMode(false);
        GameManager.ChangeGameStateToExploring();

        GameManager.NextLayoutIndex = 1;
        SaveSystem.NextSaveToLoad = secondSaveName;
        
        SceneManager.LoadSceneAsync("Gameplay");
    }

    public void PlayStage3()
    {
        GameManager.SetCanEnterBuildMode(false);
        GameManager.ChangeGameStateToExploring();
        
        GameManager.NextLayoutIndex = 2;
        SaveSystem.NextSaveToLoad = thirdSaveName;

        SceneManager.LoadSceneAsync("Gameplay");
    }
}
