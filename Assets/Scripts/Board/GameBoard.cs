using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening; // Ensure DOTween is imported

public class GameBoard : MonoBehaviour
{
    public static GameBoard Instance { get; private set; } // Singleton for easy access

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
    // Visited cells are only relevant during a single flood fill operation, so no need to be a class member
    // private HashSet<Cell> visitedCells; 

    public GameGrid Grid => gameGrid;
    public BoardRenderer Renderer => boardRenderer;
    public bool IsInitialized => isInitialized;
    public bool IsFloodFilling => isFloodFilling; // Expose this property

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Or manage lifecycle based on your game flow
        }
        else
        {
            Destroy(gameObject);
        }

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
        boardRenderer.DrawGrid(); // Initial draw of unknown tiles

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
        boardRenderer.DrawGrid(); // Redraw grid to reset visuals

        Debug.Log("New game started - traps will be generated on first reveal");
    }

    // New method: Character steps on a cell, reveals it
    public void HandleCharacterStep(Vector3Int characterGridPosition)
    {
        // Check game state before processing
        if (!GameStateManager.Instance?.CanAcceptInput ?? true)
        {
            Debug.LogWarning("Cannot handle character step - game state doesn't allow input");
            return;
        }

        if (!isInitialized || isFloodFilling)
        {
            Debug.LogWarning($"Cannot handle character step: IsInitialized={isInitialized}, IsFloodFilling={isFloodFilling}");
            return;
        }

        Cell steppedCell = gameGrid.GetCell(characterGridPosition.x, characterGridPosition.y);
        if (steppedCell == null || steppedCell.flagged || steppedCell.IsProtected)
        {
            // Character cannot step on flagged or protected cells to reveal them
            Debug.LogWarning($"Character stepped on invalid cell at {characterGridPosition.x}, {characterGridPosition.y}");
            return;
        }

        // Generate traps on the very first "reveal" action if not already generated
        if (!gameGrid.TrapsGenerated)
        {
            GenerateTrapsAfterFirstReveal(characterGridPosition);
            // Get the cell again after trap generation as its properties may have changed
            steppedCell = gameGrid.GetCell(characterGridPosition.x, characterGridPosition.y);
        }

        // Check if stepped cell is a trap
        if (steppedCell.type == Cell.CellType.Trap)
        {
            Debug.Log($"Character stepped on trap at {steppedCell.position} - Game Over!");
            steppedCell.exploded = true;
            steppedCell.revealed = true;
            boardRenderer.DrawCell(steppedCell); // Update visuals immediately
            GameStateManager.Instance?.LoseGame(); // Trigger game over
            return;
        }

        // If it's an unrevealed cell, reveal it
        if (!steppedCell.revealed)
        {
            RevealCellLogic(steppedCell);
        }
    }

    // New method: Toggle cell flag with RMB
    public void HandleCellFlag(Vector3Int cellPosition, GameObject flagEffectPrefab)
    {
        // Player input for movement and most other character actions (e.g., using abilities, flagging cells) are temporarily suspended.
        if (!GameStateManager.Instance?.CanAcceptInput ?? true)
        {
            Debug.LogWarning("Cannot handle flag input - game state doesn't allow input");
            return;
        }

        if (!isInitialized || isFloodFilling)
        {
            Debug.LogWarning($"Cannot handle flag: IsInitialized={isInitialized}, IsFloodFilling={isFloodFilling}");
            return;
        }

        Cell cell = gameGrid.GetCell(cellPosition.x, cellPosition.y);
        if (cell == null || cell.revealed || cell.IsProtected) // Cannot flag revealed or protected cells
        {
            Debug.LogWarning($"Cannot flag invalid cell at {cellPosition.x}, {cellPosition.y}");
            return;
        }

        // Don't allow flagging before traps are generated (game hasn't truly started yet)
        if (!gameGrid.TrapsGenerated)
        {
            Debug.Log("Cannot flag cells before first reveal");
            return;
        }

        cell.flagged = !cell.flagged; // Toggle the flag state
        Debug.Log($"Cell at {cellPosition.x}, {cellPosition.y} flag toggled to: {cell.flagged}");

        boardRenderer.DrawCell(cell); // Update visuals for this specific cell

        // Trigger flag effect if applicable
        if (cell.flagged && flagEffectPrefab != null)
        {
            // You might want to pool this effect or handle its lifecycle
            Vector3 worldPos = boardRenderer.GetWorldPosition(cell) + new Vector3(0.5f, 0.5f, 0);
            Instantiate(flagEffectPrefab, worldPos, Quaternion.identity);
        }

        CheckWinCondition(); // Re-check win condition after flagging
    }

    // New method: Reveal cells around a position (for initial character spawn vision)
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
                    // This is for initial reveal, so we assume no traps are generated yet.
                    // If this is called after traps are generated, then a trap could be revealed here.
                    // For initial reveal, ensure traps aren't here. This is why GenerateTrapsAfterFirstReveal is important.
                    cell.revealed = true;
                    boardRenderer.DrawCell(cell); // Update visuals immediately
                }
            }
        }
    }

    private void GenerateTrapsAfterFirstReveal(Vector3Int firstRevealPosition)
    {
        // Check if we can accept input (game state check)
        if (!GameStateManager.Instance?.CanAcceptInput ?? false)
        {
            return;
        }

        int trapCount = Mathf.RoundToInt(gridConfig.width * gridConfig.height * gridConfig.trapDensity);
        Debug.Log($"Generating {trapCount} traps after first reveal at {firstRevealPosition}");

        gameGrid.GenerateTraps(firstRevealPosition, trapCount);
        boardRenderer.DrawGrid(); // Redraw entire grid to ensure numbers appear correctly

        Debug.Log("Traps generated successfully");
    }

    // Renamed from HandleCellClick to differentiate, and adjusted logic
    public bool HandleManualCellClick(int x, int y) // This might be used for chord clicking or debugging
    {
        // Check game state before processing input
        if (!GameStateManager.Instance?.CanAcceptInput ?? true)
        {
            Debug.LogWarning("Cannot handle manual click - game state doesn't allow input");
            return false;
        }

        if (!isInitialized || isFloodFilling)
        {
            Debug.LogWarning($"Cannot handle manual click: IsInitialized={isInitialized}, IsFloodFilling={isFloodFilling}");
            return false;
        }

        Cell cell = gameGrid.GetCell(x, y);
        if (cell == null || cell.revealed || cell.flagged || cell.IsProtected)
        {
            Debug.LogWarning($"Invalid manual cell click at {x}, {y}");
            return false;
        }

        // If traps haven't been generated, this click is the first "interaction"
        if (!gameGrid.TrapsGenerated)
        {
            GenerateTrapsAfterFirstReveal(new Vector3Int(x, y, 0));
            cell = gameGrid.GetCell(x, y); // Get the cell again after trap generation
        }

        // Check if clicked cell is a trap
        if (cell.type == Cell.CellType.Trap)
        {
            Debug.Log($"Trap clicked at {x}, {y} - Game Over!");
            cell.exploded = true;
            cell.revealed = true;
            boardRenderer.DrawCell(cell); // Update visuals immediately
            GameStateManager.Instance?.LoseGame();
            return false;
        }

        RevealCellLogic(cell);
        return true;
    }

    // Consolidated reveal logic
    private void RevealCellLogic(Cell cell)
    {
        if (cell == null || cell.revealed || cell.type == Cell.CellType.Trap || cell.IsProtected || cell.flagged)
        {
            // Do not reveal if already revealed, is a trap, protected, or flagged.
            return;
        }

        Debug.Log($"Attempting to reveal cell at {cell.position} - Type: {cell.type}, Number: {cell.number}");

        if (cell.type == Cell.CellType.Empty)
        {
            Debug.Log($"Starting smooth flood fill from {cell.position}");
            // Reset visitedCells for each new flood fill operation
            StartCoroutine(SmoothFloodFill(cell, new HashSet<Cell>()));
        }
        else
        {
            cell.revealed = true;
            boardRenderer.DrawCell(cell); // Update visuals for this specific cell
            CheckWinCondition(); // Check win condition immediately after revealing
        }
    }

    private IEnumerator SmoothFloodFill(Cell startCell, HashSet<Cell> visited)
    {
        isFloodFilling = true;
        Debug.Log("Flood fill started - game cannot be paused during this operation");

        yield return StartCoroutine(Flood(startCell, visited));

        isFloodFilling = false;
        Debug.Log("Smooth flood fill complete - game can be paused again");

        // Check win condition after flood fill completes
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
        boardRenderer.DrawCell(cell); // Update visuals for this cell
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
            boardRenderer.DrawGrid(); // Redraw entire grid to show new flags
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

        boardRenderer.DrawGrid(); // Redraw grid to show revealed traps
        Debug.Log("All traps revealed");
    }

    public Cell GetCellAtWorldPosition(Vector3 worldPos)
    {
        return boardRenderer.GetCellAtWorldPosition(worldPos);
    }

    public void RefreshVisuals()
    {
        // This method can be kept for a full redraw if needed, but DrawCell is more efficient
        if (isInitialized)
        {
            boardRenderer.DrawGrid();
        }
    }

    public void OverrideGridSize(int w, int h)
    {
        gridConfig.width = w;
        gridConfig.height = h;
        isInitialized = false; // Force re-initialization on next game start
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
            // No need to reset visitedCells as it's a local variable in flood fill methods
            boardRenderer.DrawGrid(); // Redraw grid
            Debug.Log("Board reset - ready for first reveal");
        }
    }
}