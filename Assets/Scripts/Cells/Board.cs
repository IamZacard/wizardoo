using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class Board : MonoBehaviour
{
    public Tilemap tilemap { get; private set; }

    public Tile[] tileUnknownVariants;
    //public Tile tileUnknown;
    public Tile tileFloor;
    public Tile tileEmpty;
    public Tile tileMine;
    public Tile tileExploded;
    //public Tile tilePillar;
    //public Tile tileFlag;
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

    private void Awake()
    {
        tilemap = GetComponent<Tilemap>();
    }

    public void Draw(CellGrid grid)
    {
        int width = grid.Width;
        int height = grid.Height;

        // Get all game objects with the "pillar" tag
        GameObject[] pillars = GameObject.FindGameObjectsWithTag("Pillar");
        HashSet<Vector3Int> pillarPositions = new HashSet<Vector3Int>();

        // Store the positions of all pillars
        foreach (GameObject pillar in pillars)
        {
            Vector3Int position = tilemap.WorldToCell(pillar.transform.position);
            pillarPositions.Add(position);
        }

        // Iterate through all cells in the grid
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3Int cellPosition = new Vector3Int(x, y, 0);

                // Skip drawing the cell if it's a pillar position
                if (pillarPositions.Contains(cellPosition))
                {
                    grid[x, y].type = Cell.Type.Pillar;
                    continue;
                }

                Cell cell = grid[x, y];
                tilemap.SetTile(cell.position, GetTile(cell));
            }
        }
    }

    /*public void Draw(CellGrid grid)
    {
        int width = grid.Width;
        int height = grid.Height;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Cell cell = grid[x, y];
                tilemap.SetTile(cell.position, GetTile(cell));
            }
        }
    }*/

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
            // If the cell is not revealed or flagged, return the unknown tile
            return GetUnknownTile();
        }
    }



    private Tile GetUnknownTile()
    {
        // Randomly select a variant of tileUnknown
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
    }

    private Tile GetRevealedTile(Cell cell)
    {
        switch (cell.type)
        {
            case Cell.Type.Empty: return tileEmpty;            
            case Cell.Type.Mine: return cell.exploded ? tileExploded : tileMine;
            //case Cell.Type.Pillar return null;
            case Cell.Type.Number: return GetNumberTile(cell);
            default: return null;
        }
    }

    // Add this method to check if a cell is revealed
    public bool IsCellRevealed(Vector3Int cellPosition)
    {
        // Convert the world position to grid position
        Vector3Int gridCellPosition = tilemap.WorldToCell(cellPosition);

        // Get the tile at the given cell position
        TileBase tile = tilemap.GetTile(gridCellPosition);

        // Check if the tile is not null (revealed)
        return tile != null;
    }

    public Cell.Type GetCellType(Vector3Int cellPosition)
    {
        // Convert the world position to grid position
        Vector3Int gridCellPosition = tilemap.WorldToCell(cellPosition);

        // Get the tile at the given cell position
        TileBase tile = tilemap.GetTile(gridCellPosition);

        // Determine the cell type based on the tile
        if (tile == tileFloor)
        {
            return Cell.Type.Floor;
        }
        // Add other conditions to check for different cell types if needed

        // Default to Empty if no specific cell type is found
        return Cell.Type.Empty;
    }

    private Tile GetNumberTile(Cell cell)
    {
        // Check if the cell is adjacent to a pillar
        if (IsAdjacentToPillar(cell.position))
        {
            // Return null to indicate that no number tile should be drawn for this cell
            return null;
        }

        // If not adjacent to a pillar, generate the number tile as usual
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

    private bool IsAdjacentToPillar(Vector3Int cellPosition)
    {
        // Iterate through adjacent cells
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                // Skip the current cell
                if (x == 0 && y == 0)
                {
                    continue;
                }

                Vector3Int adjacentCellPosition = cellPosition + new Vector3Int(x, y, 0);

                // Check if the adjacent cell is a pillar position
                if (IsPillarPosition(adjacentCellPosition))
                {
                    return true;
                }
            }
        }

        // No adjacent pillar found
        return false;
    }

    private bool IsPillarPosition(Vector3Int cellPosition)
    {
        // Check if the cell position matches any pillar position
        return pillarPositions.Contains(cellPosition);
    }


}
