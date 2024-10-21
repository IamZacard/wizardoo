using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrapGenerator
{
    private readonly GameCellGrid cellGrid;

    public TrapGenerator(GameCellGrid cellGrid)
    {
        this.cellGrid = cellGrid;
    }

    public void GenerateTraps(Cell startingCell, int amount)
    {
        int width = cellGrid.Width;
        int height = cellGrid.Height;

        for (int i = 0; i < amount; i++)
        {
            int x, y;
            Cell cell;

            do
            {
                x = Random.Range(0, width);
                y = Random.Range(0, height);
                cell = cellGrid.GetCell(x, y);
            }
            while (cell.type == Cell.Type.Trap ||
                   CellUtility.IsAdjacent(startingCell, cell) ||
                   cell.type == Cell.Type.Pillar ||
                   cell.type == Cell.Type.Shrine);

            cell.type = Cell.Type.Trap;
        }
    }
}