using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using DG.Tweening;

// Action definitions - data-driven approach
[System.Serializable]
public class GameActions
{
    [Header("Trap Actions")]
    public UnityEngine.Events.UnityEvent<Cell> OnTrapRevealed;
    public UnityEngine.Events.UnityEvent<Cell> OnTrapExploded;

    [Header("Cell Actions")]
    public UnityEngine.Events.UnityEvent<Cell> OnCellRevealed;
    public UnityEngine.Events.UnityEvent<Cell> OnCellFlagged;
    public UnityEngine.Events.UnityEvent<Cell> OnCellUnflagged;

    [Header("Game State Actions")]
    public UnityEngine.Events.UnityEvent OnGameWon;
    public UnityEngine.Events.UnityEvent OnGameLost;
    public UnityEngine.Events.UnityEvent OnFloodFillStarted;
    public UnityEngine.Events.UnityEvent OnFloodFillCompleted;
}

[System.Serializable]
[Tooltip("Can the player reveal cells that are flagged?")]
public class InteractionRules
{

    [Header("Reveal Rules")]
    public bool canRevealFlaggedCells = false;
    public bool canRevealProtectedCells = false;
    public bool requireTrapsGenerated = true;

    [Header("Flag Rules")]
    public bool canFlagRevealedCells = false;
    public bool canFlagProtectedCells = false;
    public bool canFlagBeforeTrapsGenerated = false;

    [Header("Step Rules")]
    public bool canStepOnFlaggedCells = false;
    public bool canStepOnProtectedCells = false;
    public bool explodeOnTrapStep = true;
}

public class GameBoard : MonoBehaviour
{
    public static GameBoard Instance { get; private set; }

    [Header("Grid Settings")]
    [Tooltip("Configuration for grid width, height, and trap density")] [SerializeField] private GridConfig gridConfig;

    [Header("Components")]
    [Tooltip("Reference to the BoardRenderer component")] [SerializeField] private BoardRenderer boardRenderer;

    [Header("UI Panels")]
    [Tooltip("Panel shown when the player loses the game")] [SerializeField] private GameObject losePanel;

    [Header("Animation Settings")]
    [Tooltip("Duration for reveal animations")] [SerializeField] private float revealAnimationDuration = 0.5f;
    [Tooltip("Use diagonal directions in flood fill reveal")] [SerializeField] private bool useEightDirections = true;

    [Header("Game Actions - Data Driven")]
    [Tooltip("Data-driven UnityEvents for game actions")] [SerializeField] private GameActions gameActions;
    [Header("Interaction Rules")]
    [Tooltip("Rules governing player interactions with cells")] [SerializeField] private InteractionRules interactionRules;

    private GameGrid gameGrid;
    private bool isInitialized;
    private bool isFloodFilling;

    // Public properties
    public GameGrid Grid => gameGrid;
    public BoardRenderer Renderer => boardRenderer;
    public bool IsInitialized => isInitialized;
    public bool IsFloodFilling => isFloodFilling;
    public GameActions Actions => gameActions;

    #region Unity Lifecycle
    private void Awake()
    {
        InitializeSingleton();
        InitializeDOTween();
        InitializeComponents();
    }

    private void OnEnable()
    {
        SubscribeToEvents();
    }

    private void OnDisable()
    {
        UnsubscribeFromEvents();
    }

    private void Start()
    {
        Debug.Log("GameBoard Start");
        InitializeBoard();
    }
    #endregion

    #region Initialization
    private void InitializeSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeDOTween()
    {
        DOTween.Init(true, true, LogBehaviour.ErrorsOnly).SetCapacity(500, 125);
    }

    private void InitializeComponents()
    {
        Debug.Log("GameBoard Awake");
        if (boardRenderer == null)
        {
            boardRenderer = GetComponentInChildren<BoardRenderer>();
            Debug.Log($"BoardRenderer found in children: {boardRenderer != null}");
        }
    }

    private void SubscribeToEvents()
    {
        GameStateManager.OnGameStarted += HandleGameStarted;
        GameStateManager.OnStateChanged += HandleStateChanged;
    }

