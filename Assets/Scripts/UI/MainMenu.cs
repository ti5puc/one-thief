using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject howToPlayPanel;

    [Space(10)]
    [SerializeField] private Button howToPlayOkButton;
    [SerializeField] private Button howToPlayBackButton;

    [Space(10)]
    [SerializeField] private GameObject earnedGoldPanel;
    [SerializeField] private TMP_Text earnedGoldText;
    [SerializeField] private Button okButton;
    
    private const string first_time_play_key = "FirstTimePlay";
    
    private bool IsFirstTimePlay
    {
        get => PlayerPrefs.GetInt(first_time_play_key, 1) == 1;
        set
        {
            Debug.Log($"[MainMenu] Setting IsFirstTimePlay to {value}");
            PlayerPrefs.SetInt(first_time_play_key, value ? 1 : 0);
        }
    }
    
    private void Awake()
    {
        GameManager.ShowCursor();
        howToPlayOkButton.onClick.AddListener(OnHowToPlayOkButtonClicked);
        howToPlayBackButton.onClick.AddListener(ReturnToMainPanel);
        
        ShowMainPanel();
        
        okButton.onClick.AddListener(OnOkClicked);

        int taxGold = PlayerInventory.Instance != null ? PlayerInventory.Instance.TaxGoldToGain : 0;
        if (taxGold > 0)
            ShowEarnedGoldPanel(taxGold);
    }
    
    private void OnDestroy()
    {
        howToPlayOkButton.onClick.RemoveListener(OnHowToPlayOkButtonClicked);
        howToPlayBackButton.onClick.RemoveListener(ReturnToMainPanel);
        okButton.onClick.RemoveListener(OnOkClicked);
    }

    public void PlayGame()
    {
        if (IsFirstTimePlay)
        {
            mainPanel.SetActive(false);
            howToPlayPanel.SetActive(true);
        
            howToPlayOkButton.gameObject.SetActive(IsFirstTimePlay);
        }
        else
            SceneManager.LoadSceneAsync("Play_Menu");
    }

    public void Options()
    {
        SceneManager.LoadSceneAsync("Options_Menu");
    }

    public void Leaderboard()
    {
        SceneManager.LoadSceneAsync("Leaderboard_Screen");
    }
    
    public void QuitGame()
    {
        Application.Quit();
    }
    
    public void ShowHowToPlayPanel()
    {
        mainPanel.SetActive(false);
        howToPlayPanel.SetActive(true);
        earnedGoldPanel.SetActive(false);
        
        howToPlayOkButton.gameObject.SetActive(false);
        IsFirstTimePlay = false;
    }
    
    public void ShowMainPanel()
    {
        mainPanel.SetActive(true);
        howToPlayPanel.SetActive(false);
        earnedGoldPanel.SetActive(false);
    }

    public void ReturnToMainPanel()
    {
        ShowMainPanel();
        IsFirstTimePlay = false;
    }
    
    public void ShowEarnedGoldPanel(int goldAmount)
    {
        earnedGoldText.text = $"Você ganhou ${goldAmount} de taxa de entrada das suas masmorras!";
        earnedGoldPanel.SetActive(true);
        mainPanel.SetActive(false);
        howToPlayPanel.SetActive(false);
    }
    
    private void OnHowToPlayOkButtonClicked()
    {
        if (IsFirstTimePlay)
        {
            IsFirstTimePlay = false;
            PlayGame();
        }
        else
        {
            IsFirstTimePlay = false;
            ShowMainPanel();
        }
    }

    private void OnOkClicked()
    {
        PlayerInventory.Instance?.ClaimTaxGold();
        ShowMainPanel();
    }
}
