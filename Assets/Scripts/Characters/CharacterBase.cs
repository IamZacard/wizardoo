// CharacterBase.cs
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

    public CharacterData CharacterData { get; private set; }

    private PlayerMovement controls;
    private CharacterSpellManager spellManager;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;

    public CharacterState CurrentState { get; private set; } = CharacterState.Idle;
    private int etherealShieldStepsRemaining;
    private bool isMoving;
    private Vector2 lastMoveDirection;
    private List<IInteractable> nearbyInteractables = new List<IInteractable>();

    public Vector3Int GridPosition => groundTileMap != null ? groundTileMap.WorldToCell(transform.position) : Vector3Int.zero;

    private void Awake()
    {
        controls = new PlayerMovement();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        spellManager = GetComponent<CharacterSpellManager>();
        if (spellManager == null)
        {
            Debug.LogError($"CharacterBase: CharacterSpellManager not found on '{gameObject.name}'", this);
        }

        if (groundTileMap == null)
        {
            groundTileMap = GameBoard.Instance?.Renderer.Tilemap ?? FindObjectOfType<Tilemap>();
        }
    }

    private void OnEnable()
    {
        controls.Enable();
        controls.Main.Flag.performed += HandleFlaggingInputPerformed;
        controls.Main.AbilityUsage.performed += HandleAbilityUsageInputPerformed;
    }

    private void OnDisable()
    {
        controls.Disable();
        controls.Main.Flag.performed -= HandleFlaggingInputPerformed;
        controls.Main.AbilityUsage.performed -= HandleAbilityUsageInputPerformed;
    }

    private void Start()
    {
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

        if (spellManager != null)
        {
            spellManager.Initialize(data);
        }

        transform.position = SnapToGrid(transform.position);
        GameBoard.Instance?.RevealCellsAroundPosition(GridPosition, CharacterData.lightRadius);

        SetState(CharacterState.Idle);
    }

    private void Update()
    {
        if (CurrentState == CharacterState.Idle && Input.GetKeyDown(CharacterData.interactionKey))
        {
            HandleInteractionInput();
        }
        UpdateInteractionPrompt();
    }

    private void HandleFlaggingInputPerformed(InputAction.CallbackContext context)
    {
        if (!GameStateManager.Instance.CanUseAbilities) return;
        HandleFlaggingInput();
    }

    private void HandleMovementInput(Vector2 inputDirection)
    {
        if (!GameStateManager.Instance.CanMove ||
            isMoving ||
            (CurrentState != CharacterState.Idle && CurrentState != CharacterState.Moving) ||
            (GameBoard.Instance?.IsFloodFilling ?? false))
            return;

        Vector2 direction = Mathf.Abs(inputDirection.x) > Mathf.Abs(inputDirection.y)
            ? new Vector2(Mathf.Sign(inputDirection.x), 0)
            : Mathf.Abs(inputDirection.y) > 0
                ? new Vector2(0, Mathf.Sign(inputDirection.y))
                : Vector2.zero;

        if (direction != Vector2.zero && CanMove(direction))
        {
            StartCoroutine(MoveToPosition(direction));
        }
    }

    private void HandleAbilityUsageInputPerformed(InputAction.CallbackContext context)
    {
        if (!GameStateManager.Instance.CanUseAbilities) return;
        spellManager?.TryCastActiveSpell();
    }

    private bool CanMove(Vector2 direction)
    {
        if (groundTileMap == null) return false;
        Vector3 targetWorldPos = transform.position + (Vector3)direction;
        Vector3Int gridPos = groundTileMap.WorldToCell(targetWorldPos);
        return groundTileMap.HasTile(gridPos);
    }

    private IEnumerator MoveToPosition(Vector2 direction)
    {
        SetState(CharacterState.Moving);
        isMoving = true;
        lastMoveDirection = direction;

        Vector3 startPos = transform.position;
        Vector3 targetPos = SnapToGrid(startPos + (Vector3)direction);

        spriteRenderer.flipX = direction.x > 0;

        float jumpHeight = 0.4f;
        float jumpDuration = 0.15f;
        float elapsed = 0f;

        while (elapsed < jumpDuration)
        {
            float t = elapsed / jumpDuration;
            float height = jumpHeight * (4f * t * (1f - t));
            transform.position = Vector3.Lerp(startPos, targetPos, t) + new Vector3(0, height, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPos;
        GameBoard.Instance?.HandleCharacterStep(GridPosition);

        isMoving = false;
        if (CurrentState == CharacterState.Moving)
        {
            SetState(CharacterState.Idle);
        }
    }

    private Vector3 SnapToGrid(Vector3 position) =>
        new Vector3(Mathf.Floor(position.x) + 0.5f, Mathf.Floor(position.y) + 0.5f, position.z);

    private void HandleFlaggingInput()
    {
        if (CurrentState != CharacterState.Idle && CurrentState != CharacterState.Moving) return;

        Camera mainCamera = Camera.main ?? FindObjectOfType<Camera>();
        if (mainCamera == null) return;

        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;
        Vector3Int cellPos = groundTileMap.WorldToCell(mouseWorldPos);

        if (GameBoard.Instance != null && groundTileMap.HasTile(cellPos))
        {
            GameBoard.Instance.HandleCellFlag(cellPos, CharacterData.flagEffect);
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
                yield return null;
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
        interactionPrompt?.SetActive(shouldShow);
    }

    private void SetState(CharacterState newState)
    {
        if (CurrentState != newState)
        {
            CurrentState = newState;
            CharacterManager.Instance?.NotifyStateChanged(newState);
        }
    }

    public void SetEtherealShieldSteps(int steps)
    {
        etherealShieldStepsRemaining = steps;
        if (steps > 0)
        {
            SetState(CharacterState.Invulnerable);
        }
        else if (CurrentState == CharacterState.Invulnerable)
        {
            SetState(CharacterState.Idle);
        }
    }

    public void DecrementEtherealShieldSteps()
    {
        if (etherealShieldStepsRemaining > 0)
        {
            etherealShieldStepsRemaining--;
        }
    }

    public bool IsInvulnerable() => etherealShieldStepsRemaining > 0;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<IInteractable>() is IInteractable interactable &&
            !nearbyInteractables.Contains(interactable))
        {
            nearbyInteractables.Add(interactable);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<IInteractable>() is IInteractable interactable)
        {
            nearbyInteractables.Remove(interactable);
        }
    }

    public void MoveCharacterTo(Vector3Int targetGridPosition)
    {
        if (groundTileMap == null) return;
        Vector3 worldPosition = groundTileMap.CellToWorld(targetGridPosition) + new Vector3(0.5f, 0.5f, 0);
        transform.position = worldPosition;
        GameBoard.Instance?.HandleCharacterStep(GridPosition);
    }

    public void ForceSetState(CharacterState state) => SetState(state);

    public bool IsInState(CharacterState state) => CurrentState == state;

    public Vector3Int GetGridPosition() =>
        groundTileMap != null ? groundTileMap.WorldToCell(transform.position) : Vector3Int.zero;
}
