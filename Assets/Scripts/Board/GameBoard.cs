using UnityEngine;

public class GameBoard : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private GridConfig gridConfig;

    [Header("Components")]
    [SerializeField] private BoardRenderer boardRenderer;

    private GameGrid gameGrid;
    private bool isInitialized;
    private bool gameStarted;

    public GameGrid Grid => gameGrid;
    public BoardRenderer Renderer => boardRenderer;
    public bool IsInitialized => isInitialized;
    public bool GameStarted => gameStarted;

    private void Awake()
    {
        Debug.Log("GameBoard Awake");
        if (boardRenderer == null)
        {
            boardRenderer = GetComponentInChildren<BoardRenderer>();
            Debug.Log($"BoardRenderer found in children: {boardRenderer != null}");
        }
    }

    private void Start()
    {
        Debug.Log("GameBoard Start");
        InitializeBoard();
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
        gameStarted = false;
        Debug.Log("Board initialization complete");
    }

    public void StartNewGame(Vector3Int startPosition)
    {
        Debug.Log($"Starting new game at position {startPosition}");

        if (!isInitialized)
        {
            InitializeBoard();
        }

        gameGrid.Reset();

        int trapCount = Mathf.RoundToInt(gridConfig.width * gridConfig.height * gridConfig.trapDensity);
        Debug.Log($"Placing {trapCount} traps");

        gameGrid.GenerateTraps(startPosition, trapCount);
        boardRenderer.DrawGrid();

        gameStarted = true;
        Debug.Log("New game started successfully");
    }

    /// <summary>
    /// Handles cell click - reveals cell and triggers flood fill if appropriate
    /// </summary>
    /// <param name="x">Grid X coordinate</param>
    /// <param name="y">Grid Y coordinate</param>
    /// <returns>True if click was valid, false if game over or invalid click</returns>
    public bool HandleCellClick(int x, int y)
    {
        if (!gameStarted || !isInitialized)
        {
            Debug.LogWarning("Game not started or not initialized");
            return false;
        }

        Cell cell = gameGrid.GetCell(x, y);
        if (cell == null)
        {
            Debug.LogWarning($"Invalid cell position: {x}, {y}");
            return false;
        }

        // Can't click on already revealed cells or flagged cells
        if (cell.revealed || cell.flagged)
        {
            Debug.Log($"Cell at {x}, {y} is already revealed or flagged");
            return false;
        }

        // Can't click on protected cells (pillars, shrines)
        if (cell.IsProtected)
        {
            Debug.Log($"Cell at {x}, {y} is protected");
            return false;
        }

        // First click - start game if not already started with traps
        if (!HasTrapsGenerated())
        {
            StartNewGame(new Vector3Int(x, y, 0));
            cell = gameGrid.GetCell(x, y); // Refresh cell reference after trap generation
        }

        // Check if clicked on trap
        if (cell.type == Cell.CellType.Trap)
        {
            Debug.Log($"Trap clicked at {x}, {y} - Game Over!");
            cell.exploded = true;
            cell.revealed = true;
            RefreshVisuals();
            // Handle game over logic here
            return false;
        }

        // Reveal the cell and potentially flood fill
        RevealCell(cell);

        RefreshVisuals();
        return true;
    }

    /// <summary>
    /// Handles cell right-click for flagging
    /// </summary>
    /// <param name="x">Grid X coordinate</param>
    /// <param name="y">Grid Y coordinate</param>
    /// <returns>True if flag was toggled successfully</returns>
    public bool HandleCellRightClick(int x, int y)
    {
        if (!gameStarted || !isInitialized)
        {
            return false;
        }

        Cell cell = gameGrid.GetCell(x, y);
        if (cell == null || cell.revealed || cell.IsProtected)
        {
            return false;
        }

        // Toggle flag
        cell.flagged = !cell.flagged;
        Debug.Log($"Cell at {x}, {y} flag toggled to: {cell.flagged}");

        RefreshVisuals();
        return true;
    }

    /// <summary>
    /// Reveals a cell and triggers flood fill if it's empty
    /// </summary>
    /// <param name="cell">Cell to reveal</param>
    private void RevealCell(Cell cell)
    {
        if (cell == null || cell.revealed || cell.type == Cell.CellType.Trap || cell.IsProtected) // Added cell.IsProtected check here too for consistency
        {
            return;
        }

        Debug.Log($"Revealing cell at {cell.position} - Type: {cell.type}, Number: {cell.number}");

        // If it's an empty cell (no adjacent traps), use flood fill
        if (cell.type == Cell.CellType.Empty)
        {
            Debug.Log($"Starting flood fill from {cell.position}");
            // The FloodFill method will handle setting 'revealed' for all cells it processes
            gameGrid.FloodFill(cell);
        }
        else
        {
            // Just reveal this single cell (it has a number)
            cell.revealed = true;
        }
    }

    /// <summary>
    /// Handles middle-click or chord click (reveal adjacent cells when flags match number)
    /// </summary>
    /// <param name="x">Grid X coordinate</param>
    /// <param name="y">Grid Y coordinate</param>
    /// <returns>True if chord was successful, false if game over or invalid</returns>
    public bool HandleChordClick(int x, int y)
    {
        if (!gameStarted || !isInitialized)
        {
            return false;
        }

        Cell cell = gameGrid.GetCell(x, y);
        if (cell == null || !cell.revealed || cell.type != Cell.CellType.Number)
        {
            return false;
        }

        // Check if the number of adjacent flags equals the cell's number
        int adjacentFlags = gameGrid.CountAdjacentFlags(cell);
        if (adjacentFlags != cell.number)
        {
            Debug.Log($"Chord failed: {adjacentFlags} flags, need {cell.number}");
            return false;
        }

        Debug.Log($"Performing chord click at {x}, {y}");

        // Reveal all unflagged adjacent cells
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
        return !hitTrap;
    }

    /// <summary>
    /// Checks if the game has been won (all non-trap cells revealed)
    /// </summary>
    /// <returns>True if game is won</returns>
    public bool CheckWinCondition()
    {
        if (!gameStarted)
            return false;

        var allCells = gameGrid.GetAllCells();
        foreach (Cell cell in allCells)
        {
            // If there's an unrevealed cell that's not a trap and not protected, game isn't won
            if (!cell.revealed && cell.type != Cell.CellType.Trap && !cell.IsProtected)
            {
                return false;
            }
        }

        Debug.Log("Game Won!");
        return true;
    }

    /// <summary>
    /// Gets cell at world position (useful for mouse input)
    /// </summary>
    /// <param name="worldPos">World position</param>
    /// <returns>Cell at that position, or null if invalid</returns>
    public Cell GetCellAtWorldPosition(Vector3 worldPos)
    {
        return boardRenderer.GetCellAtWorldPosition(worldPos);
    }

    /// <summary>
    /// Checks if traps have been generated (game has started)
    /// </summary>
    /// <returns>True if traps exist on the board</returns>
    private bool HasTrapsGenerated()
    {
        if (gameGrid == null)
            return false;

        var trapCells = gameGrid.GetCellsByType(Cell.CellType.Trap);
        return trapCells.Count > 0;
    }

    public void RefreshVisuals()
    {
        if (isInitialized)
        {
            boardRenderer.DrawGrid();
        }
    }

    // Optional setters if you want to override config at runtime
    public void OverrideGridSize(int w, int h)
    {
        gridConfig.width = w;
        gridConfig.height = h;
        isInitialized = false;
    }

    public void OverrideTrapDensity(float density)
    {
        gridConfig.trapDensity = Mathf.Clamp01(density);
    }

    /// <summary>
    /// Reset the board for a new game
    /// </summary>
    public void ResetBoard()
    {
        if (gameGrid != null)
        {
            gameGrid.Reset();
            gameStarted = false;
            RefreshVisuals();
            Debug.Log("Board reset");
        }
    }
}