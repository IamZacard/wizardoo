using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NumberGenerator
{
    private readonly GameCellGrid cellGrid;

    public NumberGenerator(GameCellGrid cellGrid)
    {
        this.cellGrid = cellGrid;
    }

    public void GenerateNumbers()
    {
        int width = cellGrid.Width;
        int height = cellGrid.Height;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Cell cell = cellGrid.GetCell(x, y);

                if (cell.type == Cell.Type.Trap)
                {
                    continue;
                }

                cell.number = CellUtility.CountAdjacentTraps(cell, cellGrid);
                cell.type = cell.number > 0 ? Cell.Type.Number : Cell.Type.Empty;
            }
        }
    }
}

