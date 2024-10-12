using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class Board : MonoBehaviour
{
    public Tilemap tilemap { get; private set; }

    public Tile[] tileUnknownVariants;
    public AnimatedTile tileUnknown;
    public Tile tileFloor;
    public Tile tileEmpty;
    public Tile tileMine;
    public Tile tileExploded;
    public AnimatedTile tileFlag_anim;
    public Tile tileNum1;
    public Tile tileNum2;
    public Tile tileNum3;
    public Tile tileNum4;
    public Tile tileNum5;
    public Tile tileNum6;
    public Tile tileNum7;
    public Tile tileNum8;

    private HashSet<Vector3Int> pillarPositions = new HashSet<Vector3Int>();
    private HashSet<Vector3Int> shrinePositions = new HashSet<Vector3Int>();

    private void Awake()
    {
        tilemap = GetComponent<Tilemap>();
    }

    public void Draw(CellGrid grid)
    {
        int width = grid.Width;
        int height = grid.Height;

        // Get all game objects with the "Pillar" tag
        GameObject[] pillars = GameObject.FindGameObjectsWithTag("Pillar");
        pillarPositions.Clear();

        // Store the positions of all pillars
        foreach (GameObject pillar in pillars)
        {
            Vector3Int position = tilemap.WorldToCell(pillar.transform.position);
            pillarPositions.Add(position);
        }

        // Get all game objects with the "Shrine" tag
        GameObject[] shrines = GameObject.FindGameObjectsWithTag("Shrine");
        shrinePositions.Clear();

        // Store the positions of all shrines
        foreach (GameObject shrine in shrines)
        {
            Vector3Int position = tilemap.WorldToCell(shrine.transform.position);
            shrinePositions.Add(position);
        }

        // Iterate through all cells in the grid
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3Int cellPosition = new Vector3Int(x, y, 0);
                Cell cell = grid[x, y];

                // Check if the cell is a pillar position / Check if the cell is a shrine position
                if (pillarPositions.Contains(cellPosition))
                {
                    cell.type = Cell.Type.Pillar;
                    continue;
                }                
                else if (shrinePositions.Contains(cellPosition))
                {
                    cell.type = Cell.Type.Shrine;
                    continue;
                }

                // Set the tile
                tilemap.SetTile(cell.position, GetTile(cell));
            }
        }
    }

    private TileBase GetTile(Cell cell)
    {
        if (cell.revealed)
        {
            return GetRevealedTile(cell);
        }
        else if (cell.flagged)
        {
            return tileFlag_anim;
        }
        else
        {
            return GetUnknownTile();
        }
    }

    /*private Tile GetUnknownTile()
    {
        if (tileUnknownVariants != null && tileUnknownVariants.Length > 0)
        {
            int randomIndex = Random.Range(0, tileUnknownVariants.Length);
            return tileUnknownVariants[randomIndex];
        }
        else
        {
            Debug.LogWarning("No tileUnknown variants assigned!");
            return null;
        }
    }*/

    private TileBase GetUnknownTile()
    {
        if (tileUnknown != null )
        {
            return tileUnknown;
        }
        else
        {
            Debug.LogWarning("No tileUnknown variants assigned!");
            return null;
        }
    }

    private Tile GetRevealedTile(Cell cell)
    {
        switch (cell.type)
        {
            case Cell.Type.Empty: return tileEmpty;
            case Cell.Type.Mine: return cell.exploded ? tileExploded : tileMine;
            case Cell.Type.Number: return GetNumberTile(cell);
            case Cell.Type.Pillar: return null; // Do not display a tile for Pillar
            case Cell.Type.Shrine: return null; // Do not display a tile for Shrine
            default: return null;
        }
    }

    private Tile GetNumberTile(Cell cell)
    {
        switch (cell.number)
        {
            case 1: return tileNum1;
            case 2: return tileNum2;
            case 3: return tileNum3;
            case 4: return tileNum4;
            case 5: return tileNum5;
            case 6: return tileNum6;
            case 7: return tileNum7;
            case 8: return tileNum8;
            default: return null;
        }
    }

    public bool IsCellRevealed(Vector3Int cellPosition)
    {
        Vector3Int gridCellPosition = tilemap.WorldToCell(cellPosition);
        TileBase tile = tilemap.GetTile(gridCellPosition);
        return tile != null;
    }

    public Cell.Type GetCellType(Vector3Int cellPosition)
    {
        Vector3Int gridCellPosition = tilemap.WorldToCell(cellPosition);
        TileBase tile = tilemap.GetTile(gridCellPosition);

        if (tile == tileFloor)
        {
            return Cell.Type.Floor;
        }
        if (tile == tileEmpty)
        {
            return Cell.Type.Empty;
        }
        if (tile == tileMine)
        {
            return Cell.Type.Mine;
        }
        if (tile == tileExploded)
        {
            return Cell.Type.Mine; // Treat exploded mine as mine
        }
        // Add more checks if there are other tile types
        return Cell.Type.Empty; // Default return type if no match found
    }


    private bool IsAdjacentToPillar(Vector3Int cellPosition)
    {
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0)
                {
                    continue;
                }

                Vector3Int adjacentCellPosition = cellPosition + new Vector3Int(x, y, 0);

                if (IsPillarPosition(adjacentCellPosition))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private bool IsPillarPosition(Vector3Int cellPosition)
    {
        return pillarPositions.Contains(cellPosition);
    }

    private bool IsAdjacentToShrine(Vector3Int cellPosition)
    {
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0)
                {
                    continue;
                }

                Vector3Int adjacentCellPosition = cellPosition + new Vector3Int(x, y, 0);

                if (shrinePositions.Contains(adjacentCellPosition))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private bool IsShrinePosition(Vector3Int cellPosition)
    {
        return shrinePositions.Contains(cellPosition);
    }

    public void PlaceTraps(CellGrid grid, int numberOfTraps)
    {
        int width = grid.Width;
        int height = grid.Height;
        int trapsPlaced = 0;

        while (trapsPlaced < numberOfTraps)
        {
            int x = Random.Range(0, width);
            int y = Random.Range(0, height);

            if (grid[x, y].type == Cell.Type.Empty && !shrinePositions.Contains(new Vector3Int(x, y, 0)))
            {
                grid[x, y].type = Cell.Type.Mine;
                trapsPlaced++;
            }
        }
    }

    public void CalculateNumbers(CellGrid grid)
    {
        int width = grid.Width;
        int height = grid.Height;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Cell cell = grid[x, y];

                if (cell.type == Cell.Type.Empty)
                {
                    int mineCount = grid.CountAdjacentMines(cell);

                    if (mineCount > 0)
                    {
                        cell.type = Cell.Type.Number;
                        cell.number = mineCount;
                    }
                }
            }
        }
    }

    public void RevealCell(Vector3Int cellPosition)
    {
        // Logic to reveal the cell, e.g., change tile sprite, set visibility, etc.
        Tile tile = tilemap.GetTile<Tile>(cellPosition);
        if (tile != null)
        {
            // Change the appearance or state of the tile to show it's revealed
            // For example, changing its color or sprite
            tilemap.SetTileFlags(cellPosition, TileFlags.None);
            tilemap.SetColor(cellPosition, Color.white); // Example: change color to white to indicate revealed
        }
    }
}
