using Core;
using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.Rendering.Universal;
using System;

public abstract class CharacterBase : MonoBehaviour, ICharacterBase
{
    [Header("CharacterBase")]
    public Stats stats;
    public PlayerMovement controls;
    public bool isActive;

    private IInteractable _interactable;
    private Vector3 originalScale;
    public float _characterModelScaleNumber = 1.2f;

    [SerializeField] private Tilemap groundTileMap;
    [SerializeField] private Tilemap roomTileMap;
    [SerializeField] private Tilemap colissionTileMap;

    private Light2D characterLight;

    private GameRules _game;
    private CellGrid _grid;

    private bool _isInteractionKeyPressed => Input.GetKeyDown(KeyCode.E);

    private void OnEnable() => controls.Enable();

    private void OnDisable() => controls.Disable();

    private void Awake()
    {
        controls = new PlayerMovement();

        characterLight = GetComponentInChildren<Light2D>();
        if (characterLight == null)
        {
            Debug.LogError("Light2D component not found. Ensure it's attached as a child object to the character.");
        }
        else if (stats != null)
        {
            characterLight.pointLightOuterRadius = stats._lightRadius;
            Debug.Log($"Initial Light Radius set to {stats._lightRadius}");
        }
    }

    private void Start()
    {
        controls.Main.Movement.performed += ctx => Move(ctx.ReadValue<Vector2>());

        originalScale = transform.localScale;

        // Object assignments (no changes)
        groundTileMap = GameObject.FindGameObjectWithTag("Board")?.GetComponent<Tilemap>();
        roomTileMap = GameObject.FindGameObjectWithTag("Room")?.GetComponent<Tilemap>();
        colissionTileMap = GameObject.FindGameObjectWithTag("Wall")?.GetComponent<Tilemap>();
        _game = GameObject.FindGameObjectWithTag("GameRules")?.GetComponent<GameRules>();

        if (_game != null) _grid = _game._grid;
    }

    private void Update()
    {
        if (!_game.gameover)
        {
            Reveal();
            if (Input.GetMouseButtonDown(1) && _game.canFlag && !_game.levelComplete)
            {
                Flag();
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

        if (PlayerEventManager.Instance != null)
        {
            PlayerEventManager.Instance.TriggerMoveEvent();
            Debug.Log("Step");
        }
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

    #region RevealingCells
    public void Reveal() => _game.Reveal();
    #endregion

    #region Flagging
    public void Flag() => _game.Flag();
    #endregion

    #region Exploding
    public void Explode(Cell cell)
    {
        _game.TriggerGameOver(cell);
        PlayerEventManager.TriggerExplodeEvent();
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

    private void OnTriggerEnter2D(Collider2D other) => _interactable = other.GetComponent<IInteractable>();

    private void OnTriggerExit2D(Collider2D other) => _interactable = null;
}
