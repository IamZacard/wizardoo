using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using DG.Tweening; // Ensure DOTween is imported

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
    [SerializeField] private float scaleUpFactor = 1.5f; // Now used!
    [SerializeField] private float animationDuration = 0.15f;
    private List<GameObject> effectPool;

    private Tilemap tilemap;
    private TilemapRenderer tilemapRenderer;
    private GameGrid gameGrid;

    public Tilemap Tilemap => tilemap; // Expose the tilemap for CharacterBase to use

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

        Debug.Log($"Drawing entire grid {gameGrid.Width}x{gameGrid.Height}");

        int tilesDrawn = 0;

        for (int x = 0; x < gameGrid.Width; x++)
        {
            for (int y = 0; y < gameGrid.Height; y++)
            {
                Cell cell = gameGrid[x, y];
                if (cell != null)
                {
                    DrawCell(cell); // Call DrawCell for each cell
                    tilesDrawn++;
                }
            }
        }

        Debug.Log($"Drew {tilesDrawn} tiles");
    }

    // Public method to draw/update a single cell's visual
    public void DrawCell(Cell cell)
    {
        if (cell.IsProtected)
        {
            // Protected cells (pillars, shrines) are not rendered by the tilemap
            return;
        }

        // Determine if the tile is currently an 'unknown' or 'flag' tile on the map
        TileBase currentTileOnMap = tilemap.GetTile(cell.position);
        bool wasUnknownOrFlagged = (currentTileOnMap == tileUnknown || currentTileOnMap == tileFlag);
        bool isNowRevealedAndNotFlagged = cell.revealed && !cell.flagged;

        if (wasUnknownOrFlagged && isNowRevealedAndNotFlagged)
        {
            // Animate only if transitioning from unknown/flagged to revealed
            Debug.Log($"Animating tile reveal for cell at {cell.position}");
            StartCoroutine(AnimateTileReveal(cell));
        }
        else
        {
            // For all other cases (already revealed, flagging, unflagging, exploded, etc.),
            // just set the tile directly without animation.
            TileBase tileToUse = GetTileForCell(cell);
            if (tileToUse == null)
            {
                Debug.LogWarning($"No tile found for cell at {cell.position} (Type: {cell.type}, Revealed: {cell.revealed}, Flagged: {cell.flagged})");
                return;
            }
            tilemap.SetTile(cell.position, tileToUse);
        }
    }

    private IEnumerator AnimateTileReveal(Cell cell)
    {
        // Create a temporary GameObject to animate the 'unknown' tile away
        GameObject tempObject = new GameObject("TempTile_" + cell.position);
        tempObject.transform.position = GetWorldPosition(cell) + new Vector3(0.5f, 0.5f, 0);
        SpriteRenderer spriteRenderer = tempObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = tilemapRenderer.sortingOrder + 2; // Render above other tiles

        // Get the current sprite of the unknown/flagged tile
        TileBase currentTileBase = tilemap.GetTile(cell.position);
        Sprite currentSprite = null;

        if (currentTileBase is AnimatedTile animatedTile && animatedTile.m_AnimatedSprites != null && animatedTile.m_AnimatedSprites.Length > 0)
        {
            currentSprite = animatedTile.m_AnimatedSprites[0];
        }
        else if (currentTileBase is Tile tile)
        {
            currentSprite = tile.sprite;
        }

        if (currentSprite != null)
        {
            spriteRenderer.sprite = currentSprite;
        }
        else
        {
            Debug.LogWarning($"No sprite found for current tile at {cell.position} for animation.");
        }

        // Set the revealed tile on the tilemap immediately to prevent re-triggering animation
        TileBase revealedTile = GetTileForCell(cell);
        tilemap.SetTile(cell.position, revealedTile);

        // Animate the temporary 'unknown' tile scaling down
        // Use scaleUpFactor to ensure the sprite is initially large and scales down.
        // It's typically used for a "pop-in" effect, but here we can use it to
        // define the *starting* scale before animating to 0.
        tempObject.transform.localScale = Vector3.one * scaleUpFactor; // Initialize with scaleUpFactor
        tempObject.transform.DOScale(0f, animationDuration) // Scale to zero
            .SetEase(Ease.InQuad) // Faster end to the animation
            .OnComplete(() => {
                Debug.Log($"Temp tile animation completed for {cell.position}");
                Destroy(tempObject); // Destroy temp object once animation is done
            });

        // Trigger reveal effect (e.g., particles)
        if (revealEffectPrefab != null)
        {
            TriggerRevealEffect(cell);
        }

        yield return null; // Yield to allow DOTween to handle the animation over time.
    }

    private void TriggerRevealEffect(Cell cell)
    {
        Vector3 worldPos = GetWorldPosition(cell) + new Vector3(0.5f, 0.5f, 0);

        GameObject effect = null;
        if (useObjectPooling)
        {
            effect = GetPooledEffect();
            if (effect == null)
            {
                effect = Instantiate(revealEffectPrefab, worldPos, Quaternion.identity);
                effectPool.Add(effect);
            }
            else
            {
                effect.transform.position = worldPos;
                effect.SetActive(true);
            }
        }
        else
        {
            effect = Instantiate(revealEffectPrefab, worldPos, Quaternion.identity);
        }

        // Assuming the effect has a ParticleSystem or similar that automatically plays and then can be disabled/returned to pool
        if (effect != null)
        {
            StartCoroutine(DisableEffectAfterDuration(effect, animationDuration)); // Use the animation duration or a specific effect duration
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

    private IEnumerator DisableEffectAfterDuration(GameObject effect, float duration)
    {
        yield return new WaitForSeconds(duration);
        effect.SetActive(false);
    }

    private TileBase GetTileForCell(Cell cell)
    {
        // Order of precedence: Exploded > Flagged > Revealed > Unknown
        if (cell.exploded)
        {
            return tileExploded;
        }
        else if (cell.flagged)
        {
            return tileFlag;
        }
        else if (cell.revealed)
        {
            switch (cell.type)
            {
                case Cell.CellType.Empty:
                    return tileEmpty;
                case Cell.CellType.Trap: // Revealed trap (not exploded yet, or after game over)
                    return tileTrap;
                case Cell.CellType.Number:
                    return GetNumberTile(cell.number);
                default:
                    return tileEmpty;
            }
        }
        else // Not revealed, not flagged, not exploded
        {
            return tileUnknown;
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
        // Returns the world position of the bottom-left corner of the cell
        return tilemap.CellToWorld(cell.position);
    }
}