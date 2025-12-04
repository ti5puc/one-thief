using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BuildMenu : MonoBehaviour
{
    [Header("Levels Panel")]
    [SerializeField] private GameObject levelsPanel;
    [SerializeField] private BuildLevelCardUI levelCardPrefab;
    [SerializeField] private RectTransform scrollViewContent;
    [SerializeField] private Button createLevelButton;
    [SerializeField] private Button returnButton;
    
    [Header("Create Panel")]
    [SerializeField] private GameObject createPanel;
    [SerializeField] private Button returnCreateButton;

    private async void Awake()
    {
        createLevelButton.onClick.AddListener(ShowCreatePanel);
        returnButton.onClick.AddListener(ReturnToMenu);
        returnCreateButton.onClick.AddListener(ShowLevelsPanel);
        
        ShowLevelsPanel();
    }
    
    private void OnDestroy()
    {
        createLevelButton.onClick.RemoveListener(ShowCreatePanel);
        returnButton.onClick.RemoveListener(ReturnToMenu);
        returnCreateButton.onClick.RemoveListener(ShowLevelsPanel);
    }

    private void ReturnToMenu()
    {
        SceneManager.LoadSceneAsync("Play_Menu");
    }

    private async Task LoadPlayerLevels()
    {
        if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsAuthenticated)
        {
            Debug.LogError("Cannot load player levels: Not authenticated with Firebase.");
            return;
        }
        
        var levels = await FirebaseManager.Instance.GetMyLevels(20); // Adjust maxResults as needed
        foreach (var (levelId, saveJson) in levels)
        {
            var card = Instantiate(levelCardPrefab, scrollViewContent);
            // card.SetLevelData(levelId, saveJson);
        }
    }
    
    private async void ShowLevelsPanel()
    {
        foreach (Transform child in scrollViewContent)
            Destroy(child.gameObject);
        
        await LoadPlayerLevels();
        
        levelsPanel.SetActive(true);
        createPanel.SetActive(false);
    }
    
    private void ShowCreatePanel()
    {
        levelsPanel.SetActive(false);
        createPanel.SetActive(true);
    }

    public void Return()
    {
        SceneManager.LoadSceneAsync("Play_Menu");
    }

    public void PlayStage1()
    {
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
