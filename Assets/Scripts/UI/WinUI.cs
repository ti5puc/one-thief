using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WinUI : MonoBehaviour
{
    public static event Action OnShow;
    public static event Action OnHide;
    public static event Action OnGetGold;
    
    [SerializeField] private TMP_Text messageText;
    
    [Header("Collected all")]
    [SerializeField] private Transform okGroup;
    [SerializeField] private Button okButton;
    
    [Header("Submit")]
    [SerializeField] private Transform submitGroup;
    [SerializeField] private Button submitButton;
    [SerializeField] private Button denyButton;
    [SerializeField] private TMP_InputField levelName;

    private void Awake()
    {
        submitButton.onClick.AddListener(OnSubmitButtonClicked);
        denyButton.onClick.AddListener(OnDenyButtonClicked);
        okButton.onClick.AddListener(OnWinExploring);
        levelName.onValueChanged.AddListener(EnableButton);

        TreasureCollectCounter.OnAllTreasuresCollected += TryShow;
        
        Hide();
    }
    
    private void OnDestroy()
    {
        submitButton.onClick.RemoveListener(OnSubmitButtonClicked);
        denyButton.onClick.RemoveListener(OnDenyButtonClicked);
        okButton.onClick.RemoveListener(OnWinExploring);
        levelName.onValueChanged.RemoveListener(EnableButton);
            
        TreasureCollectCounter.OnAllTreasuresCollected -= TryShow;
    }

    private void TryShow()
    {
        if (GameManager.CurrentGameState == GameState.Exploring && GameManager.IsTestingToSubmit == false)
            ShowCollectAll();
        else
            ShowSubmit();
        
        GameManager.ShowCursor();
        GameManager.Pause();
        
        OnShow?.Invoke();
    }

    private void ShowSubmit()
    {
        messageText.text = "Fase testada. Deseja enviar?";
        submitGroup.gameObject.SetActive(true);
        okGroup.gameObject.SetActive(false);
        
        EnableButton(levelName.text);
        
        gameObject.SetActive(true);
    }

    private void ShowCollectAll()
    {
        messageText.text = "Todos os tesouros foram coletados!";
        submitGroup.gameObject.SetActive(false);
        okGroup.gameObject.SetActive(true);
        
        gameObject.SetActive(true);
    }

    private void Hide()
    {
        gameObject.SetActive(false);
        GameManager.Resume();
            
        OnHide?.Invoke();
    }

    private void EnableButton(string lvlName)
    {
        submitButton.interactable = !string.IsNullOrWhiteSpace(lvlName);
    }

    private void OnSubmitButtonClicked()
    {
        if (string.IsNullOrWhiteSpace(levelName.text))
        {
            Debug.LogError("Level needs a name.");
            return;
        }
        
        SaveSystem.SubmitLevelToFirebase(levelName.text);
        
        PlayerInventory.Instance.SpendGoldToRemove();
        
        GameManager.Resume();
        GameManager.IsTestingToSubmit = false;
        GameManager.SetCanEnterBuildMode(true);

        GameManager.ChangeGameStateToTestingBuild();
        SaveSystem.NextSaveToLoad = "current_build";
        
        PlayerInventory.Instance.ClearGoldCache();
        
        Hide();
        
        SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnDenyButtonClicked()
    {
        GameManager.Resume();
        GameManager.IsTestingToSubmit = false;
        GameManager.SetCanEnterBuildMode(true);

        GameManager.ChangeGameStateToTestingBuild();
        SaveSystem.NextSaveToLoad = "current_build";
        
        PlayerInventory.Instance.ClearGoldCache();
        
        Hide();
        
        SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnWinExploring()
    {
        OnGetGold?.Invoke();
        Hide();
        
        GameManager.Resume();
        
        GameManager.ShowCursor();
        
        SceneManager.LoadSceneAsync("Challenge_Menu");
    }
}
