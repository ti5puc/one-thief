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
    [SerializeField] private TMP_Text submitMessageText;
    [SerializeField] private Transform submitGroup;
    [SerializeField] private Button submitButton;
    [SerializeField] private Button denyButton;
    [SerializeField] private TMP_InputField levelName;
    [SerializeField] private TMP_InputField levelGold;

    private bool allChestsCollected;

    private void Awake()
    {
        submitButton.onClick.AddListener(OnSubmitButtonClicked);
        denyButton.onClick.AddListener(OnDenyButtonClicked);
        cancelButton.onClick.AddListener(OnCancelButtonClicked);
        okButton.onClick.AddListener(OnWinExploring);
        levelName.onValueChanged.AddListener(EnableButton);
        levelGold.onValueChanged.AddListener(OnGoldInputChanged);

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
        levelGold.onValueChanged.RemoveListener(OnGoldInputChanged);
            
        ExitPortal.OnAnyPortalActivated -= TryShow;
        TreasureCollectCounter.OnAllTreasuresCollected -= OnAllTreasuresCollected;
    }

    private void OnGoldInputChanged(string _) => EnableButton(levelName.text);

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
            messageText.gameObject.SetActive(true);
            submitMessageText.gameObject.SetActive(false);

            messageText.text = "Fase testada. Deseja enviar?";

            submitGroup.gameObject.SetActive(true);
            okGroup.gameObject.SetActive(false);
            cancelGroup.gameObject.SetActive(false);
            levelGold.text = string.Empty;
            EnableButton(levelName.text);
        }
        else
        {
            messageText.gameObject.SetActive(true);
            submitMessageText.gameObject.SetActive(false);

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
        messageText.gameObject.SetActive(false);
        submitMessageText.gameObject.SetActive(true);

        submitMessageText.text = $"Deseja sair?<br>Ouro resgatado: ${PlayerInventory.Instance.GoldCache}";

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
        bool nameValid = !string.IsNullOrWhiteSpace(lvlName);
        bool goldValid = int.TryParse(levelGold.text, out int goldValue)
                         && goldValue > 0
                         && PlayerInventory.Instance != null
                         && goldValue <= PlayerInventory.Instance.CurrentGold;
        submitButton.interactable = nameValid && goldValid && allChestsCollected;
    }

    private void OnSubmitButtonClicked()
    {
        if (string.IsNullOrWhiteSpace(levelName.text))
        {
            Debug.LogError("Level needs a name.");
            return;
        }

        if (!int.TryParse(levelGold.text, out int submittedGold)
            || submittedGold <= 0
            || submittedGold > PlayerInventory.Instance.CurrentGold)
        {
            Debug.LogError("Invalid gold amount.");
            return;
        }
        
        bool hasLevelId = SaveSystem.LocalSaveHasLevelId(SaveSystem.NextSaveToLoad);
        if (hasLevelId)
        {
            var levelId = SaveSystem.GetLocalSaveLevelId(SaveSystem.NextSaveToLoad);
            SaveSystem.EditLevelOnFirebase(levelId, levelName.text, submittedGold, GameManager.NextLayoutIndex);
        }
        else
            SaveSystem.SubmitLevelToFirebase(levelName.text, submittedGold, GameManager.NextLayoutIndex);
        
        PlayerInventory.Instance.DeductGold(submittedGold);
        
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
