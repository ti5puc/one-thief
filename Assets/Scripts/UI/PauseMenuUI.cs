using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuUI : MonoBehaviour
{
    public static event Action OnTest;
    
    [SerializeField] private Button testButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button menuButton;
    
    [Space(10)]
    [SerializeField] private InputActionReference pauseMenuAction;

    private bool isShowing;
    private bool wasBuilding;
    
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
        
        bool isExploring = GameManager.CurrentGameState == GameState.Exploring;
        testButton.interactable = isExploring == false;
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        GameManager.Pause();
    }

    private void Hide()
    {
        gameObject.SetActive(false);
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        GameManager.Resume();
        
        isShowing = false;
    }

    private void Test()
    {
        OnTest?.Invoke();
        
        wasBuilding = GameManager.CurrentGameState != GameState.Exploring;
        GameManager.ChangeGameStateToExploring();
        
        GameManager.IsTestingToSubmit = true;
        GameManager.SetCanEnterBuildMode(false);
        
        Hide();
    }
    
    private void ResetScene()
    {
        GameManager.Resume();
        GameManager.IsTestingToSubmit = false;

        if (wasBuilding)
        {
            GameManager.ChangeGameStateToTestingBuild();
            SaveSystem.NextSaveToLoad = string.Empty;
        }
        
        PlayerInventory.Instance.ClearGoldCache();
        
        SaveSystem.ClearAllSaves();
        
        SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
    }
    
    private void ToMenu()
    {
        PlayerInventory.Instance.ClearGoldCache();
        GameManager.IsTestingToSubmit = false;
        
        GameManager.Resume();
        SceneManager.LoadSceneAsync(0);
    }
}
