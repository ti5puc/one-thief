using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InstructionsUI : MonoBehaviour
{
    [Serializable]
    public struct InstructionPerState
    {
        public GameState GameState;
        [TextArea(3, 25)] public string InstructionText;
    }

    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private List<InstructionPerState> instructionsPerState = new();

    [Space(10)]
    [SerializeField] private RectTransform instructionsPanel;
    [SerializeField] private Button toggleInstructionsButton;
    [SerializeField] private TMP_Text toggleInstructionsButtonText;
    [SerializeField] private float slideDuration = 0.3f;
    [SerializeField] private Vector2 buttonShownPosition;
    [SerializeField] private Vector2 buttonHiddenPosition;

    private bool isShowing = true;
    private Vector2 shownPosition;
    private Vector2 hiddenPosition;
    private RectTransform toggleButtonRect;
    private Tween buttonTween;

    private void Awake()
    {
        shownPosition = instructionsPanel.anchoredPosition;
        hiddenPosition = shownPosition + new Vector2(-instructionsPanel.rect.width, 0f);

        toggleButtonRect = toggleInstructionsButton.GetComponent<RectTransform>();

        UpdateInstructions(GameManager.CurrentGameState);

        GameManager.OnGameStateChanged += UpdateInstructions;
        PauseMenuUI.OnToggleInstructions += ToggleInstructions;
        PlayerSave.OnLevelLoaded += CheckInstructionsVisibility;

        toggleInstructionsButton.onClick.AddListener(ToggleInstructions);

        ApplyFirstTimeVisibility();
    }

    private void OnDestroy()
    {
        GameManager.OnGameStateChanged -= UpdateInstructions;
        PauseMenuUI.OnToggleInstructions -= ToggleInstructions;
        PlayerSave.OnLevelLoaded -= CheckInstructionsVisibility;

        toggleInstructionsButton.onClick.RemoveListener(ToggleInstructions);
    }

    private void ApplyFirstTimeVisibility()
    {
        if (GameManager.ShowFirstTimeInstructionsOnThisSession)
        {
            GameManager.ShowFirstTimeInstructionsOnThisSession = false;
            // already visible by default, nothing to do
        }
        else
        {
            HideImmediate();
        }
    }

    private void CheckInstructionsVisibility()
    {
        if (!GameManager.ShowFirstTimeInstructionsOnThisSession)
            HideImmediate();
    }

    private void HideImmediate()
    {
        isShowing = false;
        toggleInstructionsButtonText.text = ">";
        instructionsPanel.anchoredPosition = hiddenPosition;
        instructionsPanel.gameObject.SetActive(false);
        toggleButtonRect.anchoredPosition = buttonHiddenPosition;
    }

    private void UpdateInstructions(GameState gameState)
    {
        instructionText.text = gameState switch
        {
            GameState.Building => instructionsPerState.Find(i => i.GameState == GameState.Building).InstructionText,
            GameState.Exploring => instructionsPerState.Find(i => i.GameState == GameState.Exploring).InstructionText,
            GameState.TestingBuild => instructionsPerState.Find(i => i.GameState == GameState.TestingBuild).InstructionText,
            _ => instructionText.text
        };
    }

    private void ToggleInstructions()
    {
        isShowing = !isShowing;
        toggleInstructionsButtonText.text = isShowing ? "<" : ">";

        instructionsPanel.DOKill();
        buttonTween?.Kill();
        if (isShowing)
        {
            instructionsPanel.gameObject.SetActive(true);
            instructionsPanel.anchoredPosition = hiddenPosition;
            instructionsPanel.DOAnchorPos(shownPosition, slideDuration).SetEase(Ease.OutCubic);
            buttonTween = toggleButtonRect.DOAnchorPos(buttonShownPosition, slideDuration).SetEase(Ease.OutCubic);
        }
        else
        {
            instructionsPanel.DOAnchorPos(hiddenPosition, slideDuration).SetEase(Ease.InCubic)
                .OnComplete(() => instructionsPanel.gameObject.SetActive(false));
            buttonTween = toggleButtonRect.DOAnchorPos(buttonHiddenPosition, slideDuration).SetEase(Ease.InCubic);
        }
    }
}
