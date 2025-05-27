using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class CharacterBase : MonoBehaviour
{
    [Header("Tilemap References")]
    // CharacterBase should only need the ground tilemap for movement validation
    // The actual revealing/flagging logic is handled by GameBoard and BoardRenderer.
    public Tilemap groundTileMap;
    //public Tilemap collisionTileMap;

    [Header("UI")]
    public GameObject interactionPrompt;

    // Character data
    public CharacterData CharacterData { get; private set; }

    // Components
    private PlayerMovement controls; // Assuming PlayerMovement is your generated Input System C# class
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;

    // State management
    public CharacterState CurrentState { get; private set; } = CharacterState.Idle;

    // Movement
    private bool isMoving = false;
    private Vector2 lastMoveDirection;

    // Interaction
    private List<IInteractable> nearbyInteractables = new List<IInteractable>();

    // No longer needs revealedCells HashSet, as GameBoard manages this

    private void Awake()
    {
        controls = new PlayerMovement();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Find tilemaps if not assigned (consider making this more robust, e.g., via GameBoard)
        if (groundTileMap == null)
        {
            // Try to find the ground tilemap from the GameBoard's renderer if GameBoard is initialized
            if (GameBoard.Instance != null && GameBoard.Instance.Renderer != null)
            {
                groundTileMap = GameBoard.Instance.Renderer.Tilemap;
            }
            else
            {
                // Fallback, but direct FindObjectOfType is less robust for complex scenes
                groundTileMap = FindObjectOfType<Tilemap>();
            }
        }
    }

    private void OnEnable()
    {
        controls.Enable();
        controls.Main.Flag.performed += ctx =>
        {
            Debug.Log("Flag action triggered");
            HandleFlaggingInput();
        };
    }

    private void OnDisable()
    {
        controls.Disable();
        // Unsubscribe from Flag input action
        controls.Main.Flag.performed -= ctx => HandleFlaggingInput();
    }

    private void Start()
    {
        // Subscribe to movement and interaction input events
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

        // Snap to grid and reveal initial area via GameBoard
        transform.position = SnapToGrid(transform.position);
        // On game start, the initial cell and its surroundings are revealed
        // CharacterBase doesn't 'reveal' cells directly, it tells GameBoard to.
        GameBoard.Instance?.RevealCellsAroundPosition(GetGridPosition(), CharacterData.lightRadius);

        SetState(CharacterState.Idle);
    }

    private void Update()
    {
        // Handle interaction key input (still handled directly via Update for simplicity as it's a single key)
        if (CurrentState == CharacterState.Idle && Input.GetKeyDown(CharacterData.interactionKey))
        {
            HandleInteractionInput();
        }

        // Temporary fallback for right mouse button flagging
        if (Input.GetMouseButtonDown(1)) // Right mouse button
        {
            HandleFlaggingInput();
        }

        // Update interaction prompt visibility
        UpdateInteractionPrompt();
    }

    private void HandleMovementInput(Vector2 inputDirection)
    {
        // Movement is only possible when the CharacterState is Idle or Moving.
        if (CurrentState != CharacterState.Idle && CurrentState != CharacterState.Moving || isMoving) return;

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
        if (groundTileMap == null) return false;

        Vector3 targetWorldPos = transform.position + (Vector3)direction;
        Vector3Int gridPosition = groundTileMap.WorldToCell(targetWorldPos);

        // Check if the target cell is valid and not a collision tile
        return groundTileMap.HasTile(gridPosition);
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

        transform.position = targetPosition; // Ensure character is at the target

        // Stepping on a cell reveals it.
        GameBoard.Instance?.HandleCharacterStep(GetGridPosition());

        isMoving = false;
        // Revert to Idle only if not entering Interacting state from CheckCellEffects
        if (CurrentState == CharacterState.Moving)
        {
            SetState(CharacterState.Idle);
        }
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

    private void HandleFlaggingInput()
    {
        // Player input for movement and most other character actions (e.g., using abilities, flagging cells) are temporarily suspended.
        if (CurrentState != CharacterState.Idle && CurrentState != CharacterState.Moving) return;

        // Use Unity's legacy Input system for mouse position (more reliable)
        Vector3 mouseScreenPos = Input.mousePosition;

        // Find the main camera if Camera.main is null
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
        }

        if (mainCamera == null)
        {
            Debug.LogError("No camera found for mouse world position conversion!");
            return;
        }

        // Convert screen position to world position
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
        mouseWorldPos.z = 0;

        Vector3Int cellPos = groundTileMap.WorldToCell(mouseWorldPos);

        Debug.Log($"Mouse Screen: {mouseScreenPos}, World: {mouseWorldPos}, Cell: {cellPos}");

        // Delegate flagging logic to GameBoard
        if (GameBoard.Instance != null && groundTileMap.HasTile(cellPos))
        {
            GameBoard.Instance.HandleCellFlag(cellPos, CharacterData.flagEffect); // Pass flag effect prefab
        }
        else
        {
            Debug.LogWarning($"Cannot flag cell at {cellPos} - GameBoard: {GameBoard.Instance != null}, HasTile: {groundTileMap?.HasTile(cellPos)}");
        }
    }

    private void HandleInteractionInput()
    {
        // Movement is only possible when the CharacterState is Idle or Moving.
        // Interaction is only possible when the CharacterState is Idle or Moving.
        if (CurrentState != CharacterState.Idle && CurrentState != CharacterState.Moving) return;

        if (nearbyInteractables.Count > 0)
        {
            StartCoroutine(InteractWithNearest());
        }
    }

    private IEnumerator InteractWithNearest()
    {
        if (nearbyInteractables.Count == 0) yield break;

        // Upon pressing the InteractionKey (e.g., 'E') while the CharacterState is Idle or Moving, the character's state transitions to Interacting.
        // During the Interacting state, player movement and most other character actions (e.g., using abilities, flagging cells) are temporarily suspended.
        SetState(CharacterState.Interacting);

        IInteractable interactable = nearbyInteractables[0];

        // Start interaction
        interactable.StartInteraction(this);

        // Wait for interaction duration if specified, or until external signal
        if (CharacterData.interactionDuration > 0)
        {
            yield return new WaitForSeconds(CharacterData.interactionDuration);
        }
        else
        {
            // Wait for interaction to complete (external signal from dialogue system, etc.)
            // The system responsible for that action will revert the CharacterState back to Idle.
            while (CurrentState == CharacterState.Interacting)
            {
                yield return null;
            }
        }

        // End interaction
        interactable.EndInteraction(this);

        // If interaction didn't explicitly change state (e.g., dialogue ending), revert to Idle
        if (CurrentState == CharacterState.Interacting)
        {
            SetState(CharacterState.Idle);
        }
    }

    private void UpdateInteractionPrompt()
    {
        // When the player character is within the InteractionRange of an interactable entity, a UI prompt (e.g., "E" key icon) appears, indicating readiness for interaction.
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

    // Interaction detection (triggered by Character's Collider2D)
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the other collider is an interactable entity
        var interactable = other.GetComponent<IInteractable>();
        // Add to nearby interactables if not already present
        if (interactable != null && !nearbyInteractables.Contains(interactable))
        {
            nearbyInteractables.Add(interactable);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // Remove from nearby interactables if it leaves range
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
        // Return grid position based on the ground tilemap the character is on
        return groundTileMap != null ? groundTileMap.WorldToCell(transform.position) : Vector3Int.zero;
    }
}