    private void UnsubscribeFromEvents()
    {
        GameStateManager.OnGameStarted -= HandleGameStarted;
        GameStateManager.OnStateChanged -= HandleStateChanged;
    }
    #endregion

    #region Main Actions - Clearly Separated

    /// <summary>
    /// MAIN ACTION: Character steps on a cell
    /// This is where trap stepping happens!
    /// </summary>
    public void HandleCharacterStep(Vector3Int characterGridPosition)
    {
        var context = CreateInteractionContext("Character Step", characterGridPosition);
        if (!ValidateInteraction(context)) return;

        var cell = gameGrid.GetCell(characterGridPosition.x, characterGridPosition.y);
        if (!CanStepOnCell(cell, context)) return;

        // Generate traps if needed (first interaction)
        if (!gameGrid.TrapsGenerated)
        {
            GenerateTrapsAfterFirstReveal(characterGridPosition);
            cell = gameGrid.GetCell(characterGridPosition.x, characterGridPosition.y);
        }

        // 🎯 TRAP STEP ACTION - Easy to find!
        if (cell.type == Cell.CellType.Trap)
        {
            ExecuteTrapStepAction(cell);
            return;
        }

        // Regular cell reveal
        if (!cell.revealed)
        {
            RevealCellLogic(cell);
        }
    }

    /// <summary>
    /// MAIN ACTION: Toggle cell flag
    /// </summary>
    public void HandleCellFlag(Vector3Int cellPosition, GameObject flagEffectPrefab = null)
    {
        var context = CreateInteractionContext("Cell Flag", cellPosition);
        if (!ValidateInteraction(context)) return;

        var cell = gameGrid.GetCell(cellPosition.x, cellPosition.y);
        if (!CanFlagCell(cell, context)) return;

        ExecuteFlagToggleAction(cell, flagEffectPrefab);
    }

    /// <summary>
    /// MAIN ACTION: Manual cell click (for debugging/chord clicking)
    /// </summary>
    public bool HandleManualCellClick(int x, int y)
    {
        var context = CreateInteractionContext("Manual Click", new Vector3Int(x, y, 0));
        if (!ValidateInteraction(context)) return false;

        var cell = gameGrid.GetCell(x, y);
        if (!CanRevealCell(cell, context)) return false;

        // Generate traps if needed
        if (!gameGrid.TrapsGenerated)
        {
            GenerateTrapsAfterFirstReveal(new Vector3Int(x, y, 0));
            cell = gameGrid.GetCell(x, y);
        }

        // Check for trap click
        if (cell.type == Cell.CellType.Trap)
        {
            ExecuteTrapClickAction(cell);
            return false;
        }

        RevealCellLogic(cell);
        return true;
    }
    #endregion

    #region Action Execution - All trap actions clearly grouped

    /// <summary>
    ///  TRAP STEP ACTION - Character stepped on trap
    /// </summary>
    private void ExecuteTrapStepAction(Cell trapCell)
    {
        Debug.Log($"CHARACTER STEPPED ON TRAP at {trapCell.position} - Game Over!");

        if (interactionRules.explodeOnTrapStep)
        {
            trapCell.exploded = true;
        }

        trapCell.revealed = true;
        boardRenderer.DrawCell(trapCell);

        // Fire data-driven events
        gameActions.OnTrapRevealed?.Invoke(trapCell);
        gameActions.OnTrapExploded?.Invoke(trapCell);
        gameActions.OnGameLost?.Invoke();

        GameStateManager.Instance?.LoseGame();
    }

    /// <summary>
    /// TRAP CLICK ACTION - Manual trap click
    /// </summary>
    private void ExecuteTrapClickAction(Cell trapCell)
    {
        Debug.Log($"TRAP MANUALLY CLICKED at {trapCell.position} - Game Over!");

        trapCell.exploded = true;
        trapCell.revealed = true;
        boardRenderer.DrawCell(trapCell);

        gameActions.OnTrapRevealed?.Invoke(trapCell);
        gameActions.OnTrapExploded?.Invoke(trapCell);
        gameActions.OnGameLost?.Invoke();

        GameStateManager.Instance?.LoseGame();
    }

