using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BuildChooseLayoutUI : MonoBehaviour
{
    [SerializeField] private int layoutIndex;
    [SerializeField] private Button playButton;

    private void Awake()
    {
        playButton.onClick.AddListener(PlayStage);
    }
    
    private void OnDestroy()
    {
        playButton.onClick.RemoveListener(PlayStage);
    }

    private void PlayStage()
    {
        GameManager.SetCanEnterBuildMode(true);
        GameManager.ChangeGameStateToTestingBuild();
        
        GameManager.NextLayoutIndex = layoutIndex;
        SaveSystem.NextSaveToLoad = string.Empty;
        
        SceneManager.LoadSceneAsync("Gameplay");
    }
}
