// Grid management system
using System.Collections.Generic;
using UnityEngine;

public class GameGrid
{
    private Cell[,] cells;
    private readonly int width;
    private readonly int height;

    public int Width => width;
    public int Height => height;
    public Cell this[int x, int y] => GetCell(x, y);

    public GameGrid(int w, int h)
    {
        width = w;
        height = h;
        InitializeGrid();
    }

    private void InitializeGrid()
    {
        cells = new Cell[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                cells[x, y] = new Cell(new Vector3Int(x, y, 0));
            }
        }
    }

    public Cell GetCell(int x, int y)
    {
        return IsValidPosition(x, y) ? cells[x, y] : null;
    }

    public bool TryGetCell(int x, int y, out Cell cell)
    {
        cell = GetCell(x, y);
        return cell != null;
    }

    public bool IsValidPosition(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    // Set special cell types (pillars, shrines)
    public void SetCellType(int x, int y, Cell.CellType type)
    {
        if (TryGetCell(x, y, out Cell cell))
        {
            cell.type = type;
        }
    }

    // Trap generation
    public void GenerateTraps(Vector3Int startPosition, int trapCount)
    {
        List<Cell> availableCells = GetAvailableCellsForTraps(startPosition);

        if (availableCells.Count < trapCount)
        {
            Debug.LogWarning($"Not enough valid positions for {trapCount} traps. Available: {availableCells.Count}");
            trapCount = availableCells.Count;
        }

        // Shuffle and place traps
        for (int i = 0; i < trapCount; i++)
        {
            int randomIndex = Random.Range(0, availableCells.Count);
            Cell selectedCell = availableCells[randomIndex];

            selectedCell.type = Cell.CellType.Trap;
            availableCells.RemoveAt(randomIndex);
        }

        GenerateNumbers();
    }

    private List<Cell> GetAvailableCellsForTraps(Vector3Int startPosition)
    {
        List<Cell> available = new List<Cell>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Cell cell = cells[x, y];

                // Skip if can't place trap or adjacent to start position
                if (!cell.CanPlaceTrap || IsAdjacent(cell.position, startPosition))
                    continue;

                available.Add(cell);
            }
        }

        return available;
    }

    // Number generation
    public void GenerateNumbers()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Cell cell = cells[x, y];

                if (cell.type == Cell.CellType.Trap || cell.IsProtected)
                    continue;

                int adjacentTraps = CountAdjacentTraps(cell);
                cell.number = adjacentTraps;
                cell.type = adjacentTraps > 0 ? Cell.CellType.Number : Cell.CellType.Empty;
            }
        }
    }

    public int CountAdjacentTraps(Cell cell)
    {
        return CountAdjacentCells(cell, c => c.type == Cell.CellType.Trap);
    }

    public int CountAdjacentFlags(Cell cell)
    {
        return CountAdjacentCells(cell, c => c.flagged && !c.revealed);
    }

    private int CountAdjacentCells(Cell cell, System.Func<Cell, bool> condition)
    {
        int count = 0;

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;

                int x = cell.position.x + dx;
                int y = cell.position.y + dy;

                if (TryGetCell(x, y, out Cell adjacent) && condition(adjacent))
                {
                    count++;
                }
            }
        }

        return count;
    }

    public List<Cell> GetAdjacentCells(Cell cell)
    {
        List<Cell> adjacent = new List<Cell>();

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;

                int x = cell.position.x + dx;
                int y = cell.position.y + dy;

                if (TryGetCell(x, y, out Cell adjacentCell))
                {
                    adjacent.Add(adjacentCell);
                }
            }
        }

        return adjacent;
    }

    public bool IsAdjacent(Vector3Int pos1, Vector3Int pos2)
    {
        return Mathf.Abs(pos1.x - pos2.x) <= 1 && Mathf.Abs(pos1.y - pos2.y) <= 1;
    }

    // Utility methods for game state
    public List<Cell> GetAllCells()
    {
        List<Cell> allCells = new List<Cell>();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                allCells.Add(cells[x, y]);
            }
        }
        return allCells;
    }

    public List<Cell> GetCellsByType(Cell.CellType type)
    {
        return GetAllCells().FindAll(cell => cell.type == type);
    }

    public void FloodFill(Cell startCell)
    {
        // Initial checks: If it's a trap, protected, or null, we can't flood from it.
        // We remove 'startCell.revealed' from this initial check, as the flood fill
        // itself will mark cells as revealed as it processes them.
        if (startCell == null || startCell.type == Cell.CellType.Trap || startCell.IsProtected)
        {
            Debug.Log($"FloodFill aborted - invalid starting conditions for {startCell?.position}");
            return;
        }

        Debug.Log($"Starting flood fill from {startCell.position}");

        Queue<Cell> queue = new Queue<Cell>();
        // Using a HashSet for 'visited' is smart to prevent re-processing and infinite loops.
        HashSet<Cell> visited = new HashSet<Cell>();

        queue.Enqueue(startCell);
        visited.Add(startCell); // Mark the starting cell as visited immediately

        int cellsRevealed = 0;

        while (queue.Count > 0)
        {
            Cell current = queue.Dequeue();

            // If the current cell is already revealed, or is a trap/protected, skip it.
            // This is important for cells added to the queue that might be number cells adjacent to empty cells.
            if (current.revealed || current.type == Cell.CellType.Trap || current.IsProtected)
            {
                continue;
            }

            // Reveal the current cell (this is where it actually gets marked revealed)
            current.revealed = true;
            cellsRevealed++;

            Debug.Log($"Revealed cell at {current.position} - Type: {current.type}, Number: {current.number}");

            // If it's an empty cell (no adjacent traps), continue flooding to adjacent cells
            if (current.type == Cell.CellType.Empty)
            {
                AddAdjacentCellsToFloodQueue(current, queue, visited);
            }
            // If it's a number cell, it's revealed, but we don't continue flooding *from* it.
            // Its function is to be the boundary of the flood.
        }

        Debug.Log($"Flood fill complete - revealed {cellsRevealed} cells");
    }

    private void AddAdjacentCellsToFloodQueue(Cell cell, Queue<Cell> queue, HashSet<Cell> visited)
    {
        // Check all 8 adjacent cells (including diagonals for minesweeper-style flood fill)
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue; // Skip the center cell

                int x = cell.position.x + dx;
                int y = cell.position.y + dy;

                if (TryGetCell(x, y, out Cell adjacent))
                {
                    // Only add to queue if not already visited, not a trap, and not protected.
                    // We don't check 'adjacent.revealed' here because we want to add
                    // number cells that are not yet revealed and will be revealed by this flood.
                    if (!visited.Contains(adjacent) &&
                        adjacent.type != Cell.CellType.Trap && !adjacent.IsProtected)
                    {
                        queue.Enqueue(adjacent);
                        visited.Add(adjacent); // Mark as visited to avoid re-adding
                    }
                }
            }
        }
    }

    // Alternative flood fill that only checks 4-directional (up, down, left, right)
    // Use this if you want more traditional flood fill behavior
    public void FloodFill4Direction(Cell startCell)
    {
        if (startCell == null || startCell.revealed || startCell.type == Cell.CellType.Trap || startCell.IsProtected)
        {
            return;
        }

        Queue<Cell> queue = new Queue<Cell>();
        HashSet<Cell> visited = new HashSet<Cell>();

        queue.Enqueue(startCell);
        visited.Add(startCell);

        while (queue.Count > 0)
        {
            Cell current = queue.Dequeue();

            if (current.revealed || current.type == Cell.CellType.Trap || current.IsProtected)
                continue;

            current.revealed = true;

            if (current.type == Cell.CellType.Empty)
            {
                AddAdjacent4DirectionToQueue(current, queue, visited);
            }
        }
    }

    private void AddAdjacent4DirectionToQueue(Cell cell, Queue<Cell> queue, HashSet<Cell> visited)
    {
        Vector3Int position = cell.position;

        // Check 4 directions: left, right, down, up
        int[,] directions = { { -1, 0 }, { 1, 0 }, { 0, -1 }, { 0, 1 } };

        for (int i = 0; i < 4; i++)
        {
            int x = position.x + directions[i, 0];
            int y = position.y + directions[i, 1];

            if (TryGetCell(x, y, out Cell adjacent))
            {
                if (!visited.Contains(adjacent) && !adjacent.revealed &&
                    adjacent.type != Cell.CellType.Trap && !adjacent.IsProtected)
                {
                    queue.Enqueue(adjacent);
                    visited.Add(adjacent);
                }
            }
        }
    }

    public void Reset()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Cell cell = cells[x, y];
                if (!cell.IsProtected) // Keep pillars and shrines
                {
                    cell.type = Cell.CellType.Empty;
                    cell.number = 0;
                }
                cell.revealed = false;
                cell.flagged = false;
                cell.exploded = false;
            }
        }
    }
}