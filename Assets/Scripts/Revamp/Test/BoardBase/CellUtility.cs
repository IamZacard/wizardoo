using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CellUtility
{
    public static int CountAdjacentTraps(Cell cell, GameCellGrid cellGrid)
    {
        int count = 0;

        for (int adjacentX = -1; adjacentX <= 1; adjacentX++)
        {
            for (int adjacentY = -1; adjacentY <= 1; adjacentY++)
            {
                if (adjacentX == 0 && adjacentY == 0)
                {
                    continue;
                }

                int x = cell.position.x + adjacentX;
                int y = cell.position.y + adjacentY;

                if (cellGrid.TryGetCell(x, y, out Cell adjacent) && adjacent.type == Cell.Type.Trap)
                {
                    count++;
                }
            }
        }

        return count;
    }

    public static int CountAdjacentFlags(Cell cell, CellGrid cellGrid)
    {
        int count = 0;

        for (int adjacentX = -1; adjacentX <= 1; adjacentX++)
        {
            for (int adjacentY = -1; adjacentY <= 1; adjacentY++)
            {
                if (adjacentX == 0 && adjacentY == 0)
                {
                    continue;
                }

                int x = cell.position.x + adjacentX;
                int y = cell.position.y + adjacentY;

                if (cellGrid.TryGetCell(x, y, out Cell adjacent) && !adjacent.revealed && adjacent.flagged)
                {
                    count++;
                }
            }
        }

        return count;
    }

    public static bool IsAdjacent(Cell a, Cell b)
    {
        return Mathf.Abs(a.position.x - b.position.x) <= 1 &&
               Mathf.Abs(a.position.y - b.position.y) <= 1;
    }    
}

