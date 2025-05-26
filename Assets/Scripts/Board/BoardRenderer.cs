
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Debug version of BoardRenderer with extensive logging
[RequireComponent(typeof(Tilemap), typeof(TilemapRenderer))]
public class BoardRenderer : MonoBehaviour
{
    [Header("Tiles")]
    public TileBase tileUnknown;
    public TileBase tileEmpty;
    public TileBase tileTrap;
    public TileBase tileExploded;
    public TileBase tileFlag;
    public TileBase[] numberTiles; // Index 0 = number 1, Index 1 = number 2, etc.

    private Tilemap tilemap;
    private TilemapRenderer tilemapRenderer;
    private GameGrid gameGrid;

    public Tilemap Tilemap => tilemap;

    private void Awake()
    {
        tilemap = GetComponent<Tilemap>();
        tilemapRenderer = GetComponent<TilemapRenderer>();

        Debug.Log($"BoardRenderer Awake - Tilemap: {tilemap != null}, TilemapRenderer: {tilemapRenderer != null}");

        // Check if components are properly set up
        if (tilemap == null)
            Debug.LogError("Tilemap component not found!");
        if (tilemapRenderer == null)
            Debug.LogError("TilemapRenderer component not found!");
    }

    public void Initialize(GameGrid grid)
    {
        gameGrid = grid;
        Debug.Log($"BoardRenderer Initialize - Grid: {gameGrid != null}");

        if (gameGrid != null)
        {
            Debug.Log($"Grid size: {gameGrid.Width}x{gameGrid.Height}");
        }

        SetupStaticObjects();
    }

    private void SetupStaticObjects()
    {
        // Find and mark pillars
        GameObject[] pillars = GameObject.FindGameObjectsWithTag("Pillar");
        Debug.Log($"Found {pillars.Length} pillars");

        foreach (GameObject pillar in pillars)
        {
            Vector3Int pos = tilemap.WorldToCell(pillar.transform.position);
            if (gameGrid.IsValidPosition(pos.x, pos.y))
            {
                gameGrid.SetCellType(pos.x, pos.y, Cell.CellType.Pillar);
                Debug.Log($"Set pillar at {pos}");
            }
        }

        // Find and mark shrines
        GameObject[] shrines = GameObject.FindGameObjectsWithTag("Shrine");
        Debug.Log($"Found {shrines.Length} shrines");

        foreach (GameObject shrine in shrines)
        {
            Vector3Int pos = tilemap.WorldToCell(shrine.transform.position);
            if (gameGrid.IsValidPosition(pos.x, pos.y))
            {
                gameGrid.SetCellType(pos.x, pos.y, Cell.CellType.Shrine);
                Debug.Log($"Set shrine at {pos}");
            }
        }
    }

    public void DrawGrid()
    {
        if (gameGrid == null)
        {
            Debug.LogError("GameGrid is null! Cannot draw grid.");
            return;
        }

        if (tilemap == null)
        {
            Debug.LogError("Tilemap is null! Cannot draw grid.");
            return;
        }

        Debug.Log($"Drawing grid {gameGrid.Width}x{gameGrid.Height}");

        // Clear existing tiles first
        tilemap.SetTilesBlock(new BoundsInt(0, 0, 0, gameGrid.Width, gameGrid.Height, 1), new TileBase[gameGrid.Width * gameGrid.Height]);

        int tilesDrawn = 0;

        for (int x = 0; x < gameGrid.Width; x++)
        {
            for (int y = 0; y < gameGrid.Height; y++)
            {
                Cell cell = gameGrid[x, y];
                if (cell != null)
                {
                    DrawCell(cell);
                    tilesDrawn++;
                }
            }
        }

        Debug.Log($"Drew {tilesDrawn} tiles");

        // Verify tiles were actually set
        BoundsInt bounds = tilemap.cellBounds;
        Debug.Log($"Tilemap bounds: {bounds}");

        // Check if we have the unknown tile assigned
        if (tileUnknown == null)
        {
            Debug.LogError("tileUnknown is not assigned!");
        }
        else
        {
            Debug.Log($"tileUnknown assigned: {tileUnknown.name}");
        }
    }

    private void DrawCell(Cell cell)
    {
        // Don't draw tiles over protected cells (they have their own GameObjects)
        if (cell.IsProtected)
        {
            Debug.Log($"Skipping protected cell at {cell.position}");
            return;
        }

        TileBase tileToUse = GetTileForCell(cell);

        if (tileToUse == null)
        {
            Debug.LogWarning($"No tile found for cell at {cell.position} (Type: {cell.type}, Revealed: {cell.revealed}, Flagged: {cell.flagged})");
            return;
        }

        tilemap.SetTile(cell.position, tileToUse);

        // Verify the tile was set
        TileBase setTile = tilemap.GetTile(cell.position);
        if (setTile == null)
        {
            Debug.LogError($"Failed to set tile at {cell.position}");
        }
    }

    private TileBase GetTileForCell(Cell cell)
    {
        if (cell.flagged && !cell.revealed)
        {
            return tileFlag;
        }

        if (!cell.revealed)
        {
            return tileUnknown;
        }

        // Cell is revealed
        switch (cell.type)
        {
            case Cell.CellType.Empty:
                return tileEmpty;
            case Cell.CellType.Trap:
                return cell.exploded ? tileExploded : tileTrap;
            case Cell.CellType.Number:
                return GetNumberTile(cell.number);
            default:
                return tileEmpty;
        }
    }

    private TileBase GetNumberTile(int number)
    {
        if (numberTiles != null && number > 0 && number <= numberTiles.Length)
        {
            return numberTiles[number - 1];
        }
        return tileEmpty;
    }

    // Utility methods
    public Cell GetCellAtWorldPosition(Vector3 worldPos)
    {
        Vector3Int cellPos = tilemap.WorldToCell(worldPos);
        Debug.Log($"World pos {worldPos} -> Cell pos {cellPos}");
        return gameGrid?.GetCell(cellPos.x, cellPos.y);
    }

    public Vector3 GetWorldPosition(Cell cell)
    {
        return tilemap.CellToWorld(cell.position);
    }
}