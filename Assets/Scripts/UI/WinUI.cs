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
    [SerializeField] private TMP_Text okButtonText;
    
    [Space(10)]
    [SerializeField] private Transform cancelGroup;
    [SerializeField] private Button cancelButton;
    
    [Header("Submit")]
    [SerializeField] private Transform submitGroup;
    [SerializeField] private Button submitButton;
    [SerializeField] private Button denyButton;
    [SerializeField] private TMP_InputField levelName;

    private bool allChestsCollected;

    private void Awake()
    {
        submitButton.onClick.AddListener(OnSubmitButtonClicked);
        denyButton.onClick.AddListener(OnDenyButtonClicked);
        cancelButton.onClick.AddListener(OnCancelButtonClicked);
        okButton.onClick.AddListener(OnWinExploring);
        levelName.onValueChanged.AddListener(EnableButton);

        ExitPortal.OnAnyPortalActivated += TryShow;
        TreasureCollectCounter.OnAllTreasuresCollected += OnAllTreasuresCollected;
        
        Hide();
    }
    
    private void OnDestroy()
    {
        submitButton.onClick.RemoveListener(OnSubmitButtonClicked);
        denyButton.onClick.RemoveListener(OnDenyButtonClicked);
        cancelButton.onClick.RemoveListener(OnCancelButtonClicked);
        okButton.onClick.RemoveListener(OnWinExploring);
        levelName.onValueChanged.RemoveListener(EnableButton);
            
        ExitPortal.OnAnyPortalActivated -= TryShow;
        TreasureCollectCounter.OnAllTreasuresCollected -= OnAllTreasuresCollected;
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

    private void OnAllTreasuresCollected()
    {
        allChestsCollected = true;
    }

    private void ShowSubmit()
    {
        if (allChestsCollected)
        {
            messageText.text = "Fase testada. Deseja enviar?";
            submitGroup.gameObject.SetActive(true);
            okGroup.gameObject.SetActive(false);
            cancelGroup.gameObject.SetActive(false);
            EnableButton(levelName.text);
        }
        else
        {
            messageText.text = "Colete todos os baús antes de enviar a fase.";
            okButtonText.text = "Continuar testando";
            submitGroup.gameObject.SetActive(false);
            okGroup.gameObject.SetActive(true);
            cancelGroup.gameObject.SetActive(false);
        }
        
        gameObject.SetActive(true);
    }

    private void ShowCollectAll()
    {
        messageText.text = $"Deseja sair?<br>Ouro resgatado: {PlayerInventory.Instance.GoldCache}";
        okButtonText.text = "Sim";
        submitGroup.gameObject.SetActive(false);
        okGroup.gameObject.SetActive(true);
        cancelGroup.gameObject.SetActive(true);
        
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
        submitButton.interactable = !string.IsNullOrWhiteSpace(lvlName) && allChestsCollected;
    }

    private void OnSubmitButtonClicked()
    {
        if (string.IsNullOrWhiteSpace(levelName.text))
        {
            Debug.LogError("Level needs a name.");
            return;
        }
        
        bool hasLevelId = SaveSystem.LocalSaveHasLevelId(SaveSystem.NextSaveToLoad);
        if (hasLevelId)
        {
            var levelId = SaveSystem.GetLocalSaveLevelId(SaveSystem.NextSaveToLoad);
            SaveSystem.EditLevelOnFirebase(levelId, levelName.text, PlayerInventory.Instance.GoldCache, GameManager.NextLayoutIndex);
        }
        else
            SaveSystem.SubmitLevelToFirebase(levelName.text, PlayerInventory.Instance.GoldCache, GameManager.NextLayoutIndex);
        
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
        // Se está no modo submit mas sem todos os baús, apenas fecha o UI e deixa continuar jogando
        if (GameManager.IsTestingToSubmit && !allChestsCollected)
        {
            Hide();
            GameManager.HideCursor();
            return;
        }
        
        OnGetGold?.Invoke();
        Hide();
        
        GameManager.Resume();
        
        GameManager.ShowCursor();
        
        SceneManager.LoadSceneAsync("Challenge_Menu");
    }
    
    private void OnCancelButtonClicked()
    {
        Hide();
        GameManager.HideCursor();
    }
}
