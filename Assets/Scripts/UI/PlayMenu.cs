using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayMenu : MonoBehaviour
{
    [SerializeField] private GameObject playPanel;

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
