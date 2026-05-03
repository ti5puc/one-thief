using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ChallengeLevelCardUI : MonoBehaviour
{
    public static event Action OnNotEnoughGold;

    [SerializeField] private TMP_Text levelNameText;
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text totalGoldText;
    [SerializeField] private TMP_Text taxGoldText;
    [SerializeField] private TMP_Text dificultyText;

    [Space(10)]
    [SerializeField] private Image layoutImage;
    [SerializeField] private List<Sprite> layoutSprites;

    [Space(10)]
    [SerializeField] private Button playButton;

    private string levelId;
    private string playerId;
    private int layoutIndex;
    private int entryTax;

    private void Awake()
    {
        playButton.onClick.AddListener(OnPlayButtonClicked);
    }
    
    private void OnDestroy()
    {
        playButton.onClick.RemoveListener(OnPlayButtonClicked);
    }

    public void SetLevelData(string levelId, string playerId, string levelName, string playerName, int totalGold, int totalDeaths, int layoutIndex, int entryTax = 0, float totalWins = 0f)
    {
        this.levelId = levelId;
        this.playerId = playerId;
        this.layoutIndex = layoutIndex;
        this.entryTax = entryTax;
        
        levelNameText.text = levelName;
        playerNameText.text = $"Criado por: {playerName}";
        totalGoldText.text = $"Para saquear: ${totalGold}";
        taxGoldText.text = $"Taxa: ${entryTax}";
        dificultyText.text = $"Dificuldade: {SaveSystem.GetDifficultyLabel(totalDeaths, totalWins)}";
        
        layoutImage.sprite = layoutSprites[Mathf.Clamp(layoutIndex, 0, layoutSprites.Count - 1)];
    }
    
    private async void OnPlayButtonClicked()
    {
        try
        {
            if (entryTax > 0)
            {
                if (PlayerInventory.Instance == null || PlayerInventory.Instance.CurrentGold < entryTax)
                {
                    OnNotEnoughGold?.Invoke();
                    return;
                }
                PlayerInventory.Instance.DeductGold(entryTax);
            }

            Debug.Log($"[ChallengeLevelCardUI] Loading level {levelId} for play...");
            
            GameManager.SetCanEnterBuildMode(false);
            GameManager.IsTestingToSubmit = false;
            GameManager.ChangeGameStateToExploring();
            
            string firebaseSaveId = await SaveSystem.LoadFirebaseLevel(levelId);
            if (firebaseSaveId == null)
            {
                Debug.LogError($"[ChallengeLevelCardUI] Failed to load level '{levelId}' for playing");
                return;
            }
            
            GameManager.NextLayoutIndex = layoutIndex;
            SaveSystem.NextSaveToLoad = firebaseSaveId;
            
            Debug.Log($"[ChallengeLevelCardUI] Starting level {levelId} with layout {layoutIndex}");
            SceneManager.LoadSceneAsync("Gameplay");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ChallengeLevelCardUI] Error loading level for play: {ex.Message}\n{ex.StackTrace}");
        }
    }
}
