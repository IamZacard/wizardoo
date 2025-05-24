using UnityEngine;

public enum GameState
{
    Playing,
    Paused,
    GameOver,
    LevelComplete
}

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }
    public GameState CurrentState { get; private set; }

    private void Awake()
    {
        Instance = this;
        SetState(GameState.Playing);
    }

    public void SetState(GameState newState)
    {
        CurrentState = newState;
        Debug.Log("Game state changed to: " + newState);
    }

    public bool IsPlaying => CurrentState == GameState.Playing;
}
