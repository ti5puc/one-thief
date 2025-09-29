using NaughtyAttributes;
using UnityEngine;

public enum GameState
{
    Exploring,
    Building
}

public class GameManager : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField, ReadOnly] private GameState currentGameState;

    public static GameManager Instance { get; private set; }
    public static GameState CurrentGameState => Instance.currentGameState;

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
