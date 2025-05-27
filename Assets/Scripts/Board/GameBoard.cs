using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class GameBoard : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private GridConfig gridConfig;

    [Header("Components")]
    [SerializeField] private BoardRenderer boardRenderer;

    [Header("Flood Fill Settings")]
    [SerializeField] private float revealAnimationDuration = 0.5f;
    [SerializeField] private bool useEightDirections = true;

    private GameGrid gameGrid;
    private bool isInitialized;
    private bool isFloodFilling;
    private HashSet<Cell> visitedCells;

    public GameGrid Grid => gameGrid;
    public BoardRenderer Renderer => boardRenderer;
    public bool IsInitialized => isInitialized;
    public bool IsFloodFilling => isFloodFilling; // Expose this property

    private void Awake()
    {
        Debug.Log("GameBoard Awake");
        if (boardRenderer == null)
        {
            boardRenderer = GetComponentInChildren<BoardRenderer>();
            Debug.Log($"BoardRenderer found in children: {boardRenderer != null}");
        }
        DOTween.Init(true, true, LogBehaviour.ErrorsOnly).SetCapacity(500, 125);
    }

    private void OnEnable()
    {
        // Subscribe to game state events
        GameStateManager.OnGameStarted += HandleGameStarted;
        GameStateManager.OnStateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        // Unsubscribe from events
        GameStateManager.OnGameStarted -= HandleGameStarted;
        GameStateManager.OnStateChanged -= HandleStateChanged;
    }

    private void Start()
    {
        Debug.Log("GameBoard Start");
        InitializeBoard();
    }

    private void HandleGameStarted()
    {
        Debug.Log("GameBoard: Game started event received");
        StartNewGame();
    }

    private void HandleStateChanged(GameState previousState, GameState newState)
    {
        // Handle any board-specific state changes
        switch (newState)
        {
            case GameState.Playing:
                // Ensure board is ready for gameplay
                if (!isInitialized)
                {
                    InitializeBoard();
                }
                break;

            case GameState.Lost:
                // Reveal all traps when game is lost
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
        Debug.Log("Starting new game - waiting for first click");

        if (!isInitialized)
        {
            InitializeBoard();
        }

        gameGrid.Reset();
        boardRenderer.DrawGrid();

        Debug.Log("New game started - traps will be generated on first click");
    }

    private void GenerateTrapsAfterFirstClick(Vector3Int firstClickPosition)
    {
        // Check if we can accept input (game state check)
        if (!GameStateManager.Instance?.CanAcceptInput ?? false)
        {
            return;
        }

        int trapCount = Mathf.RoundToInt(gridConfig.width * gridConfig.height * gridConfig.trapDensity);
        Debug.Log($"Generating {trapCount} traps after first click at {firstClickPosition}");

        gameGrid.GenerateTraps(firstClickPosition, trapCount);
        boardRenderer.DrawGrid();

        Debug.Log("Traps generated successfully");
    }

    public bool HandleCellClick(int x, int y)
    {
        // Check game state before processing input
        if (!GameStateManager.Instance?.CanAcceptInput ?? true)
        {
            Debug.LogWarning("Cannot handle click - game state doesn't allow input");
            return false;
        }

        if (!isInitialized || isFloodFilling)
        {
            Debug.LogWarning($"Cannot handle click: IsInitialized={isInitialized}, IsFloodFilling={isFloodFilling}");
            return false;
        }

        Cell cell = gameGrid.GetCell(x, y);
        if (cell == null || cell.revealed || cell.flagged || cell.IsProtected)
        {
            Debug.LogWarning($"Invalid cell click at {x}, {y}");
            return false;
        }

        // Generate traps on first click if not already generated
        if (!gameGrid.TrapsGenerated)
        {
            GenerateTrapsAfterFirstClick(new Vector3Int(x, y, 0));
            // Get the cell again after trap generation as its properties may have changed
            cell = gameGrid.GetCell(x, y);
        }

        // Check if clicked cell is a trap
        if (cell.type == Cell.CellType.Trap)
        {
            Debug.Log($"Trap clicked at {x}, {y} - Game Over!");
            cell.exploded = true;
            cell.revealed = true;
            RefreshVisuals();

            // Trigger game over through state manager
            GameStateManager.Instance?.LoseGame();
            return false;
        }

        RevealCell(cell);
        return true;
    }

    public bool HandleCellRightClick(int x, int y)
    {
        // Check game state before processing input
        if (!GameStateManager.Instance?.CanAcceptInput ?? true)
        {
            return false;
        }

        if (!isInitialized || isFloodFilling)
        {
            return false;
        }

        Cell cell = gameGrid.GetCell(x, y);
        if (cell == null || cell.revealed || cell.IsProtected)
        {
            return false;
        }

        // Don't allow flagging before traps are generated
        if (!gameGrid.TrapsGenerated)
        {
            Debug.Log("Cannot flag cells before first click");
            return false;
        }

        cell.flagged = !cell.flagged;
        Debug.Log($"Cell at {x}, {y} flag toggled to: {cell.flagged}");
        RefreshVisuals();
        return true;
    }

    private void RevealCell(Cell cell)
    {
        if (cell == null || cell.revealed || cell.type == Cell.CellType.Trap || cell.IsProtected)
        {
            return;
        }

        Debug.Log($"Revealing cell at {cell.position} - Type: {cell.type}, Number: {cell.number}");

        if (cell.type == Cell.CellType.Empty)
        {
            Debug.Log($"Starting smooth flood fill from {cell.position}");
            visitedCells = new HashSet<Cell>();
            StartCoroutine(SmoothFloodFill(cell));
        }
        else
        {
            cell.revealed = true;
            RefreshVisuals();
        }
    }

    private IEnumerator SmoothFloodFill(Cell cell)
    {
        isFloodFilling = true;
        Debug.Log("Flood fill started - game cannot be paused during this operation");

        yield return StartCoroutine(Flood(cell));

        isFloodFilling = false;
        Debug.Log("Smooth flood fill complete - game can be paused again");

        // Check win condition after flood fill completes
        CheckWinCondition();
    }

    private IEnumerator Flood(Cell cell)
    {
        if (cell == null || cell.revealed || cell.type == Cell.CellType.Trap || cell.IsProtected || visitedCells.Contains(cell))
        {
            yield break;
        }

        visitedCells.Add(cell);
        cell.revealed = true;
        RefreshVisuals();
        Debug.Log($"Revealed cell at {cell.position} - Type: {cell.type}, Number: {cell.number}");

        yield return new WaitForSeconds(revealAnimationDuration);

        if (cell.type == Cell.CellType.Empty)
        {
            yield return StartCoroutine(FloodAdjacentCells(cell.position));
        }
    }

    private IEnumerator FloodAdjacentCells(Vector3Int position)
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
                        yield return StartCoroutine(Flood(adjacent));
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
                    yield return StartCoroutine(Flood(adjacent));
                }
            }
        }
    }

    public bool HandleChordClick(int x, int y)
    {
        // Check game state before processing input
        if (!GameStateManager.Instance?.CanAcceptInput ?? true)
        {
            return false;
        }

        if (!isInitialized || isFloodFilling)
        {
            return false;
        }

        // Don't allow chord clicking before traps are generated
        if (!gameGrid.TrapsGenerated)
        {
            Debug.Log("Cannot chord click before first click");
            return false;
        }

        Cell cell = gameGrid.GetCell(x, y);
        if (cell == null || !cell.revealed || cell.type != Cell.CellType.Number)
        {
            return false;
        }

        int adjacentFlags = gameGrid.CountAdjacentFlags(cell);
        if (adjacentFlags != cell.number)
        {
            Debug.Log($"Chord failed: {adjacentFlags} flags, need {cell.number}");
            return false;
        }

        Debug.Log($"Performing chord click at {x}, {y}");
        var adjacentCells = gameGrid.GetAdjacentCells(cell);
        bool hitTrap = false;

        foreach (Cell adjacent in adjacentCells)
        {
            if (!adjacent.revealed && !adjacent.flagged && !adjacent.IsProtected)
            {
                if (adjacent.type == Cell.CellType.Trap)
                {
                    adjacent.exploded = true;
                    adjacent.revealed = true;
                    hitTrap = true;
                    Debug.Log($"Chord revealed trap at {adjacent.position}");
                }
                else
                {
                    RevealCell(adjacent);
                }
            }
        }

        RefreshVisuals();

        if (hitTrap)
        {
            GameStateManager.Instance?.LoseGame();
        }

        return !hitTrap;
    }

    public bool CheckWinCondition()
    {
        // Only check win condition during active gameplay
        if (!GameStateManager.Instance?.IsGameActive ?? false)
            return false;

        if (!gameGrid.TrapsGenerated)
            return false;

        var allCells = gameGrid.GetAllCells();

        // Count different cell states
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

        // Win condition 1: All traps are flagged
        bool allTrapsFlagged = (totalTraps > 0 && flaggedTraps == totalTraps);

        // Win condition 2: All non-trap cells are revealed
        bool allNonTrapsRevealed = (unrevealedNonTraps == 0);

        if (allTrapsFlagged || allNonTrapsRevealed)
        {
            Debug.Log($"Game Won! Condition: {(allTrapsFlagged ? "All traps flagged" : "All non-traps revealed")}");

            // Auto-flag any unflagged traps when winning
            AutoFlagRemainingTraps();

            // Trigger win through state manager
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
                Debug.Log($"Auto-flagged trap at {cell.position}");
            }
        }

        if (anyTrapsAutoFlagged)
        {
            RefreshVisuals();
            Debug.Log("All remaining traps have been auto-flagged!");
        }
    }

    /// <summary>
    /// Reveals all traps on the board (used for game over)
    /// </summary>
    private void RevealAllTraps()
    {
        if (gameGrid == null)
            return;

        var trapCells = gameGrid.GetCellsByType(Cell.CellType.Trap);
        foreach (Cell trap in trapCells)
        {
            trap.revealed = true;
        }

        RefreshVisuals();
        Debug.Log("All traps revealed");
    }

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

    /// <summary>
    /// Override the trap density for future games
    /// </summary>
    /// <param name="density">New trap density (0–1)</param>
    public void OverrideTrapDensity(float density)
    {
        // Clamp to valid range
        gridConfig.trapDensity = Mathf.Clamp01(density);
        // Mark board so it will be reinitialized with new density
        isInitialized = false;
        Debug.Log($"Trap density overridden to {gridConfig.trapDensity}");
    }

    public void ResetBoard()
    {
        if (gameGrid != null)
        {
            gameGrid.Reset();
            isFloodFilling = false;
            visitedCells = null;
            RefreshVisuals();
            Debug.Log("Board reset - ready for first click");
        }
    }
}