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
        okButton.onClick.AddListener(OnWinExploring);

        TreasureCollectCounter.OnAllTreasuresCollected += TryShow;
        
        Hide();
    }
    
    private void OnDestroy()
    {
        submitButton.onClick.RemoveListener(OnSubmitButtonClicked);
        okButton.onClick.RemoveListener(OnWinExploring);
            
        TreasureCollectCounter.OnAllTreasuresCollected -= TryShow;
    }

    private void TryShow()
    {
        if (GameManager.CurrentGameState == GameState.Exploring)
            ShowCollectAll();
        else
            ShowSubmit();
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

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
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        GameManager.Resume();
    }

    private void OnSubmitButtonClicked()
    {
        PlayerInventory.Instance.SpendGoldToRemove();
        Hide();
    }

    private void OnWinExploring()
    {
        OnGetGold?.Invoke();
        Hide();
        
        GameManager.Resume();
        SceneManager.LoadSceneAsync(2);
    }
}
