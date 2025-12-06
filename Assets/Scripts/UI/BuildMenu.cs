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

    private void Awake()
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
        
        // TODO: add pagination logic
        var levels = await FirebaseManager.Instance.GetMyLevels(30);
        foreach (var (levelId, saveJson) in levels)
        {
            var card = Instantiate(levelCardPrefab, scrollViewContent);
            var levelData = SaveSystem.ParseLevelDataFromJson(saveJson);
            var playerName = await SaveSystem.GetPlayerName(levelData.PlayerId);
            
            card.SetLevelData(levelId, levelData.PlayerId, levelData.LevelName, playerName, levelData.TotalGold, 
                levelData.TotalDeaths, levelData.LayoutIndex);
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

    // on unity event
    public void Return()
    {
        SceneManager.LoadSceneAsync("Play_Menu");
    }
}
