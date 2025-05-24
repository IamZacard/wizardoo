using System.Collections.Generic;
using UnityEngine;

public static class CellRevealSystem
{
    private static GameBoard board;

    public static void Setup(GameBoard gameBoard)
    {
        board = gameBoard;
    }

    public static void RevealCells(GameBoard board, GridCell startCell)
    {
        if (startCell.AdjacentTrapCount > 0)
        {
            startCell.Reveal();
            return;
        }

        Queue<GridCell> queue = new Queue<GridCell>();
        HashSet<GridCell> visited = new HashSet<GridCell>();

        queue.Enqueue(startCell);
        visited.Add(startCell);

        while (queue.Count > 0)
        {
            var cell = queue.Dequeue();
            if (!cell.IsRevealed && !cell.IsFlagged)
                cell.Reveal();

            if (cell.AdjacentTrapCount == 0)
            {
                foreach (var neighbor in GetNeighbors(cell))
                {
                    if (!visited.Contains(neighbor) && !neighbor.IsTrapped)
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }
    }

    private static List<GridCell> GetNeighbors(GridCell cell)
    {
        List<GridCell> neighbors = new();
        int[] dx = { -1, 0, 1 };
        int[] dy = { -1, 0, 1 };

        foreach (var x in dx)
        {
            foreach (var y in dy)
            {
                if (x == 0 && y == 0) continue;

                var neighbor = board.GetCell(cell.X + x, cell.Y + y);
                if (neighbor != null) neighbors.Add(neighbor);
            }
        }

        return neighbors;
    }
}
