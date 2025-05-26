using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using DG.Tweening;

[RequireComponent(typeof(Tilemap), typeof(TilemapRenderer))]
public class BoardRenderer : MonoBehaviour
{
    [Header("Tiles")]
    public TileBase tileUnknown;
    public TileBase tileEmpty;
    public TileBase tileTrap;
    public TileBase tileExploded;
    public TileBase tileFlag;
    public TileBase[] numberTiles;

    [Header("Reveal Animation Settings")]
    [SerializeField] private GameObject revealEffectPrefab;
    [SerializeField] private bool useObjectPooling = true;
    [SerializeField] private float scaleUpFactor = 1.5f;
    [SerializeField] private float animationDuration = 0.15f;
    private List<GameObject> effectPool;

    private Tilemap tilemap;
    private TilemapRenderer tilemapRenderer;
    private GameGrid gameGrid;

    public Tilemap Tilemap => tilemap;

    private void Awake()
    {
        tilemap = GetComponent<Tilemap>();
        tilemapRenderer = GetComponent<TilemapRenderer>();

        Debug.Log($"BoardRenderer Awake - Tilemap: {tilemap != null}, TilemapRenderer: {tilemapRenderer != null}");

        if (tilemap == null)
            Debug.LogError("Tilemap component not found!");
        if (tilemapRenderer == null)
            Debug.LogError("TilemapRenderer component not found!");

        effectPool = new List<GameObject>();
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

        BoundsInt bounds = tilemap.cellBounds;
        Debug.Log($"Tilemap bounds: {bounds}");

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
        if (cell.IsProtected)
        {
            Debug.Log($"Skipping protected cell at {cell.position}");
            return;
        }

        bool wasRevealed = tilemap.GetTile(cell.position) != tileUnknown && tilemap.GetTile(cell.position) != tileFlag;
        bool isNowRevealed = cell.revealed && !cell.flagged;

        if (!wasRevealed && isNowRevealed)
        {
            Debug.Log($"Animating tile reveal for cell at {cell.position}");
            StartCoroutine(AnimateTileReveal(cell));
        }
        else
        {
            TileBase tileToUse = GetTileForCell(cell);
            if (tileToUse == null)
            {
                Debug.LogWarning($"No tile found for cell at {cell.position} (Type: {cell.type}, Revealed: {cell.revealed}, Flagged: {cell.flagged})");
                return;
            }
            tilemap.SetTile(cell.position, tileToUse);
        }

        TileBase setTile = tilemap.GetTile(cell.position);
        if (setTile == null && !isNowRevealed)
        {
            Debug.LogError($"Failed to set tile at {cell.position}");
        }
    }

    private IEnumerator AnimateTileReveal(Cell cell)
    {
        GameObject tempObject = new GameObject("TempTile");
        tempObject.transform.position = GetWorldPosition(cell) + new Vector3(0.5f, 0.5f, 0);
        SpriteRenderer spriteRenderer = tempObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = tilemapRenderer.sortingOrder + 2;

        TileBase currentTile = tilemap.GetTile(cell.position) ?? tileUnknown;
        Sprite sprite = null;

        if (currentTile is AnimatedTile animatedTile && animatedTile.m_AnimatedSprites != null && animatedTile.m_AnimatedSprites.Length > 0)
        {
            sprite = animatedTile.m_AnimatedSprites[0];
            Debug.Log($"Using AnimatedTile sprite {sprite?.name} at {cell.position}");
        }
        else if (currentTile is Tile tile)
        {
            sprite = tile.sprite;
            Debug.Log($"Using Tile sprite {sprite?.name} at {cell.position}");
        }

        if (sprite != null)
        {
            spriteRenderer.sprite = sprite;
        }
        else
        {
            Debug.LogWarning($"No sprite for current tile at {cell.position} (Tile type: {currentTile?.GetType().Name})");
        }

        TileBase revealedTile = GetTileForCell(cell);

        tempObject.transform.localScale = Vector3.one;
        tempObject.transform.DOScale(scaleUpFactor, animationDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => {
                Debug.Log($"Tile scale-up completed at {cell.position}");
            });

        if (revealEffectPrefab != null)
        {
            TriggerRevealEffect(cell);
        }

        yield return new WaitForSeconds(animationDuration);

        tilemap.SetTile(cell.position, revealedTile);
        Debug.Log($"Set revealed tile at {cell.position}");

        Destroy(tempObject);
    }

    private void TriggerRevealEffect(Cell cell)
    {
        Vector3 worldPos = GetWorldPosition(cell) + new Vector3(0.5f, 0.5f, 0);

        if (useObjectPooling)
        {
            GameObject effect = GetPooledEffect();
            if (effect != null)
            {
                effect.transform.position = worldPos;
                effect.SetActive(true);
                StartCoroutine(DisableEffectAfterDuration(effect));
            }
            else
            {
                effect = Instantiate(revealEffectPrefab, worldPos, Quaternion.identity);
                effectPool.Add(effect);
            }
        }
        else
        {
            Instantiate(revealEffectPrefab, worldPos, Quaternion.identity);
        }

        Debug.Log($"Triggered reveal effect at {cell.position} for {cell.type}");
    }

    private GameObject GetPooledEffect()
    {
        foreach (GameObject effect in effectPool)
        {
            if (!effect.activeInHierarchy)
            {
                return effect;
            }
        }
        return null;
    }

    private IEnumerator DisableEffectAfterDuration(GameObject effect)
    {
        yield return new WaitForSeconds(animationDuration);
        effect.SetActive(false);
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