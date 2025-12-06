using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ChallengeMenu : MonoBehaviour
{
    [Header("Levels Panel")]
    [SerializeField] private ChallengeLevelCardUI levelCardPrefab;
    [SerializeField] private RectTransform scrollViewContent;
    [SerializeField] private Button returnButton;

    private async void Awake()
    {
        returnButton.onClick.AddListener(ReturnToMenu);
        
        foreach (Transform child in scrollViewContent)
            Destroy(child.gameObject);
        
        await LoadPlayerLevels();
    }
    
    private void OnDestroy()
    {
        returnButton.onClick.RemoveListener(ReturnToMenu);
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
        var levels = await FirebaseManager.Instance.GetAllLevels(50);
        foreach (var (levelId, saveJson) in levels)
        {
            var card = Instantiate(levelCardPrefab, scrollViewContent);
            var levelData = SaveSystem.ParseLevelDataFromJson(saveJson);
            var playerName = await SaveSystem.GetPlayerName(levelData.PlayerId);
            
            card.SetLevelData(levelId, levelData.PlayerId, levelData.LevelName, playerName, levelData.TotalGold, 
                levelData.TotalDeaths, levelData.LayoutIndex);
        }
    }

    // on unity event
    public void Return()
    {
        SceneManager.LoadSceneAsync("Play_Menu");
    }
}
