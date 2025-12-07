using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuUI : MonoBehaviour
{
    public static event Action<bool> OnPauseMenuToggled;
    public static event Action OnTest;
    
    [SerializeField] private Button testButton;
    [SerializeField] private TMP_Text testButtonText;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button menuButton;
    
    [Space(5)]
    [SerializeField] private TMP_Text treasureHintText;
    
    [Space(10)]
    [SerializeField] private InputActionReference pauseMenuAction;

    private bool isShowing;
    private bool shouldBackToBuild;
    private bool isWinUIOpen;
    
    private void Awake()
    {
        pauseMenuAction.action.Enable();
        pauseMenuAction.action.performed += TogglePauseMenu;
        
        testButton.onClick.AddListener(Test);
        resetButton.onClick.AddListener(ResetScene);
        menuButton.onClick.AddListener(ToMenu);

        WinUI.OnShow += OnWinUIOpen;
        WinUI.OnHide += OnWinUIHide;
        
        Hide();
    }

    private void OnDestroy()
    {
        pauseMenuAction.action.Disable();
        pauseMenuAction.action.performed -= TogglePauseMenu;
        
        testButton.onClick.RemoveListener(Test);
        resetButton.onClick.RemoveListener(ResetScene);
        menuButton.onClick.RemoveListener(ToMenu);
        
        WinUI.OnShow -= OnWinUIOpen;
        WinUI.OnHide -= OnWinUIHide;
    }

    private void TogglePauseMenu(InputAction.CallbackContext callback)
    {
        if (isShowing)
            Hide();
        else
            Show();
        
        OnPauseMenuToggled?.Invoke(isShowing);
    }

    private void Show()
    {
        if (isWinUIOpen) return;
        
        isShowing = true;
        gameObject.SetActive(true);

        if (GameManager.IsTestingToSubmit)
        {
            testButton.interactable = true;
            testButtonText.text = "Voltar à construção";
        }
        else
        {
            bool isExploring = GameManager.CurrentGameState == GameState.Exploring;
            var treasureCounter = FindFirstObjectByType<TreasureCollectCounter>();
            var hasNoTreasureOnScene = treasureCounter != null && treasureCounter.TreasuresOnScene <= 0;

            testButtonText.text = "Testar fase";
            treasureHintText.gameObject.SetActive(hasNoTreasureOnScene);

            testButton.interactable = !isExploring && !hasNoTreasureOnScene;
        }
        
        GameManager.ShowCursor();

        GameManager.Pause();
    }

    private void Hide()
    {
        gameObject.SetActive(false);
        
        GameManager.Resume();
        GameManager.HideCursor();
        
        isShowing = false;
    }

    private void Test()
    {
        if (GameManager.IsPlayerDead)
        {
            GameManager.IsTestingToSubmit = false;
            GameManager.IsPlayerDead = false;
            GameManager.SetCanEnterBuildMode(true);
            
            GameManager.ChangeGameStateToTestingBuild();
            
            Hide();
            
            SaveSystem.NextSaveToLoad = "current_build";
            SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
            return;
        }
        
        OnTest?.Invoke();
        
        if (GameManager.IsTestingToSubmit)
        {
            GameManager.IsTestingToSubmit = false;
            GameManager.SetCanEnterBuildMode(true);
            
            GameManager.ChangeGameStateToTestingBuild();
        }
        else
        {
            GameManager.IsTestingToSubmit = true;
            GameManager.SetCanEnterBuildMode(false);
            
            GameManager.ChangeGameStateToExploring();
        }
        
        Hide();
    }
    
    private void ResetScene()
    {
        GameManager.Resume();

        if (GameManager.IsTestingToSubmit)
        {
            GameManager.IsTestingToSubmit = false;
            GameManager.SetCanEnterBuildMode(true);

            GameManager.ChangeGameStateToTestingBuild();
                
            SaveSystem.NextSaveToLoad = string.Empty;
            SaveSystem.ClearAllSaves(false, false);
        }
        
        GameManager.IsPlayerDead = false;
        PlayerInventory.Instance.ClearGoldCache();
        
        if (GameManager.CurrentGameState != GameState.Exploring && !GameManager.IsTestingToSubmit)
        {
            SaveSystem.NextSaveToLoad = string.Empty;
            SaveSystem.ClearAllSaves(false, false);
        }
        
        SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
    }
    
    private void ToMenu()
    {
        PlayerInventory.Instance.ClearGoldCache();
        GameManager.IsTestingToSubmit = false;
        GameManager.IsPlayerDead = false;
        
        GameManager.Resume();
        SceneManager.LoadSceneAsync("Main_Menu");
    }

    private void OnWinUIOpen()
    {
        isWinUIOpen = true;
        
        if (isShowing)
            Hide();
    }

    private void OnWinUIHide()
    {
        isWinUIOpen = false;
    }
}