    /// <summary>
    /// FLAG TOGGLE ACTION
    /// </summary>
    private void ExecuteFlagToggleAction(Cell cell, GameObject flagEffectPrefab)
    {
        bool wasFlagged = cell.flagged;
        cell.flagged = !cell.flagged;

        Debug.Log($"Cell at {cell.position} flag toggled to: {cell.flagged}");
        boardRenderer.DrawCell(cell);

        // Fire appropriate event
        if (cell.flagged)
        {
            gameActions.OnCellFlagged?.Invoke(cell);
            SpawnFlagEffect(cell, flagEffectPrefab);
        }
        else
        {
            gameActions.OnCellUnflagged?.Invoke(cell);
        }

        CheckWinCondition();
    }

    private void SpawnFlagEffect(Cell cell, GameObject flagEffectPrefab)
    {
        if (flagEffectPrefab != null)
        {
            Vector3 worldPos = boardRenderer.GetWorldPosition(cell) + new Vector3(0.5f, 0.5f, 0);
            Instantiate(flagEffectPrefab, worldPos, Quaternion.identity);
        }
    }
    #endregion

    #region Validation - Rule-based system

    private struct InteractionContext
    {
        public string actionName;
        public Vector3Int position;
        public bool isValid;
        public string reason;
    }

    private InteractionContext CreateInteractionContext(string actionName, Vector3Int position)
    {
        return new InteractionContext
        {
            actionName = actionName,
            position = position,
            isValid = true,
            reason = ""
        };
    }

    private bool ValidateInteraction(InteractionContext context)
    {
        if (!GameStateManager.Instance?.CanAcceptInput ?? true)
        {
            Debug.LogWarning($"Cannot handle {context.actionName} - game state doesn't allow input");
            return false;
        }

        if (!isInitialized || isFloodFilling)
        {
            Debug.LogWarning($"Cannot handle {context.actionName}: IsInitialized={isInitialized}, IsFloodFilling={isFloodFilling}");
            return false;
        }

        return true;
    }

    private bool CanStepOnCell(Cell cell, InteractionContext context)
    {
        if (cell == null)
        {
            Debug.LogWarning($"Invalid cell for {context.actionName} at {context.position}");
            return false;
        }

        if (cell.flagged && !interactionRules.canStepOnFlaggedCells)
        {
            Debug.LogWarning($"Cannot step on flagged cell at {context.position}");
            return false;
        }

        if (cell.IsProtected && !interactionRules.canStepOnProtectedCells)
        {
            Debug.LogWarning($"Cannot step on protected cell at {context.position}");
            return false;
        }

        return true;
    }

    private bool CanFlagCell(Cell cell, InteractionContext context)
    {
        if (cell == null)
        {
            Debug.LogWarning($"Invalid cell for {context.actionName} at {context.position}");
            return false;
        }

        if (cell.revealed && !interactionRules.canFlagRevealedCells)
        {
            Debug.LogWarning($"Cannot flag revealed cell at {context.position}");
            return false;
        }

        if (cell.IsProtected && !interactionRules.canFlagProtectedCells)
        {
            Debug.LogWarning($"Cannot flag protected cell at {context.position}");
            return false;
        }

        if (!gameGrid.TrapsGenerated && !interactionRules.canFlagBeforeTrapsGenerated)
        {
            Debug.Log("Cannot flag cells before first reveal");
            return false;
        }

        return true;
    }

    private bool CanRevealCell(Cell cell, InteractionContext context)
    {
        if (cell == null)
        {
            Debug.LogWarning($"Invalid cell for {context.actionName} at {context.position}");
            return false;
        }

        if (cell.revealed || cell.IsProtected)
        {
            Debug.LogWarning($"Cannot reveal cell at {context.position} - already revealed or protected");
            return false;
        }

        if (cell.flagged && !interactionRules.canRevealFlaggedCells)
        {
            Debug.LogWarning($"Cannot reveal flagged cell at {context.position}");
            return false;
        }

        return true;
    }
    #endregion

    #region Game Flow
    private void HandleGameStarted()
    {
        Debug.Log("GameBoard: Game started event received");
        StartNewGame();
    }

