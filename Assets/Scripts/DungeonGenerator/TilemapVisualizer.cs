using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

public class TilemapVisualizer : MonoBehaviour
{
    [SerializeField]
    private Tilemap floorTilemap, wallTilemap;
    [SerializeField]
    private TileBase floorTile, wallTop, wallSideRight, wallSiderLeft, wallBottom, wallFull, 
        wallInnerCornerDownLeft, wallInnerCornerDownRight, wallInnerCornerUpLeft, wallInnerCornerUpRight,
        wallDiagonalCornerDownRight, wallDiagonalCornerDownLeft, wallDiagonalCornerUpRight, wallDiagonalCornerUpLeft;

    public Tilemap tilemap { get; private set; }

    public TileBase[] tileUnknownVariants;
    public AnimatedTile tileUnknown;
    public TileBase tileFloor;
    public Tile tileEmpty;
    public Tile tileTrap;
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

    private GameCellGrid cellGrid;

    public void Initialize(GameCellGrid grid)
    {
        cellGrid = grid;  // Reference to the grid
    }
    private void Awake()
    {
        GameObject boardObject = GameObject.FindGameObjectWithTag("Board");
        if (boardObject != null)
        {
            tilemap = boardObject.GetComponent<Tilemap>(); // Assign the correct type
            if (tilemap == null)
            {
                Debug.LogWarning("TilemapVisualizer component not found on the GameObject with tag 'Board'!");
            }
        }
        else
        {
            Debug.LogWarning("No GameObject with tag 'Board' found!");
        }
    }

    public void PaintFloorTiles(IEnumerable<Vector2Int> floorPositions)
    {
        PaintTiles(floorPositions, floorTilemap, floorTile, Cell.Type.Floor);
    }

    public void PaintAndAssignTiles(IEnumerable<Vector2Int> positions, GameCellGrid grid, Cell.Type cellType)
    {
        foreach (var position in positions)
        {
            Vector3Int gridPosition = new Vector3Int(position.x, position.y, 0);
            Cell cell = grid[position.x, position.y];

            // Assign the specified cell type
            cell.type = cellType;

            // Get the appropriate tile based on the cell type
            TileBase tile = GetTileForType(cell);

            // Set the tile on the tilemap
            if (tile != null)
            {
                tilemap.SetTile(gridPosition, tile);
            }
        }
    }

    private TileBase GetTileForType(Cell cell)
    {
        // Determine the tile based on the cell type
        return cell.type switch
        {            
            Cell.Type.Floor => floorTile,
            Cell.Type.Empty => tileEmpty,
            Cell.Type.Trap => cell.exploded ? tileExploded : tileTrap,
            Cell.Type.Number => GetNumberTile(cell),
            _ => null
        };
    }

    private void PaintTiles(IEnumerable<Vector2Int> positions, Tilemap tilemap, TileBase tile, Cell.Type cellType)
    {
        foreach (var position in positions)
        {
            PaintSingleTile(tilemap, tile, position, cellType);
        }
    }

    internal void PaintSingleBasicWall(Vector2Int position, string binaryType)
    {
        int typeAsInt = Convert.ToInt32(binaryType, 2);
        TileBase tile = null;
        Cell.Type cellType = Cell.Type.Wall;
        if (WallTypesHelper.wallTop.Contains(typeAsInt))
        {
            tile = wallTop;
        }else if (WallTypesHelper.wallSideRight.Contains(typeAsInt))
        {
            tile = wallSideRight;
        }
        else if (WallTypesHelper.wallSideLeft.Contains(typeAsInt))
        {
            tile = wallSiderLeft;
        }
        else if (WallTypesHelper.wallBottm.Contains(typeAsInt))
        {
            tile = wallBottom;
        }
        else if (WallTypesHelper.wallFull.Contains(typeAsInt))
        {
            tile = wallFull;
        }

        if (tile!=null)
            PaintSingleTile(wallTilemap, tile, position, cellType);
    }

    private void PaintSingleTile(Tilemap tilemap, TileBase tile, Vector2Int position, Cell.Type cellType)
    {
        var tilePosition = tilemap.WorldToCell((Vector3Int)position);
        tilemap.SetTile(tilePosition, tile);

        // Also update the cell type in the GameCellGrid
        if (cellGrid != null && cellGrid.InBounds(position.x, position.y))
        {
            var cell = cellGrid.GetCell(position.x, position.y);
            if (cell != null)
            {
                cell.type = cellType;  // Assign cell type
            }
        }
    }

    public void Clear()
    {
        floorTilemap.ClearAllTiles();
        wallTilemap.ClearAllTiles();
    }

    internal void PaintSingleCornerWall(Vector2Int position, string binaryType)
    {
        int typeASInt = Convert.ToInt32(binaryType, 2);
        TileBase tile = null;
        Cell.Type cellType = Cell.Type.Wall;

        if (WallTypesHelper.wallInnerCornerDownLeft.Contains(typeASInt))
        {
            tile = wallInnerCornerDownLeft;
        }
        else if (WallTypesHelper.wallInnerCornerDownRight.Contains(typeASInt))
        {
            tile = wallInnerCornerDownRight;
        }
        else if (WallTypesHelper.wallInnerCornerUpLeft.Contains(typeASInt))
        {
            tile = wallInnerCornerDownRight;
        }
        else if (WallTypesHelper.wallInnerCornerUpRight.Contains(typeASInt))
        {
            tile = wallInnerCornerDownRight;
        }
        else if (WallTypesHelper.wallDiagonalCornerDownLeft.Contains(typeASInt))
        {
            tile = wallDiagonalCornerDownLeft;
        }
        else if (WallTypesHelper.wallDiagonalCornerDownRight.Contains(typeASInt))
        {
            tile = wallDiagonalCornerDownRight;
        }
        else if (WallTypesHelper.wallDiagonalCornerUpRight.Contains(typeASInt))
        {
            tile = wallDiagonalCornerUpRight;
        }
        else if (WallTypesHelper.wallDiagonalCornerUpLeft.Contains(typeASInt))
        {
            tile = wallDiagonalCornerUpLeft;
        }
        else if (WallTypesHelper.wallFullEightDirections.Contains(typeASInt))
        {
            tile = wallFull;
        }
        else if (WallTypesHelper.wallBottmEightDirections.Contains(typeASInt))
        {
            tile = wallBottom;
        }

        if (tile != null)
            PaintSingleTile(wallTilemap, tile, position, cellType);
    }

    public void Draw(GameCellGrid grid)
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

    private TileBase GetUnknownTile()
    {
        if (tileUnknown != null)
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
            case Cell.Type.Trap: return cell.exploded ? tileExploded : tileTrap;
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

        if (tile == tileFloor) return Cell.Type.Floor;
        if (tile == tileEmpty) return Cell.Type.Empty;
        if (tile == tileTrap) return Cell.Type.Trap;
        if (tile == tileExploded) return Cell.Type.Trap;

        return Cell.Type.Empty;
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
                grid[x, y].type = Cell.Type.Trap;
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
                    int mineCount = grid.CountAdjacentTraps(cell);

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
