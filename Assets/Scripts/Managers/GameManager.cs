using NaughtyAttributes;
using UnityEngine;

public enum GameState
{
    Exploring,
    Building
}

[DefaultExecutionOrder(-100)]
public class GameManager : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private LayerMask groundLayerMask;

    [Header("Debug")]
    [SerializeField, ReadOnly] private GameState currentGameState;

    public static GameManager Instance { get; private set; }
    public static GameState CurrentGameState => Instance.currentGameState;
    public static LayerMask GroundLayerMask => Instance.groundLayerMask;
    public static string PlayerTag => Instance.playerTag;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static void ChangeGameStateToExploring()
    {
        Instance.currentGameState = GameState.Exploring;
    }

    public static void ChangeGameStateToBuilding()
    {
        Instance.currentGameState = GameState.Building;
    }
}
