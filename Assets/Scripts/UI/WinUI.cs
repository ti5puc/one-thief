using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WinUI : MonoBehaviour
{
    public static event Action OnGetGold;
    
    [SerializeField] private TMP_Text messageText;
    
    [Header("Collected all")]
    [SerializeField] private Transform okGroup;
    [SerializeField] private Button okButton;
    
    [Header("Submit")]
    [SerializeField] private Transform submitGroup;
    [SerializeField] private Button submitButton;
    [SerializeField] private Button denyButton;

    private void Awake()
    {
        submitButton.onClick.AddListener(OnSubmitButtonClicked);
        denyButton.onClick.AddListener(OnDenyButtonClicked);
        okButton.onClick.AddListener(OnWinExploring);

        TreasureCollectCounter.OnAllTreasuresCollected += TryShow;
        
        Hide();
    }
    
    private void OnDestroy()
    {
        submitButton.onClick.RemoveListener(OnSubmitButtonClicked);
        denyButton.onClick.RemoveListener(OnDenyButtonClicked);
        okButton.onClick.RemoveListener(OnWinExploring);
            
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
    }

    private void ShowSubmit()
    {
        messageText.text = "Fase testada. Deseja enviar?";
        submitGroup.gameObject.SetActive(true);
        okGroup.gameObject.SetActive(false);
        
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
    }

    private void OnSubmitButtonClicked()
    {
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
        
        SceneManager.LoadSceneAsync(2);
    }
}
