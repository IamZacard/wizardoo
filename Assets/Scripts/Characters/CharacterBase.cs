using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

public class CharacterBase : MonoBehaviour
{
    [Header("Tilemap References")]
    public Tilemap groundTileMap;
    public Tilemap collisionTileMap;
    public Tilemap revealedTileMap; // For tracking revealed cells

    [Header("UI")]
    public GameObject interactionPrompt;

    // Character data
    public CharacterData CharacterData { get; private set; }

    // Components
    private PlayerMovement controls;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;

    // State management
    public CharacterState CurrentState { get; private set; } = CharacterState.Idle;

    // Movement
    private bool isMoving = false;
    private Vector2 lastMoveDirection;

    // Interaction
    private List<IInteractable> nearbyInteractables = new List<IInteractable>();

    // Vision/Reveal system
    private HashSet<Vector3Int> revealedCells = new HashSet<Vector3Int>();

    private void Awake()
    {
        controls = new PlayerMovement();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Find tilemaps if not assigned
        if (groundTileMap == null)
            groundTileMap = FindObjectOfType<Tilemap>();
    }

    private void OnEnable()
    {
        controls.Enable();
    }

    private void OnDisable()
    {
        controls.Disable();
    }

    private void Start()
    {
        // Subscribe to input events
        controls.Main.Movement.performed += ctx => HandleMovementInput(ctx.ReadValue<Vector2>());
        controls.Main.AbilityUsage.performed += ctx => HandleInteractionInput();

        // Initialize interaction prompt
        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
    }

    public void Initialize(CharacterData data)
    {
        CharacterData = data;

        // Set sprite if available
        if (spriteRenderer != null && data.icon != null)
        {
            spriteRenderer.sprite = data.icon;
        }

        // Snap to grid and reveal initial area
        transform.position = SnapToGrid(transform.position);
        RevealCellsAroundPosition(transform.position);

        SetState(CharacterState.Idle);
    }

    private void Update()
    {
        // Handle mouse input for flagging
        if (CurrentState == CharacterState.Idle && Input.GetMouseButtonDown(0))
        {
            HandleFlaggingInput();
        }

        // Handle interaction key input
        if (CurrentState == CharacterState.Idle && Input.GetKeyDown(CharacterData.interactionKey))
        {
            HandleInteractionInput();
        }

        // Update interaction prompt visibility
        UpdateInteractionPrompt();
    }

    private void HandleMovementInput(Vector2 inputDirection)
    {
        if (CurrentState != CharacterState.Idle || isMoving) return;

        // Normalize to single-axis movement
        Vector2 direction = NormalizeToSingleAxis(inputDirection);

        if (direction != Vector2.zero && CanMove(direction))
        {
            StartCoroutine(MoveToPosition(direction));
        }
    }

    private Vector2 NormalizeToSingleAxis(Vector2 input)
    {
        // Priority: horizontal over vertical if both are pressed
        if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
        {
            return new Vector2(Mathf.Sign(input.x), 0);
        }
        else if (Mathf.Abs(input.y) > 0)
        {
            return new Vector2(0, Mathf.Sign(input.y));
        }
        return Vector2.zero;
    }

    private bool CanMove(Vector2 direction)
    {
        if (groundTileMap == null || collisionTileMap == null) return false;

        Vector3 targetWorldPos = transform.position + (Vector3)direction;
        Vector3Int gridPosition = groundTileMap.WorldToCell(targetWorldPos);

        return groundTileMap.HasTile(gridPosition) && !collisionTileMap.HasTile(gridPosition);
    }

    private IEnumerator MoveToPosition(Vector2 direction)
    {
        SetState(CharacterState.Moving);
        isMoving = true;
        lastMoveDirection = direction;

        Vector3 startPosition = transform.position;
        Vector3 targetPosition = startPosition + (Vector3)direction;
        targetPosition = SnapToGrid(targetPosition);

        // Flip sprite based on horizontal movement
        if (direction.x > 0)
            spriteRenderer.flipX = true;
        else if (direction.x < 0)
            spriteRenderer.flipX = false;

        // Jump animation
        yield return StartCoroutine(JumpToPosition(startPosition, targetPosition));

        // Reveal cells around new position
        RevealCellsAroundPosition(targetPosition);

        // Check for interactions or traps at new position
        CheckCellEffects(targetPosition);

        isMoving = false;
        SetState(CharacterState.Idle);
    }