    private void HandleStateChanged(GameState previousState, GameState newState)
    {
        switch (newState)
        {
            case GameState.Playing:
                if (!isInitialized)
                {
                    InitializeBoard();
                }
                break;

            case GameState.Lost:
                RevealAllTraps();
                break;
        }
    }

    public void InitializeBoard()
    {
        int width = gridConfig.width;
        int height = gridConfig.height;
        Debug.Log($"Initializing board with size {width}x{height}");

        gameGrid = new GameGrid(width, height);
        boardRenderer.Initialize(gameGrid);
        boardRenderer.DrawGrid();

        isInitialized = true;
        isFloodFilling = false;
        Debug.Log("Board initialization complete - ready for gameplay");
    }

    public void StartNewGame()
    {
        Debug.Log("Starting new game - waiting for first action");

        if (!isInitialized)
        {
            InitializeBoard();
        }

        gameGrid.Reset();
        boardRenderer.DrawGrid();

        Debug.Log("New game started - traps will be generated on first reveal");
    }
    #endregion

    #region Reveal Logic & Flood Fill
    public void RevealCellsAroundPosition(Vector3Int centerCellPos, int radius)
    {
        if (gameGrid == null) return;

        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                Vector3Int currentPos = centerCellPos + new Vector3Int(x, y, 0);
                Cell cell = gameGrid.GetCell(currentPos.x, currentPos.y);

                if (cell != null && !cell.revealed && !cell.flagged)
                {
                    cell.revealed = true;
                    boardRenderer.DrawCell(cell);
                    gameActions.OnCellRevealed?.Invoke(cell);
                }
            }
        }
    }

    private void RevealCellLogic(Cell cell)
    {
        if (cell == null || cell.revealed || cell.type == Cell.CellType.Trap || cell.IsProtected || cell.flagged)
        {
            return;
        }

        Debug.Log($"Attempting to reveal cell at {cell.position} - Type: {cell.type}, Number: {cell.number}");

        if (cell.type == Cell.CellType.Empty)
        {
            Debug.Log($"Starting smooth flood fill from {cell.position}");
            StartCoroutine(SmoothFloodFill(cell, new HashSet<Cell>()));
        }
        else
        {
            cell.revealed = true;
            boardRenderer.DrawCell(cell);
            gameActions.OnCellRevealed?.Invoke(cell);
            CheckWinCondition();
        }
    }

    private IEnumerator SmoothFloodFill(Cell startCell, HashSet<Cell> visited)
    {
        isFloodFilling = true;
        gameActions.OnFloodFillStarted?.Invoke();
        Debug.Log("Flood fill started - game cannot be paused during this operation");

        yield return StartCoroutine(Flood(startCell, visited));

        isFloodFilling = false;
        gameActions.OnFloodFillCompleted?.Invoke();
        Debug.Log("Smooth flood fill complete - game can be paused again");

        CheckWinCondition();
    }

    private IEnumerator Flood(Cell cell, HashSet<Cell> visited)
    {
        if (cell == null || cell.revealed || cell.type == Cell.CellType.Trap || cell.IsProtected || cell.flagged || visited.Contains(cell))
        {
            yield break;
        }

        visited.Add(cell);
        cell.revealed = true;
        boardRenderer.DrawCell(cell);
        gameActions.OnCellRevealed?.Invoke(cell);
        Debug.Log($"Revealed cell at {cell.position} - Type: {cell.type}, Number: {cell.number}");

        yield return new WaitForSeconds(revealAnimationDuration);

        if (cell.type == Cell.CellType.Empty)
        {
            yield return StartCoroutine(FloodAdjacentCells(cell.position, visited));
        }
    }

    private IEnumerator FloodAdjacentCells(Vector3Int position, HashSet<Cell> visited)
    {
        if (useEightDirections)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    if (gameGrid.TryGetCell(position.x + dx, position.y + dy, out Cell adjacent))
                    {
                        yield return StartCoroutine(Flood(adjacent, visited));
                    }
                }
            }
        }
        else
        {
            int[,] directions = { { -1, 0 }, { 1, 0 }, { 0, -1 }, { 0, 1 } };
            for (int i = 0; i < 4; i++)
            {
                int x = position.x + directions[i, 0];
                int y = position.y + directions[i, 1];
                if (gameGrid.TryGetCell(x, y, out Cell adjacent))
                {
                    yield return StartCoroutine(Flood(adjacent, visited));
                }
            }
        }
    }
    #endregion

    #region Win/Loss Logic
    private void GenerateTrapsAfterFirstReveal(Vector3Int firstRevealPosition)
    {
        if (!GameStateManager.Instance?.CanAcceptInput ?? false)
        {
            return;
        }

        int trapCount = Mathf.RoundToInt(gridConfig.width * gridConfig.height * gridConfig.trapDensity);
        Debug.Log($"Generating {trapCount} traps after first reveal at {firstRevealPosition}");

        gameGrid.GenerateTraps(firstRevealPosition, trapCount);
        boardRenderer.DrawGrid();

        Debug.Log("Traps generated successfully");
    }

    public bool CheckWinCondition()
    {
        if (!GameStateManager.Instance?.IsGameActive ?? false)
            return false;

        if (!gameGrid.TrapsGenerated)
            return false;

        var allCells = gameGrid.GetAllCells();
        int totalTraps = 0;
        int flaggedTraps = 0;
        int unrevealedNonTraps = 0;

        foreach (Cell cell in allCells)
        {
            if (cell.type == Cell.CellType.Trap)
            {
                totalTraps++;
                if (cell.flagged)
                    flaggedTraps++;
            }
            else if (!cell.revealed && !cell.IsProtected)
            {
                unrevealedNonTraps++;
            }
        }

        bool allTrapsFlagged = (totalTraps > 0 && flaggedTraps == totalTraps);
        bool allNonTrapsRevealed = (unrevealedNonTraps == 0);

        if (allTrapsFlagged || allNonTrapsRevealed)
        {
            Debug.Log($"Game Won! Condition: {(allTrapsFlagged ? "All traps flagged" : "All non-traps revealed")}");
            AutoFlagRemainingTraps();
            gameActions.OnGameWon?.Invoke();
            GameStateManager.Instance?.WinGame();
            return true;
        }

        return false;
    }

    private void AutoFlagRemainingTraps()
    {
        var allCells = gameGrid.GetAllCells();
        bool anyTrapsAutoFlagged = false;

        foreach (Cell cell in allCells)
        {
            if (cell.type == Cell.CellType.Trap && !cell.flagged)
            {
                cell.flagged = true;
                anyTrapsAutoFlagged = true;
                gameActions.OnCellFlagged?.Invoke(cell);
                Debug.Log($"Auto-flagged trap at {cell.position}");
            }
        }

        if (anyTrapsAutoFlagged)
        {
            boardRenderer.DrawGrid();
            Debug.Log("All remaining traps have been auto-flagged!");
        }
    }

    private void RevealAllTraps()
    {
        if (gameGrid == null)
            return;

        var trapCells = gameGrid.GetCellsByType(Cell.CellType.Trap);
        foreach (Cell trap in trapCells)
        {
            trap.revealed = true;
            gameActions.OnTrapRevealed?.Invoke(trap);
        }

        boardRenderer.DrawGrid();
        Debug.Log("All traps revealed");
    }
    #endregion

    #region Public Utility Methods
    public Cell GetCellAtWorldPosition(Vector3 worldPos)
    {
        return boardRenderer.GetCellAtWorldPosition(worldPos);
    }

    public void RefreshVisuals()
    {
        if (isInitialized)
        {
            boardRenderer.DrawGrid();
        }
    }

    public void OverrideGridSize(int w, int h)
    {
        gridConfig.width = w;
        gridConfig.height = h;
        isInitialized = false;
    }

    public void OverrideTrapDensity(float density)
    {
        gridConfig.trapDensity = Mathf.Clamp01(density);
        isInitialized = false;
        Debug.Log($"Trap density overridden to {gridConfig.trapDensity}");
    }

    public void ResetBoard()
    {
        if (gameGrid != null)
        {
            gameGrid.Reset();
            isFloodFilling = false;
            boardRenderer.DrawGrid();
            Debug.Log("Board reset - ready for first reveal");
        }
    }
    #endregion
}