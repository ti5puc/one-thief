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
    public static event Action OnInitialized;
    public static event Action<GameState> OnGameStateChanged;
    
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private LayerMask groundLayerMask;

    [Space(10)]
    [SerializeField] private bool startOnBuildingForDebug;

    [Space(10)]
    [SerializeField] private PlaceableSettings treasureChestReference;
    
    [Space(10)]
    [SerializeField] private FirebaseManager firebaseManager;
    
    [Header("Debug")]
    [SerializeField, ReadOnly] private GameState currentGameState;
    [SerializeField, ReadOnly] private bool canEnterBuildMode;
    [SerializeField, ReadOnly] private int nextLayoutIndex;
    [SerializeField, ReadOnly] private bool isGamePaused;
    [SerializeField, ReadOnly] private bool isTestingToSubmit;
    [SerializeField, ReadOnly] private bool isPlayerDead;
    [SerializeField, ReadOnly] private bool isInitialized;
    
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
    public static PlaceableSettings TreasureChestReference => Instance.treasureChestReference;
    public static bool IsTestingToSubmit
    {
        get => Instance.isTestingToSubmit;
        set => Instance.isTestingToSubmit = value;
    }
    public static bool IsPlayerDead
    {
        get => Instance.isPlayerDead;
        set => Instance.isPlayerDead = value;
    }
    public static bool IsInitialized => Instance.isInitialized;
    public static FirebaseManager FirebaseManager => Instance.firebaseManager;

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
        
        isInitialized = true;
        OnInitialized?.Invoke();
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

    public static void ShowCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    public static void HideCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
