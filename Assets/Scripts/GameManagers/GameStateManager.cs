// GameStateManager.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private GameState currentState = GameState.None;
    private GameState previousState = GameState.None;
    private float gameTime = 0f;
    private bool isTimerRunning = false;

    // Events for state changes
    public static event Action<GameState, GameState> OnStateChanged;
    public static event Action OnGameStarted;
    public static event Action OnGamePaused;
    public static event Action OnGameResumed;
    public static event Action<float> OnGameWon;
    public static event Action OnGameLost;

    // Properties
    public GameState CurrentState => currentState;
    public float GameTime => gameTime;
    public bool IsGameActive => currentState == GameState.Playing;
    public bool CanAcceptInput => currentState == GameState.Playing;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LogDebug("Initialized");
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        string scene = SceneManager.GetActiveScene().name;
        if (scene == "MainMenu")
        {
            SetState(GameState.MainMenu);
        }
        else
        {
            StartNewGame(); // Initialize game scene as a new game
        }
    }

    private void Update()
    {
        if (isTimerRunning && currentState == GameState.Playing)
            gameTime += Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (currentState == GameState.Won || currentState == GameState.Lost)
                ReturnToMenu();
        }
    }

    public void SetState(GameState newState)
    {
        if (currentState == newState) return;

        GameState old = currentState;
        previousState = currentState;
        currentState = newState;
        LogDebug($"{old} → {newState}");

        if (old == GameState.Playing) StopTimer();

        switch (newState)
        {
            case GameState.MainMenu:
                LoadMainMenu();
                break;

            case GameState.Playing:
                Time.timeScale = 1f;
                StartTimer();
                if (old == GameState.Paused)
                {
                    OnGameResumed?.Invoke(); // Notify resume without starting new game
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

        OnStateChanged?.Invoke(old, newState);
    }

    private void LoadMainMenu()
    {
        Time.timeScale = 1f;
        ResetTimer();
        SceneManager.LoadScene("MainMenu");
    }

    public void PauseGame() => SetState(GameState.Paused);

    public void ResumeGame()
    {
        SetState(GameState.Playing); // Will invoke OnGameResumed if from Paused
    }

    public void WinGame() => SetState(GameState.Won);
    public void LoseGame() => SetState(GameState.Lost);
    public void ReturnToMenu() => SetState(GameState.MainMenu);

    public void StartNewGame()
    {
        ResetTimer();
        SetState(GameState.Playing);
        OnGameStarted?.Invoke(); // Trigger board reset only here
    }

    public void RestartGame()
    {
        StartNewGame(); // Restart as a new game
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
        int m = Mathf.FloorToInt(gameTime / 60f);
        int s = Mathf.FloorToInt(gameTime % 60f);
        return $"{m:00}:{s:00}";
    }

    private void LogDebug(string msg)
    {
        if (showDebugLogs) Debug.Log($"[GSM] {msg}");
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
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