using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

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

    private void Awake()
    {
        UpdateInstructions(GameManager.CurrentGameState);
        GameManager.OnGameStateChanged += UpdateInstructions;
    }
    
    private void OnDestroy()
    {
        GameManager.OnGameStateChanged -= UpdateInstructions;
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
}
