using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuUI : MonoBehaviour
{
    public static event Action OnTest;
    
    [SerializeField] private Button testButton;
    [SerializeField] private TMP_Text testButtonText;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button menuButton;
    
    [Space(10)]
    [SerializeField] private InputActionReference pauseMenuAction;

    private bool isShowing;
    private bool shouldBackToBuild;
    
    private void Awake()
    {
        pauseMenuAction.action.Enable();
        pauseMenuAction.action.performed += TogglePauseMenu;
        
        testButton.onClick.AddListener(Test);
        resetButton.onClick.AddListener(ResetScene);
        menuButton.onClick.AddListener(ToMenu);
        
        Hide();
    }

    private void OnDestroy()
    {
        pauseMenuAction.action.Disable();
        pauseMenuAction.action.performed -= TogglePauseMenu;
        
        testButton.onClick.RemoveListener(Test);
        resetButton.onClick.RemoveListener(ResetScene);
        menuButton.onClick.RemoveListener(ToMenu);
    }

    private void TogglePauseMenu(InputAction.CallbackContext callback)
    {
        if (isShowing)
            Hide();
        else
            Show();
    }

    private void Show()
    {
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
            testButton.interactable = isExploring == false;
            testButtonText.text = "Testar fase";
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
        }
        
        GameManager.IsPlayerDead = false;
        SaveSystem.NextSaveToLoad = string.Empty;
        PlayerInventory.Instance.ClearGoldCache();
        SaveSystem.ClearAllSaves();
        
        SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
    }
    
    private void ToMenu()
    {
        PlayerInventory.Instance.ClearGoldCache();
        GameManager.IsTestingToSubmit = false;
        GameManager.IsPlayerDead = false;
        
        GameManager.Resume();
        SceneManager.LoadSceneAsync(0);
    }
}
