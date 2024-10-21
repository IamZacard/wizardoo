using UnityEngine;

public class GameCellGrid
{
    private readonly Cell[,] cells;

    public int Width => cells.GetLength(0);
    public int Height => cells.GetLength(1);

    public Cell this[int x, int y] => cells[x, y];

    public GameCellGrid(int width, int height)
    {
        cells = new Cell[width, height];
        InitializeCells();
    }

    private void InitializeCells()
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                cells[x, y] = new Cell
                {
                    position = new Vector3Int(x, y, 0),
                    type = Cell.Type.Empty
                };
            }
        }
    }

    public Cell GetCell(int x, int y)
    {
        if (InBounds(x, y))
        {
            return cells[x, y];
        }
        else
        {
            return null;
        }
    }

    public bool InBounds(int x, int y)
    {
        return x >= 0 && x < Width && y >= 0 && y < Height;
    }

    public bool TryGetCell(int x, int y, out Cell cell)
    {
        cell = GetCell(x, y);
        return cell != null;
    }
}
