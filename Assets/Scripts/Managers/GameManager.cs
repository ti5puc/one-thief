using System;
using NaughtyAttributes;
using UnityEngine;

public enum GameState
{
    Exploring,
    Building,
    TestingBuild
}

[DefaultExecutionOrder(-100)]
public class GameManager : MonoBehaviour
{
    public static event Action<GameState> OnGameStateChanged;
    
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private LayerMask groundLayerMask;

    [Space(10)]
    [SerializeField] private bool startOnBuildingForDebug;

    [Header("Debug")]
    [SerializeField, ReadOnly] private GameState currentGameState;
    [SerializeField, ReadOnly] private bool canEnterBuildMode;
    [SerializeField, ReadOnly] private int nextLayoutIndex;
    [SerializeField, ReadOnly] private bool isGamePaused;
    
    public static GameManager Instance { get; private set; }
    public static GameState CurrentGameState => Instance.currentGameState;
    public static LayerMask GroundLayerMask => Instance.groundLayerMask;
    public static string PlayerTag => Instance.playerTag;
    public static bool CanEnterBuildMode => Instance.canEnterBuildMode;
    public static int NextLayoutIndex
    {
        get => Instance.nextLayoutIndex;
        set => Instance.nextLayoutIndex = value;
    }
    public static bool IsGamePaused => Instance.isGamePaused;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (startOnBuildingForDebug)
        {
            currentGameState = GameState.TestingBuild;
            canEnterBuildMode = true;
        }
    }

    public static void SetCanEnterBuildMode(bool canEnter)
    {
        Instance.canEnterBuildMode = canEnter;
    }
    
    public static void ChangeGameStateToExploring()
    {
        Instance.currentGameState = GameState.Exploring;
        OnGameStateChanged?.Invoke(Instance.currentGameState);
    }

    public static void ChangeGameStateToBuilding()
    {
        if (Instance.canEnterBuildMode == false)
            return;

        Instance.currentGameState = GameState.Building;
        OnGameStateChanged?.Invoke(Instance.currentGameState);
    }
    
    public static void ChangeGameStateToTestingBuild()
    {
        if (Instance.canEnterBuildMode == false)
            return;
        
        Instance.currentGameState = GameState.TestingBuild;
        OnGameStateChanged?.Invoke(Instance.currentGameState);
    }

    public static void Pause()
    {
        Instance.isGamePaused = true;
    }

    public static void Resume()
    {
        Instance.isGamePaused = false;
    }
}
