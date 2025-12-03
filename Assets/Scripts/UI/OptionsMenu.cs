using UnityEngine;
using UnityEngine.SceneManagement;

public class OptionsMenu : MonoBehaviour
{
    public void Return()
    {
        SceneManager.LoadSceneAsync("Main_Menu");
    }

    public async void ResetPlayerData()
    {
        await FirebaseManager.DeletePlayerData();
        
        SaveSystem.ClearAllSaves(alsoClearInventory: true);
        
        SceneManager.LoadSceneAsync("Startup");
    }
}
