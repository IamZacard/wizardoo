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

    private static Dictionary<string, GameObject[]> taggedObjectCache = new();

    private List<GameObject> effectPool = new();
    private Tilemap tilemap;
    private TilemapRenderer tilemapRenderer;
    private GameGrid gameGrid;

    public Tilemap Tilemap => tilemap;

    private void Awake()
    {
        tilemap = GetComponent<Tilemap>();
        tilemapRenderer = GetComponent<TilemapRenderer>();
    }

    public void Initialize(GameGrid grid)
    {
        gameGrid = grid;
        SetupStaticObjects();
    }

    private void OnDestroy()
    {
        foreach (var effect in effectPool)
        {
            if (effect != null) DestroyImmediate(effect);
        }
        effectPool.Clear();

        foreach (var cache in taggedObjectCache.Values)
        {
            // Clear references
        }
        taggedObjectCache.Clear();
    }

    private void Start()
    {
        // Pre-cache all tagged objects once
        taggedObjectCache["Pillar"] = GameObject.FindGameObjectsWithTag("Pillar");
        taggedObjectCache["Shrine"] = GameObject.FindGameObjectsWithTag("Shrine");
    }

    private void SetupStaticObjects()
    {
        SetupObjectsWithTag("Pillar", Cell.CellType.Pillar);
        SetupObjectsWithTag("Shrine", Cell.CellType.Shrine);
    }

    private void SetupObjectsWithTag(string tag, Cell.CellType cellType)
    {
        if (!taggedObjectCache.ContainsKey(tag))
            taggedObjectCache[tag] = GameObject.FindGameObjectsWithTag(tag);
        foreach (var obj in taggedObjectCache[tag])
        {
            Vector3Int pos = tilemap.WorldToCell(obj.transform.position);
            if (gameGrid.IsValidPosition(pos.x, pos.y))
                gameGrid.SetCellType(pos.x, pos.y, cellType);
        }
    }

    public void DrawGrid()
    {
        StartCoroutine(DrawGridAsync());
    }

    public IEnumerator DrawGridAsync()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        const int maxMilliseconds = 16; // ~60fps budget

        // Use SetTilesBlock for better performance
        BoundsInt area = new BoundsInt(0, 0, 0, gameGrid.Width, gameGrid.Height, 1);
        TileBase[] tileArray = new TileBase[area.size.x * area.size.y];

        int index = 0;
        for (int y = 0; y < gameGrid.Height; y++)
        {
            for (int x = 0; x < gameGrid.Width; x++)
            {
                var cell = gameGrid[x, y];
                tileArray[index++] = cell?.IsProtected == true ? null : GetTileForCell(cell);

                if (stopwatch.ElapsedMilliseconds > maxMilliseconds)
                {
                    yield return null;
                    stopwatch.Restart();
                }
            }
        }

        tilemap.SetTilesBlock(area, tileArray);
    }

    public void DrawCell(Cell cell)
    {
        if (cell.IsProtected) return;

        var currentTile = tilemap.GetTile(cell.position);
        bool needsRevealAnimation = (currentTile == tileUnknown || currentTile == tileFlag)
                                     && cell.revealed && !cell.flagged;

        if (needsRevealAnimation)
        {
            StartCoroutine(AnimateTileReveal(cell));
        }
        else
        {
            var tile = GetTileForCell(cell);
            tilemap.SetTile(cell.position, tile);
        }
    }

    private IEnumerator AnimateTileReveal(Cell cell)
    {
        GameObject tempTile = new GameObject($"TempTile_{cell.position}")
        {
            transform =
            {
                position = GetWorldCenter(cell),
                localScale = Vector3.one * scaleUpFactor
            }
        };

        var renderer = tempTile.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = tilemapRenderer.sortingOrder + 2;
        renderer.sprite = GetSpriteFromTile(tilemap.GetTile(cell.position));

        // Apply revealed tile instantly to avoid retriggering
        tilemap.SetTile(cell.position, GetTileForCell(cell));

        tempTile.transform.DOScale(0f, animationDuration).SetEase(Ease.InQuad).OnComplete(() =>
        {
            Destroy(tempTile);
        });

        if (revealEffectPrefab != null)
            TriggerRevealEffect(cell);

        yield return null;
    }

    private void TriggerRevealEffect(Cell cell)
    {
        Vector3 worldPos = GetWorldCenter(cell);
        GameObject effect = useObjectPooling ? GetOrCreateEffect(worldPos) : Instantiate(revealEffectPrefab, worldPos, Quaternion.identity);

        if (effect != null)
            StartCoroutine(DisableEffectAfterDuration(effect, animationDuration));
    }

    private GameObject GetOrCreateEffect(Vector3 position)
    {
        foreach (var effect in effectPool)
        {
            if (!effect.activeInHierarchy)
            {
                effect.transform.position = position;
                effect.SetActive(true);
                return effect;
            }
        }

        var newEffect = Instantiate(revealEffectPrefab, position, Quaternion.identity);
        effectPool.Add(newEffect);
        return newEffect;
    }

    private IEnumerator DisableEffectAfterDuration(GameObject effect, float duration)
    {
        yield return new WaitForSeconds(duration);
        effect.SetActive(false);
    }

    private TileBase GetTileForCell(Cell cell)
    {
        if (cell.exploded) return tileExploded;
        if (cell.flagged) return tileFlag;

        if (cell.revealed)
        {
            return cell.type switch
            {
                Cell.CellType.Empty => tileEmpty,
                Cell.CellType.Trap => tileTrap,
                Cell.CellType.Number => GetNumberTile(cell.number),
                _ => tileEmpty
            };
        }

        return tileUnknown;
    }

    private TileBase GetNumberTile(int number)
    {
        return (number > 0 && number <= numberTiles.Length) ? numberTiles[number - 1] : tileEmpty;
    }

    private Sprite GetSpriteFromTile(TileBase tileBase)
    {
        if (tileBase is Tile tile) return tile.sprite;
        if (tileBase is AnimatedTile animated && animated.m_AnimatedSprites?.Length > 0)
            return animated.m_AnimatedSprites[0];

        return null;
    }

    public Cell GetCellAtWorldPosition(Vector3 worldPos)
    {
        Vector3Int pos = tilemap.WorldToCell(worldPos);
        return gameGrid?.GetCell(pos.x, pos.y);
    }

    public Vector3 GetWorldPosition(Cell cell) => tilemap.CellToWorld(cell.position);
    private Vector3 GetWorldCenter(Cell cell) => GetWorldPosition(cell) + new Vector3(0.5f, 0.5f, 0);
}
