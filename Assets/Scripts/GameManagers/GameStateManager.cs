using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    private GameGrid gameGrid;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    [SerializeField] private bool allowEscapeReturnToMenu = true;

    private GameState currentState = GameState.None;
    private GameState previousState = GameState.None;
    private float gameTime = 0f;
    private bool isTimerRunning = false;

    // State events
    public static event Action<GameState, GameState> OnBeforeStateChange;
    public static event Action<GameState, GameState> OnStateChanged;
    public static event Action OnGameStarted;
    public static event Action OnGamePaused;
    public static event Action OnGameResumed;
    public static event Action<float> OnGameWon;
    public static event Action OnGameLost;

    // Public properties
    public GameState CurrentState => currentState;
    public float GameTime => gameTime;
    public bool IsGameActive => currentState == GameState.Playing;
    public bool CanAcceptInput => currentState == GameState.Playing;

    public bool CanMove => currentState == GameState.Playing || currentState == GameState.Won;
    public bool CanUseAbilities => currentState == GameState.Playing;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LogDebug("GameStateManager initialized");
    }

    private void Start()
    {
        var scene = SceneManager.GetActiveScene().name;

        if (scene == "MainMenu")
        {
            SetState(GameState.MainMenu);
        }
        else
        {
            StartNewGame();
        }
    }

    private void Update()
    {
        if (isTimerRunning && currentState == GameState.Playing)
        {
            gameTime += Time.deltaTime;
        }

        if (allowEscapeReturnToMenu && Input.GetKeyDown(KeyCode.Escape))
        {
            if (currentState == GameState.Won || currentState == GameState.Lost)
                ReturnToMenu();
        }
    }

    public void SetState(GameState newState)
    {
        if (currentState == newState || !Enum.IsDefined(typeof(GameState), newState))
            return;

        GameState oldState = currentState;
        previousState = currentState;

        OnBeforeStateChange?.Invoke(oldState, newState);

        currentState = newState;
        LogDebug($"State changed: {oldState} -> {newState}");

        if (oldState == GameState.Playing)
            StopTimer();

        switch (newState)
        {
            case GameState.MainMenu:
                LoadMainMenu();
                break;

            case GameState.Playing:
                Time.timeScale = 1f;
                StartTimer();
                if (oldState == GameState.Paused)
                {
                    OnGameResumed?.Invoke();
                }
                break;

            case GameState.Paused:
                Time.timeScale = 0f;
                OnGamePaused?.Invoke();
                break;

            case GameState.Won:
                StopTimer();
                Time.timeScale = 1f;
                OnGameWon?.Invoke(gameTime);
                break;

            case GameState.Lost:
                StopTimer();
                Time.timeScale = 1f;
                OnGameLost?.Invoke();
                break;
        }

        OnStateChanged?.Invoke(oldState, newState);
    }


    public void StartNewGame()
    {
        ResetTimer();
        SetState(GameState.Playing);
        OnGameStarted?.Invoke();
    }

    //public void RestartGame() => StartNewGame();
    public void RestartGame()
    {
        ResetTimer();
        CharacterManager.Instance.SpawnCharacter(CharacterManager.Instance.selectedCharacterData);

        SetState(GameState.Playing);
        OnGameStarted?.Invoke();
    }

    public void PauseGame() => SetState(GameState.Paused);
    public void ResumeGame() => SetState(GameState.Playing);
    public void WinGame() => SetState(GameState.Won);
    public void LoseGame() => SetState(GameState.Lost);
    public void ReturnToMenu() => SetState(GameState.MainMenu);

    private void LoadMainMenu()
    {
        Time.timeScale = 1f;
        ResetTimer();
        SceneManager.LoadScene("MainMenu");
    }

    private void StartTimer() => isTimerRunning = true;
    private void StopTimer() => isTimerRunning = false;

    private void ResetTimer()
    {
        gameTime = 0f;
        isTimerRunning = false;
    }

    public string GetFormattedGameTime()
    {
        int minutes = Mathf.FloorToInt(gameTime / 60f);
        int seconds = Mathf.FloorToInt(gameTime % 60f);
        return minutes.ToString("00") + ":" + seconds.ToString("00");
    }

    private void LogDebug(string msg)
    {
        if (showDebugLogs)
            Debug.Log($"[GSM] {msg}");
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}

public enum GameState
{
    None,
    MainMenu,
    Playing,
    Paused,
    Won,
    Lost
}
