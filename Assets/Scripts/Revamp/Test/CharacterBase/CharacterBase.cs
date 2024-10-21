using Core;
using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public abstract class CharacterBase : MonoBehaviour, ICharacterBase
{
    [Header("CharacterBase")]
    public Stats stats;

    public PlayerMovement controls;

    private GameRulesManager game;
    private GameCellGrid grid;
    private TilemapVisualizer board;
    private TrapGenerator trapGen;
    private NumberGenerator numGen;
    private RoomFirstDungeonGenerator gen;

    private IInteractable _interactable;

    private Vector3 originalScale;
    public float _characterModelScaleNumber = 1.2f;

    [SerializeField] private Tilemap groundTileMap;
    [SerializeField] private Tilemap roomTileMap;
    [SerializeField] private Tilemap colissionTileMap;

    public bool isActive;
    private bool _isInteractionKeyPressed => Input.GetKeyDown(KeyCode.E);

    private void Start()
    {
        controls.Main.Movement.performed += ctx => Move(ctx.ReadValue<Vector2>());

        // Initialize original scale
        originalScale = transform.localScale;

        board = FindAnyObjectByType<TilemapVisualizer>();
        grid = new GameCellGrid(8, 8);

        trapGen = new TrapGenerator(grid);  // Pass cellGrid as a dependency
        numGen = new NumberGenerator(grid);         // Assuming it doesn't require parameters

        GameObject boardObject = GameObject.FindGameObjectWithTag("Board");
        GameObject roomObject = GameObject.FindGameObjectWithTag("Room");
        GameObject wallObject = GameObject.FindGameObjectWithTag("Wall");
        GameObject gameRulesObject = GameObject.FindGameObjectWithTag("GameRules");

        if (boardObject != null)
        {
            groundTileMap = boardObject.GetComponent<Tilemap>();
        }
        else
        {
            Debug.LogWarning("No GameObject with tag 'Board' found!");
        }

        if (roomObject != null)
        {
            roomTileMap = roomObject.GetComponent<Tilemap>();
        }
        else
        {
            Debug.LogWarning("No GameObject with tag 'Room' found!");
        }

        if (wallObject != null)
        {
            colissionTileMap = wallObject.GetComponent<Tilemap>();
        }
        else
        {
            Debug.LogWarning("No GameObject with tag 'Wall' found!");
        }

        if (gameRulesObject != null)
        {
            game = gameRulesObject.GetComponent<GameRulesManager>();
        }
        else
        {
            Debug.LogWarning("No GameObject with tag 'GameRules' found!");
        }
    }

    private void OnEnable()
    {
        controls.Enable();
    }

    private void OnDisable()
    {
        controls.Disable();
    }

    void Awake()
    {
        controls = new PlayerMovement();
    }

    private void Update()
    {
        if (isActive)
        {
            Reveal();

            if (Input.GetMouseButtonDown(1)) // Right-click flag
            {
                Flag();
            }

            if (_isInteractionKeyPressed)
            {
                Interact();
            }
        }
    }

    #region Movement
    public virtual void Move(Vector2 direction)
    {
        if (!isActive) return; // Prevent movement if the player is not active

        // Ensure only one direction is processed at a time
        direction = new Vector2(
            direction.x != 0 ? Mathf.Sign(direction.x) : 0,
            direction.y != 0 ? Mathf.Sign(direction.y) : 0
        );

        if (CanMove(direction))
        {
            // Perform the jump
            Vector3 targetPosition = transform.position + (Vector3)direction;
            StartCoroutine(JumpToPosition(targetPosition)); // Ensure this is the only call to JumpToPosition
        }
    }

    private bool CanMove(Vector2 direction)
    {
        if (groundTileMap == null || colissionTileMap == null) return false;

        Vector3Int groundGridPosition = groundTileMap.WorldToCell(transform.position + (Vector3)direction);
        bool canMoveOnGround = groundTileMap.HasTile(groundGridPosition) && !colissionTileMap.HasTile(groundGridPosition);

        if (roomTileMap != null && roomTileMap.gameObject.activeSelf)
        {
            Vector3Int roomGridPosition = roomTileMap.WorldToCell(transform.position + (Vector3)direction);
            bool canMoveInRoom = roomTileMap.HasTile(roomGridPosition) && !colissionTileMap.HasTile(roomGridPosition);

            if (canMoveInRoom)
            {
                return true;
            }
        }

        return canMoveOnGround;
    }

    private IEnumerator JumpToPosition(Vector3 targetPosition)
    {
        float jumpHeight = 0.4f; // Height of the jump
        float jumpDuration = 0.05f; // Duration of the jump
        Vector3 startPosition = transform.position;
        Vector3 peakPosition = startPosition + new Vector3(0, jumpHeight, 0);

        // Move to peak position
        float elapsed = 0f;
        while (elapsed < jumpDuration / 2)
        {
            transform.position = Vector3.Lerp(startPosition, peakPosition, elapsed / (jumpDuration / 2));
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Move to target position
        elapsed = 0f;
        while (elapsed < jumpDuration / 2)
        {
            transform.position = Vector3.Lerp(peakPosition, targetPosition, elapsed / (jumpDuration / 2));
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Snap to final position
        transform.position = SnapPosition(targetPosition);

        // Scale character after jump
        StartCoroutine(ScaleCharacter());
    }

    private Vector3 SnapPosition(Vector3 position)
    {
        // Snapping to the nearest half unit grid (0.5 units)
        position.x = Mathf.Floor(position.x) + 0.5f;
        position.y = Mathf.Floor(position.y) + 0.5f;
        return position;
    }

    private IEnumerator ScaleCharacter()
    {
        transform.localScale = originalScale * _characterModelScaleNumber;
        yield return new WaitForSeconds(0.25f);
        transform.localScale = originalScale;
    }
    #endregion

    #region Flagging
    public void Flag()
    {
        if (!TryGetCellAtMousePosition(out Cell cell) || cell.revealed) return;

        if (cell.flagged)
        {
            cell.flagged = false;
            stats._flagCount += 1;
        }
        else if (stats._flagCount > 0)
        {
            cell.flagged = true;
            stats._flagCount -= 1;
        }
    }

    public bool TryGetCellAtMousePosition(out Cell cell)
    {
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int cellPosition = board.tilemap.WorldToCell(worldPosition);
        return grid.TryGetCell(cellPosition.x, cellPosition.y, out cell);
    }
    #endregion

    #region RevealingCells
    public void Reveal()
    {
        if (TryGetCellAtPlayerPosition(out Cell cell))
        {
            if (!game._generated)
            {
                trapGen.GenerateTraps(cell, game._trapCount);
                numGen.GenerateNumbers();
                game._generated = true;
                isActive = true;
            }

            Reveal(cell);
        }
    }

    // Inside your Reveal function:
    public void Reveal(Cell cell)
    {
        if (cell.revealed || cell.flagged) return;

        switch (cell.type)
        {
            case Cell.Type.Trap:
                Explode(cell);
                break;
            case Cell.Type.Empty:
                StartCoroutine(game.Flood(cell));
                break;
            default:
                cell.revealed = true;
                break;
        }

        game.CheckWinCondition();       

        board.Draw(grid);
    }

    public bool TryGetCellAtPlayerPosition(out Cell cell)
    {
        Vector3 worldPosition = transform.position;
        Vector3Int cellPosition = board.tilemap.WorldToCell(worldPosition);
        return grid.TryGetCell(cellPosition.x, cellPosition.y, out cell);
    }
    #endregion

    #region Exploding
    public void Explode(Cell cell)
    {
        TriggerGameOver(cell);
    }

    public void TriggerGameOver(Cell cell)
    {
        Debug.Log("Game Over!");

        cell.exploded = true;
        cell.revealed = true;

        // Additional game-over logic can go here (e.g., showing a UI panel)
    }
    #endregion

    #region Interacting
    public void Interact()
    {
        if (_isInteractionKeyPressed && isActive && _interactable != null)
        {
            _interactable.Interact();
        }
    }
    #endregion

    private void OnTriggerEnter2D(Collider2D other)
    {
        _interactable = other.GetComponent<IInteractable>();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        _interactable = null;
    }
}
