using UnityEngine;

public class Test : MonoBehaviour
{
    [SerializeField] private GridConfig gridConfig;
    private GameBoard gameBoard;
    private Camera mainCamera;

    void Start()
    {
        gameBoard = FindObjectOfType<GameBoard>();
        mainCamera = Camera.main;

        if (mainCamera != null && gridConfig != null)
        {
            mainCamera.transform.position = new Vector3(gridConfig.width / 2f, gridConfig.height / 2f, -10f);
        }

        if (gameBoard != null)
        {
            Invoke(nameof(StartTestGame), 0.1f);
        }
    }

    void StartTestGame()
    {
        gameBoard.StartNewGame();
    }

    void Update()
    {
        // Left click to reveal cell
        if (Input.GetMouseButtonDown(0))
        {
            RevealCellAtMouse();
        }

        // Right click to flag cell
        if (Input.GetMouseButtonDown(1))
        {
            FlagCellAtMouse();
        }

        // R key to restart
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("Restarting game...");
            gameBoard?.StartNewGame();
        }

        // T key to test tile placement
        if (Input.GetKeyDown(KeyCode.T))
        {
            TestTilePlacement();
        }

        // Space - reveal all (cheat for testing)
        if (Input.GetKeyDown(KeyCode.Space) && Input.GetKey(KeyCode.LeftShift))
        {
            Debug.Log("Revealing all cells (cheat mode)");
            RevealAllCells();
        }
    }

    void TestTilePlacement()
    {
        Debug.Log("Testing tile placement...");

        if (gameBoard?.Renderer?.Tilemap != null)
        {
            // Try to place a tile directly
            var tilemap = gameBoard.Renderer.Tilemap;
            var testTile = gameBoard.Renderer.tileUnknown;

            if (testTile != null)
            {
                tilemap.SetTile(new Vector3Int(0, 0, 0), testTile);
                Debug.Log("Test tile placed at (0,0)");

                // Check if it was actually placed
                var placedTile = tilemap.GetTile(new Vector3Int(0, 0, 0));
                Debug.Log($"Tile at (0,0): {placedTile}");
            }
            else
            {
                Debug.LogError("Test tile is null!");
            }
        }
    }

    void RevealCellAtMouse()
    {
        Vector3 mousePos = Input.mousePosition;
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(mousePos);
        worldPos.z = 0;

        Cell cell = gameBoard.Renderer.GetCellAtWorldPosition(worldPos);

        if (cell != null)
        {
            int x = cell.position.x;
            int y = cell.position.y;
            gameBoard.HandleCellClick(x, y);
            gameBoard.CheckWinCondition();
        }
        else
        {
            Debug.Log("No cell found at mouse position");
        }
    }

    void FlagCellAtMouse()
    {
        Vector3 mousePos = Input.mousePosition;
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(mousePos);
        worldPos.z = 0;

        Cell cell = gameBoard.Renderer.GetCellAtWorldPosition(worldPos);

        if (cell != null && !cell.revealed)
        {
            cell.flagged = !cell.flagged;
            gameBoard.RefreshVisuals();

            Debug.Log($"Flagged cell at {cell.position} - Flagged: {cell.flagged}");
        }
    }

    #region debugging

    /// <summary>
    /// Reveals all traps on the board (used for game over)
    /// </summary>
    private void RevealAllTraps()
    {
        if (gameBoard.Grid == null)
            return;

        var trapCells = gameBoard.Grid.GetCellsByType(Cell.CellType.Trap);
        foreach (Cell trap in trapCells)
        {
            trap.revealed = true;
        }

        gameBoard.RefreshVisuals();
    }

    /// <summary>
    /// Reveals all cells (cheat/debug function)
    /// </summary>
    private void RevealAllCells()
    {
        if (gameBoard.Grid == null)
            return;

        var allCells = gameBoard.Grid.GetAllCells();
        foreach (Cell cell in allCells)
        {
            if (!cell.IsProtected)
            {
                cell.revealed = true;
            }
        }

        gameBoard.RefreshVisuals();
    }

    /// <summary>
    /// Shows debug information about the hovered cell
    /// </summary>
    private void ShowCellDebugInfo(Cell cell)
    {
        if (cell == null)
            return;

        Debug.Log($"Cell Info - Pos: {cell.position}, Type: {cell.type}, Number: {cell.number}, " +
                  $"Revealed: {cell.revealed}, Flagged: {cell.flagged}, Protected: {cell.IsProtected}");
    }
    #endregion
}