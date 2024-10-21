using UnityEngine;

public class CellGrid
{
    private readonly Cell[,] cells;

    public int Width => cells.GetLength(0);
    public int Height => cells.GetLength(1);

    public Cell this[int x, int y] => cells[x, y];

    public CellGrid(int width, int height)
    {
        cells = new Cell[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                cells[x, y] = new Cell
                {
                    position = new Vector3Int(x, y, 0),
                    type = Cell.Type.Empty
                };
            }
        }
    }

    public void GenerateTraps(Cell startingCell, int amount)
    {
        int width = Width;
        int height = Height;

        for (int i = 0; i < amount; i++)
        {
            int x, y;
            Cell cell;

            // Keep generating random positions until a suitable position is found
            do
            {
                x = Random.Range(0, width);
                y = Random.Range(0, height);
                cell = cells[x, y];
            }
            while (cell.type == Cell.Type.Trap || IsAdjacent(startingCell, cell) || cell.type == Cell.Type.Pillar || cell.type == Cell.Type.Shrine);

            // Assign the cell as a mine
            cell.type = Cell.Type.Trap;
        }
    }

    public void GenerateNumbers()
    {
        int width = Width;
        int height = Height;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Cell cell = cells[x, y];

                if (cell.type == Cell.Type.Trap)
                {
                    continue; // Skip generating numbers on mines
                }

                cell.number = CountAdjacentTraps(cell);
                cell.type = cell.number > 0 ? Cell.Type.Number : Cell.Type.Empty;
            }
        }
    }


    public int CountAdjacentTraps(Cell cell)
    {
        int count = 0;

        for (int adjacentX = -1; adjacentX <= 1; adjacentX++)
        {
            for (int adjacentY = -1; adjacentY <= 1; adjacentY++)
            {
                if (adjacentX == 0 && adjacentY == 0) {
                    continue;
                }

                int x = cell.position.x + adjacentX;
                int y = cell.position.y + adjacentY;

                if (TryGetCell(x, y, out Cell adjacent) && adjacent.type == Cell.Type.Trap) {
                    count++;
                }
            }
        }

        return count;
    }

    public int CountAdjacentFlags(Cell cell)
    {
        int count = 0;

        for (int adjacentX = -1; adjacentX <= 1; adjacentX++)
        {
            for (int adjacentY = -1; adjacentY <= 1; adjacentY++)
            {
                if (adjacentX == 0 && adjacentY == 0) {
                    continue;
                }

                int x = cell.position.x + adjacentX;
                int y = cell.position.y + adjacentY;

                if (TryGetCell(x, y, out Cell adjacent) && !adjacent.revealed && adjacent.flagged) {
                    count++;
                }
            }
        }

        return count;
    }

    public Cell GetCell(int x, int y)
    {
        if (InBounds(x, y)) {
            return cells[x, y];
        } else {
            return null;
        }
    }

    public bool TryGetCell(int x, int y, out Cell cell)
    {
        cell = GetCell(x, y);
        return cell != null;
    }

    public bool InBounds(int x, int y)
    {
        return x >= 0 && x < Width && y >= 0 && y < Height;
    }

    public bool IsAdjacent(Cell a, Cell b)
    {
        return Mathf.Abs(a.position.x - b.position.x) <= 1 &&
               Mathf.Abs(a.position.y - b.position.y) <= 1;
    }

}
