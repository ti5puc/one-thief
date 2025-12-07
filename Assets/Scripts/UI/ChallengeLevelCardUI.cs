using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ChallengeLevelCardUI : MonoBehaviour
{
    [SerializeField] private TMP_Text levelNameText;
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text totalGoldText;
    [SerializeField] private TMP_Text totalDeathsText;

    [Space(10)]
    [SerializeField] private Image layoutImage;
    [SerializeField] private List<Sprite> layoutSprites;

    [Space(10)]
    [SerializeField] private Button playButton;

    private string levelId;
    private string playerId;
    private int layoutIndex;

    private void Awake()
    {
        playButton.onClick.AddListener(OnPlayButtonClicked);
    }
    
    private void OnDestroy()
    {
        playButton.onClick.RemoveListener(OnPlayButtonClicked);
    }

    public void SetLevelData(string levelId, string playerId, string levelName, string playerName, int totalGold, int totalDeaths, int layoutIndex)
    {
        this.levelId = levelId;
        this.playerId = playerId;
        this.layoutIndex = layoutIndex;
        
        levelNameText.text = levelName;
        playerNameText.text = $"Criado por: {playerName}";
        totalGoldText.text = $"Para saquear: ${totalGold}";
        totalDeathsText.text =$"Mortes: {totalDeaths}";
        
        layoutImage.sprite = layoutSprites[Mathf.Clamp(layoutIndex, 0, layoutSprites.Count - 1)];
    }
    
    private async void OnPlayButtonClicked()
    {
        try
        {
            Debug.Log($"[ChallengeLevelCardUI] Loading level {levelId} for play...");
            
            GameManager.SetCanEnterBuildMode(false);
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