    private IEnumerator JumpToPosition(Vector3 start, Vector3 target)
    {
        float jumpHeight = 0.4f;
        float jumpDuration = 0.15f;

        float elapsed = 0f;

        while (elapsed < jumpDuration)
        {
            float t = elapsed / jumpDuration;

            // Parabolic arc for jump
            float height = jumpHeight * (4f * t * (1f - t));
            Vector3 currentPos = Vector3.Lerp(start, target, t);
            currentPos.y += height;

            transform.position = currentPos;

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = target;
    }

    private Vector3 SnapToGrid(Vector3 position)
    {
        return new Vector3(
            Mathf.Floor(position.x) + 0.5f,
            Mathf.Floor(position.y) + 0.5f,
            position.z
        );
    }

    private void RevealCellsAroundPosition(Vector3 worldPosition)
    {
        if (groundTileMap == null || CharacterData == null) return;

        Vector3Int centerCell = groundTileMap.WorldToCell(worldPosition);
        int radius = CharacterData.lightRadius;

        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                Vector3Int cellPos = centerCell + new Vector3Int(x, y, 0);

                if (!revealedCells.Contains(cellPos) && groundTileMap.HasTile(cellPos))
                {
                    revealedCells.Add(cellPos);
                    OnCellRevealed(cellPos);
                }
            }
        }
    }

    private void OnCellRevealed(Vector3Int cellPosition)
    {
        // Mark cell as revealed in revealed tilemap if available
        if (revealedTileMap != null)
        {
            // You can set a revealed tile here or trigger other reveal effects
        }

        // Trigger any reveal events (like showing numbers, traps, etc.)
        var cellEffect = GetCellEffect(cellPosition);
        if (cellEffect != null)
        {
            cellEffect.OnRevealed();
        }
    }

    private void CheckCellEffects(Vector3 worldPosition)
    {
        Vector3Int cellPos = groundTileMap.WorldToCell(worldPosition);
        var cellEffect = GetCellEffect(cellPos);

        if (cellEffect != null)
        {
            cellEffect.OnPlayerEntered(this);
        }
    }

    private ICellEffect GetCellEffect(Vector3Int cellPosition)
    {
        // This would interface with your minesweeper/trap system
        // For now, return null - implement based on your cell effect system
        return null;
    }

    private void HandleFlaggingInput()
    {
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;

        Vector3Int cellPos = groundTileMap.WorldToCell(mouseWorldPos);

        if (groundTileMap.HasTile(cellPos))
        {
            ToggleCellFlag(cellPos);
        }
    }

    private void ToggleCellFlag(Vector3Int cellPosition)
    {
        // Create flag effect at cell position
        if (CharacterData.flagEffect != null)
        {
            Vector3 worldPos = groundTileMap.CellToWorld(cellPosition) + Vector3.one * 0.5f;

            // Check if cell is already flagged and toggle accordingly
            // This would interface with your flag management system
            GameObject flagInstance = Instantiate(CharacterData.flagEffect, worldPos, Quaternion.identity);

            // Add flag management logic here
        }
    }

    private void HandleInteractionInput()
    {
        if (nearbyInteractables.Count > 0)
        {
            StartCoroutine(InteractWithNearest());
        }
    }

    private IEnumerator InteractWithNearest()
    {
        if (nearbyInteractables.Count == 0) yield break;

        SetState(CharacterState.Interacting);

        IInteractable interactable = nearbyInteractables[0];

        // Start interaction
        interactable.StartInteraction(this);

        // Wait for interaction duration if specified
        if (CharacterData.interactionDuration > 0)
        {
            yield return new WaitForSeconds(CharacterData.interactionDuration);
        }
        else
        {
            // Wait for interaction to complete (external signal)
            while (CurrentState == CharacterState.Interacting)
            {
                yield return null;
            }
        }

        // End interaction
        interactable.EndInteraction(this);

        if (CurrentState == CharacterState.Interacting)
        {
            SetState(CharacterState.Idle);
        }
    }

    private void UpdateInteractionPrompt()
    {
        bool shouldShow = CurrentState == CharacterState.Idle && nearbyInteractables.Count > 0;

        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(shouldShow);
        }
    }

    private void SetState(CharacterState newState)
    {
        if (CurrentState != newState)
        {
            CurrentState = newState;
            CharacterManager.Instance?.NotifyStateChanged(newState);
        }
    }

    // Interaction detection
    private void OnTriggerEnter2D(Collider2D other)
    {
        var interactable = other.GetComponent<IInteractable>();
        if (interactable != null && !nearbyInteractables.Contains(interactable))
        {
            nearbyInteractables.Add(interactable);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var interactable = other.GetComponent<IInteractable>();
        if (interactable != null)
        {
            nearbyInteractables.Remove(interactable);
        }
    }

    // Public methods for external systems
    public void ForceSetState(CharacterState state)
    {
        SetState(state);
    }

    public bool IsInState(CharacterState state)
    {
        return CurrentState == state;
    }

    public Vector3Int GetGridPosition()
    {
        return groundTileMap != null ? groundTileMap.WorldToCell(transform.position) : Vector3Int.zero;
    }

    public HashSet<Vector3Int> GetRevealedCells()
    {
        return new HashSet<Vector3Int>(revealedCells);
    }
}