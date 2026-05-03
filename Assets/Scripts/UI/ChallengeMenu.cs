using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ChallengeMenu : MonoBehaviour
{
    [Header("Levels Panel")]
    [SerializeField] private GameObject levelsPanel;
    [SerializeField] private ChallengeLevelCardUI levelCardPrefab;
    [SerializeField] private RectTransform scrollViewContent;
    [SerializeField] private Button returnButton;
    
    [Header("No Gold Panel")]
    [SerializeField] private GameObject noGoldPanel;
    [SerializeField] private Button noGoldReturnButton;

    private async void Awake()
    {
        returnButton.onClick.AddListener(ReturnToMenu);
        noGoldReturnButton.onClick.AddListener(HideNoGoldMessage);
        noGoldPanel.SetActive(false);

        ChallengeLevelCardUI.OnNotEnoughGold += ShowNoGoldMessage;
        
        foreach (Transform child in scrollViewContent)
            Destroy(child.gameObject);
        
        await LoadPlayerLevels();
    }
    
    private void OnDestroy()
    {
        returnButton.onClick.RemoveListener(ReturnToMenu);
        noGoldReturnButton.onClick.RemoveListener(HideNoGoldMessage);

        ChallengeLevelCardUI.OnNotEnoughGold -= ShowNoGoldMessage;
    }

    private void ReturnToMenu()
    {
        SceneManager.LoadSceneAsync("Play_Menu");
    }

    private void ShowNoGoldMessage()
    {
        levelsPanel.SetActive(false);
        noGoldPanel.SetActive(true);
    }

    private void HideNoGoldMessage()
    {
        levelsPanel.SetActive(true);
        noGoldPanel.SetActive(false);
    }

    private async Task LoadPlayerLevels()
    {
        if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsAuthenticated)
        {
            Debug.LogError("Cannot load player levels: Not authenticated with Firebase.");
            return;
        }
        
        try
        {
            Debug.Log("[ChallengeMenu] Loading levels from Firebase...");
            
            // TODO: add pagination logic
            var levels = await FirebaseManager.Instance.GetAllLevels(50);
            
            Debug.Log($"[ChallengeMenu] Loaded {levels.Count} levels from Firebase");
            
            foreach (var (levelId, saveJson) in levels)
            {
                try
                {
                    var card = Instantiate(levelCardPrefab, scrollViewContent);
                    var levelData = SaveSystem.ParseLevelDataFromJson(saveJson);
                    
                    if (levelData == null)
                    {
                        Debug.LogError($"[ChallengeMenu] Failed to parse level data for {levelId}");
                        Destroy(card.gameObject);
                        continue;
                    }
                    
                    var playerName = await SaveSystem.GetPlayerName(levelData.PlayerId);
                    
                    card.SetLevelData(levelId, levelData.PlayerId, levelData.LevelName, playerName, levelData.TotalGold, 
                        levelData.TotalDeaths, levelData.LayoutIndex, levelData.EntryTax, levelData.TotalWins);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[ChallengeMenu] Error loading level card {levelId}: {ex.Message}");
                }
            }
            
            Debug.Log("[ChallengeMenu] Finished loading all level cards");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ChallengeMenu] Error loading player levels: {ex.Message}\n{ex.StackTrace}");
        }
    }

    // on unity event
    public void Return()
    {
        SceneManager.LoadSceneAsync("Play_Menu");
    }
}
