using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class CharacterBase : MonoBehaviour
{
    [Header("Tilemap References")]
    public Tilemap groundTileMap;

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

    private void Awake()
    {
        controls = new PlayerMovement();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (groundTileMap == null)
        {
            if (GameBoard.Instance != null && GameBoard.Instance.Renderer != null)
            {
                groundTileMap = GameBoard.Instance.Renderer.Tilemap;
            }
            else
            {
                groundTileMap = FindObjectOfType<Tilemap>();
            }
        }
    }

    private void OnEnable()
    {
        controls.Enable();
        // Correct way to subscribe: reference the method directly
        controls.Main.Flag.performed += HandleFlaggingInputPerformed;
    }

    private void OnDisable()
    {
        controls.Disable();
        // Correct way to unsubscribe: reference the *same* method directly
        controls.Main.Flag.performed -= HandleFlaggingInputPerformed;
    }

    private void Start()
    {
        // Subscribe to movement and interaction input events
        controls.Main.Movement.performed += ctx => HandleMovementInput(ctx.ReadValue<Vector2>());
        controls.Main.AbilityUsage.performed += ctx => HandleInteractionInput();

        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
    }

    public void Initialize(CharacterData data)
    {
        CharacterData = data;

        if (spriteRenderer != null && data.icon != null)
        {
            spriteRenderer.sprite = data.icon;
        }

        transform.position = SnapToGrid(transform.position);
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

        UpdateInteractionPrompt();
    }

    // New method specifically for the input action's performed event
    private void HandleFlaggingInputPerformed(InputAction.CallbackContext context)
    {
        if (!GameStateManager.Instance.CanUseAbilities) return;

        Debug.Log("Flag action performed via Input System!");
        HandleFlaggingInput(); // Call your existing logic
    }

    private void HandleMovementInput(Vector2 inputDirection)
    {
        if (!GameStateManager.Instance.CanMove || isMoving || (CurrentState != CharacterState.Idle && CurrentState != CharacterState.Moving))
            return;

        Vector2 direction = NormalizeToSingleAxis(inputDirection);

        if (direction != Vector2.zero && CanMove(direction))
        {
            StartCoroutine(MoveToPosition(direction));
        }
    }

    private Vector2 NormalizeToSingleAxis(Vector2 input)
    {
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

        if (direction.x > 0)
            spriteRenderer.flipX = true;
        else if (direction.x < 0)
            spriteRenderer.flipX = false;

        yield return StartCoroutine(JumpToPosition(startPosition, targetPosition));

        transform.position = targetPosition;

        GameBoard.Instance?.HandleCharacterStep(GetGridPosition());

        isMoving = false;
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
        if (CurrentState != CharacterState.Idle && CurrentState != CharacterState.Moving) return;

        Vector3 mouseScreenPos = Input.mousePosition;

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

        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
        mouseWorldPos.z = 0;

        Vector3Int cellPos = groundTileMap.WorldToCell(mouseWorldPos);

        Debug.Log($"Mouse Screen: {mouseScreenPos}, World: {mouseWorldPos}, Cell: {cellPos}");

        if (GameBoard.Instance != null && groundTileMap.HasTile(cellPos))
        {
            GameBoard.Instance.HandleCellFlag(cellPos, CharacterData.flagEffect);
        }
        else
        {
            Debug.LogWarning($"Cannot flag cell at {cellPos} - GameBoard: {GameBoard.Instance != null}, HasTile: {groundTileMap?.HasTile(cellPos)}");
        }
    }

    private void HandleInteractionInput()
    {
        if (CurrentState != CharacterState.Idle && CurrentState != CharacterState.Moving) return;

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

        interactable.StartInteraction(this);

        if (CharacterData.interactionDuration > 0)
        {
            yield return new WaitForSeconds(CharacterData.interactionDuration);
        }
        else
        {
            while (CurrentState == CharacterState.Interacting)
            {
                yield return null;
            }
        }

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
}