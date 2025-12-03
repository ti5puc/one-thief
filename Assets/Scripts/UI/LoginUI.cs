using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoginUI : MonoBehaviour
{
    [Header("First Login Panel")]
    [SerializeField] private GameObject firstLoginPanel;
    
    [Space(5)]
    [SerializeField] private TMP_InputField firstLoginPlayerNameInput;
    [SerializeField] private TMP_Text firstLoginPlayerIdText;
    
    [Space(5)]
    [SerializeField] private Button firstLoginEnterGameButton;
    
    [Header("Login Panel")]
    [SerializeField] private GameObject loginPanel;

    [Space(5)]
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text playerIdText;
    
    [Space(5)]
    [SerializeField] private Button enterGameButton;
    [SerializeField] private Button resetPlayerSaveButton;

    private void Awake()
    {
        firstLoginPanel.SetActive(false);
        loginPanel.SetActive(false);
        
        firstLoginEnterGameButton.onClick.AddListener(OnFirstLoginEnterGame);
        enterGameButton.onClick.AddListener(EnterGame);
        resetPlayerSaveButton.onClick.AddListener(OnResetPlayerSave);
        
        LoadingBarUI.OnLoadingCompleteEvent += ShowLoginPanel;
    }

    private void OnDestroy()
    {
        firstLoginEnterGameButton.onClick.RemoveListener(OnFirstLoginEnterGame);
        enterGameButton.onClick.RemoveListener(EnterGame);
        resetPlayerSaveButton.onClick.RemoveListener(OnResetPlayerSave);
        
        LoadingBarUI.OnLoadingCompleteEvent -= ShowLoginPanel;
    }

    private void OnFirstLoginEnterGame()
    {
        // Get the player name from input field
        var inputName = firstLoginPlayerNameInput.text;
        string playerName = string.IsNullOrEmpty(inputName) ? "Guest" : inputName;
        
        // Validate player name
        if (string.IsNullOrWhiteSpace(playerName))
        {
            Debug.LogWarning("[LoginUI] Player name cannot be empty!");
            // You can show an error message to the user here
            return;
        }
        
        // Trim and limit length
        playerName = playerName.Trim();
        if (playerName.Length > 20)
        {
            playerName = playerName.Substring(0, 20);
        }
        
        // Set the player name in FirebaseManager
        FirebaseManager.SetPlayerName(playerName);
        
        // Create initial inventory data with player name
        InventoryData initialData = new InventoryData
        {
            Gold = 0,
            PlayerName = playerName
        };
        
        // Save the inventory (this will save both locally and to Firebase)
        SaveSystem.SaveInventory(initialData);
        
        Debug.Log($"[LoginUI] First login complete for player: {playerName}");
        
        // Enter the game
        EnterGame();
    }

    private void EnterGame()
    {
        SceneManager.LoadSceneAsync("Main_Menu");
    }

    private async void OnResetPlayerSave()
    {
        // Delete Firebase player data
        bool firebaseDeleted = await FirebaseManager.DeletePlayerData();
        
        if (firebaseDeleted)
        {
            Debug.Log("[LoginUI] Firebase player data deleted successfully");
        }
        else
        {
            Debug.LogWarning("[LoginUI] Failed to delete Firebase player data or not authenticated");
        }
        
        // Clear all local saves including inventory
        SaveSystem.ClearAllSaves(alsoClearInventory: true);
        
        Debug.Log("[LoginUI] All player saves have been reset!");
        
        // Reload the scene to restart the login process
        SceneManager.LoadSceneAsync("Startup");
    }

    private void ShowLoginPanel()
    {
        if (FirebaseManager.Instance.IsFirstLogin)
        {
            firstLoginPanel.SetActive(true);
            loginPanel.SetActive(false);
            
            firstLoginPlayerIdText.text = $"Player ID: {FirebaseManager.Instance.UserId}";
            
            // Focus on the input field so user can start typing immediately
            if (firstLoginPlayerNameInput != null)
            {
                firstLoginPlayerNameInput.Select();
                firstLoginPlayerNameInput.ActivateInputField();
            }
        }
        else
        {
            firstLoginPanel.SetActive(false);
            loginPanel.SetActive(true);
            
            playerNameText.text = $"{FirebaseManager.Instance.PlayerName}";
            playerIdText.text = $"Player ID: {FirebaseManager.Instance.UserId}";
        }
    }
}
