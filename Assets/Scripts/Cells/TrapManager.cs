using System.Collections.Generic;
using UnityEngine;

public static class TrapManager
{
    public static void PlaceTraps(GameBoard board, int trapCount)
    {
        List<GridCell> allCells = new();
        for (int x = 0; x < board.levelConfig.Width; x++)
        {
            for (int y = 0; y < board.levelConfig.Height; y++)
            {
                allCells.Add(board.GetCell(x, y));
            }
        }

        for (int i = 0; i < trapCount; i++)
        {
            int index = Random.Range(0, allCells.Count);
            GridCell trapCell = allCells[index];
            trapCell.IsTrapped = true;
            allCells.RemoveAt(index);
        }

        // Count adjacent traps
        for (int x = 0; x < board.levelConfig.Width; x++)
        {
            for (int y = 0; y < board.levelConfig.Height; y++)
            {
                GridCell cell = board.GetCell(x, y);
                if (cell.IsTrapped) continue;

                int count = 0;
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        GridCell neighbor = board.GetCell(x + dx, y + dy);
                        if (neighbor != null && neighbor.IsTrapped) count++;
                    }
                }

                cell.AdjacentTrapCount = count;
            }
        }
    }
}
