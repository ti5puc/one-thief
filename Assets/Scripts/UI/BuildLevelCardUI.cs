using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BuildLevelCardUI : MonoBehaviour
{
    [SerializeField] private TMP_Text levelNameText;
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text totalGoldText;
    [SerializeField] private TMP_Text totalDeathsText;

    [Space(10)]
    [SerializeField] private Image layoutImage;
    [SerializeField] private List<Sprite> layoutSprites;

    [Space(10)]
    [SerializeField] private Button editButton;
    [SerializeField] private Button deleteButton;

    private string levelId;
    private string playerId;
    private int layoutIndex;

    private void Awake()
    {
        editButton.onClick.AddListener(OnEditButtonClicked);
        deleteButton.onClick.AddListener(OnDeleteButtonClicked);
    }
    
    private void OnDestroy()
    {
        editButton.onClick.RemoveListener(OnEditButtonClicked);
        deleteButton.onClick.RemoveListener(OnDeleteButtonClicked);
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
    
    private async void OnEditButtonClicked()
    {
        GameManager.SetCanEnterBuildMode(true);
        GameManager.ChangeGameStateToTestingBuild();
        
        string firebaseSaveId = await SaveSystem.LoadFirebaseLevel(levelId);
        if (firebaseSaveId == null)
        {
            Debug.LogError($"[BuildLevelCardUI] Failed to load level '{levelId}' for editing");
            return;
        }
        
        GameManager.NextLayoutIndex = layoutIndex;
        SaveSystem.NextSaveToLoad = firebaseSaveId;
        
        SceneManager.LoadSceneAsync("Gameplay");
    }
    
    private async void OnDeleteButtonClicked()
    {
        // Check if user is authenticated
        if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsAuthenticated)
        {
            Debug.LogError("[BuildLevelCardUI] Cannot delete level - not authenticated");
            return;
        }

        // Check if the logged user is the owner of the level
        if (FirebaseManager.Instance.UserId != playerId)
        {
            Debug.LogWarning("[BuildLevelCardUI] Cannot delete level - you are not the owner");
            return;
        }

        // Delete the level from Firebase
        bool success = await FirebaseManager.Instance.DeleteDocument("levels", levelId);
        
        if (success)
        {
            Debug.Log($"[BuildLevelCardUI] Level '{levelId}' deleted successfully");
            
            // Destroy the card UI
            Destroy(gameObject);
        }
        else
        {
            Debug.LogError($"[BuildLevelCardUI] Failed to delete level '{levelId}'");
        }
    }
}
