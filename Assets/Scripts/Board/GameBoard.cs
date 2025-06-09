// GameBoard.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

[System.Serializable]
public class GameActions
{
    public UnityEngine.Events.UnityEvent<Cell> OnTrapRevealed;
    public UnityEngine.Events.UnityEvent<Cell> OnTrapExploded;

    public UnityEngine.Events.UnityEvent<Cell> OnCellRevealed;
    public UnityEngine.Events.UnityEvent<Cell> OnCellFlagged;
    public UnityEngine.Events.UnityEvent<Cell> OnCellUnflagged;    

    public UnityEngine.Events.UnityEvent OnGameWon;
    public UnityEngine.Events.UnityEvent OnGameLost;
    public UnityEngine.Events.UnityEvent OnFloodFillStarted;
    public UnityEngine.Events.UnityEvent OnFloodFillCompleted;
}

[System.Serializable]
[Tooltip("Can the player reveal cells that are flagged?")]
public class InteractionRules
{
    public bool canRevealFlaggedCells = false;
    public bool canRevealProtectedCells = false;
    public bool requireTrapsGenerated = true;

    public bool canFlagRevealedCells = false;
    public bool canFlagProtectedCells = false;
    public bool canFlagBeforeTrapsGenerated = false;

    public bool canStepOnFlaggedCells = false;
    public bool canStepOnProtectedCells = false;
    public bool explodeOnTrapStep = true;
}

public class GameBoard : MonoBehaviour
{
    public static GameBoard Instance { get; private set; }

    [Header("Grid Settings")]
    [Tooltip("Configuration for grid width, height, and trap density")]
    [SerializeField] private GridConfig gridConfig;

    [Header("Components")]
    [Tooltip("Reference to the BoardRenderer component")]
    [SerializeField] private BoardRenderer boardRenderer;

    [Header("Camera")]
    [SerializeField] private Transform cameraTarget;

    [Header("UI Panels")]
    [Tooltip("Panel shown when the player loses the game")]
    [SerializeField] private GameObject losePanel;

    [Header("Animation Settings")]
    [SerializeField] private float revealAnimationDuration = 0.5f;
    [SerializeField] private bool useEightDirections = true;

    [Header("Game Actions")]
    [SerializeField] private GameActions gameActions;
    [SerializeField] private InteractionRules interactionRules;

    private GameGrid gameGrid;
    private bool isInitialized;
    private bool isFloodFilling;

    // Add a reference to the active character
    private CharacterBase activeCharacter;
    public GameGrid Grid => gameGrid;
    public BoardRenderer Renderer => boardRenderer;
    public bool IsInitialized => isInitialized;
    public bool IsFloodFilling => isFloodFilling;
    public GameActions Actions => gameActions;

    public UnityEngine.Events.UnityEvent<CharacterBase> OnCharacterStepEvent = new UnityEngine.Events.UnityEvent<CharacterBase>();

    #region Unity Lifecycle
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        DOTween.Init(true, true, LogBehaviour.ErrorsOnly).SetCapacity(500, 125);

        if (boardRenderer == null)
        {
            boardRenderer = GetComponentInChildren<BoardRenderer>();
        }
    }

    private void OnEnable()
    {
        GameStateManager.OnGameStarted += HandleGameStarted;
        GameStateManager.OnStateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        GameStateManager.OnGameStarted -= HandleGameStarted;
        GameStateManager.OnStateChanged -= HandleStateChanged;
    }

    private void OnDestroy()
    {
        GameStateManager.OnGameStarted -= HandleGameStarted;
        GameStateManager.OnStateChanged -= HandleStateChanged;
    }

    private void Start()
    {
        InitializeBoard();
    }
    #endregion

    #region Core Methods
    public void HandleCharacterStep(Vector3Int characterGridPosition)
    {
        activeCharacter = CharacterManager.Instance?.ActiveCharacter;

        if (!ValidateInteraction()) return;

        Cell cell = gameGrid.GetCell(characterGridPosition.x, characterGridPosition.y);
        if (!CanStepOnCell(cell)) return;

        if (!gameGrid.TrapsGenerated)
        {
            GenerateTrapsAfterFirstReveal(characterGridPosition);
            cell = gameGrid.GetCell(characterGridPosition.x, characterGridPosition.y);
        }

        // Check invulnerability BEFORE decrementing the counter
        IDefensiveAbility defensiveAbility = activeCharacter as IDefensiveAbility;
        bool isCharacterInvulnerable = defensiveAbility?.IsInvulnerable() ?? false;

        if (cell.type == Cell.CellType.Trap)
        {
            if (activeCharacter != null && isCharacterInvulnerable)
            {
                Debug.Log($"{activeCharacter.CharacterData.characterName} stepped on a trap while invulnerable. Game Over averted!");
                defensiveAbility?.OnDefenseTriggered();
                ExecuteFlagToggleAction(cell, activeCharacter.CharacterData.flagEffect);
            }
            else
            {
                ExecuteTrapStepAction(cell);
            }
        }
        else if (!cell.revealed)
        {
            RevealCellLogic(cell);
        }

        OnCharacterStepEvent?.Invoke(activeCharacter);
    }

    public void HandleCellFlag(Vector3Int cellPosition, GameObject flagEffectPrefab = null)
    {
        if (!ValidateInteraction()) return;

        Cell cell = gameGrid.GetCell(cellPosition.x, cellPosition.y);
        if (!CanFlagCell(cell)) return;

        ExecuteFlagToggleAction(cell, flagEffectPrefab);
    }

    public bool HandleManualCellClick(int x, int y)
    {
        if (!ValidateInteraction()) return false;

        Cell cell = gameGrid.GetCell(x, y);
        if (!CanRevealCell(cell)) return false;

        if (!gameGrid.TrapsGenerated)
        {
            GenerateTrapsAfterFirstReveal(new Vector3Int(x, y, 0));
            cell = gameGrid.GetCell(x, y);
        }

        if (cell.type == Cell.CellType.Trap)
        {
            ExecuteTrapClickAction(cell);
            return false;
        }

        RevealCellLogic(cell);
        return true;
    }

    private void ExecuteTrapStepAction(Cell trapCell)
    {
        if (interactionRules.explodeOnTrapStep)
        {
            trapCell.exploded = true;
        }
        trapCell.revealed = true;
        boardRenderer.DrawCell(trapCell);

        gameActions.OnTrapRevealed?.Invoke(trapCell);
        gameActions.OnTrapExploded?.Invoke(trapCell);
        gameActions.OnGameLost?.Invoke();

        GameStateManager.Instance?.LoseGame();
    }

    private void ExecuteTrapClickAction(Cell trapCell)
    {
        trapCell.exploded = true;
        trapCell.revealed = true;
        boardRenderer.DrawCell(trapCell);

        gameActions.OnTrapRevealed?.Invoke(trapCell);
        gameActions.OnTrapExploded?.Invoke(trapCell);
        gameActions.OnGameLost?.Invoke();

        GameStateManager.Instance?.LoseGame();
    }

    private void ExecuteFlagToggleAction(Cell cell, GameObject flagEffectPrefab)
    {
        cell.flagged = !cell.flagged;
        boardRenderer.DrawCell(cell);

        if (cell.flagged)
        {
            gameActions.OnCellFlagged?.Invoke(cell);
            if (flagEffectPrefab != null)
            {
                Vector3 worldPos = boardRenderer.GetWorldPosition(cell) + new Vector3(0.5f, 0.5f, 0);
                Instantiate(flagEffectPrefab, worldPos, Quaternion.identity);
            }
        }
        else
        {
            gameActions.OnCellUnflagged?.Invoke(cell);
        }

        CheckWinCondition();
    }
    #endregion

    #region Validation
    private bool ValidateInteraction()
    {
        if (!(GameStateManager.Instance?.CanAcceptInput ?? false)) return false;
        if (!isInitialized || isFloodFilling) return false;
        return true;
    }

    private bool CanStepOnCell(Cell cell)
    {
        if (cell == null) return false;
        if (cell.flagged && !interactionRules.canStepOnFlaggedCells) return false;
        if (cell.IsProtected && !interactionRules.canStepOnProtectedCells) return false;
        return true;
    }

    private bool CanFlagCell(Cell cell)
    {
        if (cell == null) return false;
        if (cell.revealed && !interactionRules.canFlagRevealedCells) return false;
        if (cell.IsProtected && !interactionRules.canFlagProtectedCells) return false;
        if (!gameGrid.TrapsGenerated && !interactionRules.canFlagBeforeTrapsGenerated) return false;
        return true;
    }

    private bool CanRevealCell(Cell cell)
    {
        if (cell == null) return false;
        if (cell.revealed || cell.IsProtected) return false;
        if (cell.flagged && !interactionRules.canRevealFlaggedCells) return false;
        return true;
    }
    #endregion

    #region Game Flow
    private void HandleGameStarted()
    {
        StartNewGame();
    }

    private void HandleStateChanged(GameState previousState, GameState newState)
    {
        if (newState == GameState.Playing && !isInitialized)
        {
            InitializeBoard();
        }
        else if (newState == GameState.Lost)
        {
            RevealAllTraps();
        }
    }

    public void InitializeBoard()
    {
        int width = gridConfig.width;
        int height = gridConfig.height;

        if (cameraTarget != null)
        {
            float camX = gridConfig.width / 2f;
            float camY = gridConfig.height / 2f;
            cameraTarget.position = new Vector3(camX, camY, cameraTarget.position.z);
        }


        gameGrid = new GameGrid(width, height);
        boardRenderer.Initialize(gameGrid);
        boardRenderer.DrawGrid();

        isInitialized = true;
        isFloodFilling = false;
    }

    public void StartNewGame()
    {
        if (!isInitialized)
        {
            InitializeBoard();
            return;
        }

        gameGrid.Reset();
        boardRenderer.DrawGrid();

        ResetLevelObjects();
    }
    #endregion

    #region Reveal & Flood Fill

    private void RevealCellLogic(Cell cell)
    {
        if (cell == null || cell.revealed || cell.type == Cell.CellType.Trap || cell.IsProtected || cell.flagged)
            return;

        if (cell.type == Cell.CellType.Empty)
        {
            StartCoroutine(StartFloodFill(cell));
        }
        else
        {
            cell.revealed = true;
            boardRenderer.DrawCell(cell);
            gameActions.OnCellRevealed?.Invoke(cell);
            CheckWinCondition();
        }
    }

    private IEnumerator StartFloodFill(Cell startCell)
    {
        isFloodFilling = true;
        gameActions.OnFloodFillStarted?.Invoke();

        HashSet<Cell> visited = new();
        yield return FloodFillStep(startCell, visited);

        isFloodFilling = false;
        gameActions.OnFloodFillCompleted?.Invoke();
        CheckWinCondition();
    }
    private IEnumerator FloodFillStep(Cell cell, HashSet<Cell> visited)
    {
        if (cell == null || cell.revealed || cell.type == Cell.CellType.Trap || cell.IsProtected || cell.flagged || visited.Contains(cell))
            yield break;

        visited.Add(cell);
        cell.revealed = true;
        boardRenderer.DrawCell(cell);
        gameActions.OnCellRevealed?.Invoke(cell);

        yield return new WaitForSeconds(revealAnimationDuration);

        if (cell.type == Cell.CellType.Empty)
        {
            yield return FloodFillAdjacent(cell.position, visited);
        }
    }

    private IEnumerator FloodFillAdjacent(Vector3Int position, HashSet<Cell> visited)
    {
        int[,] directions = useEightDirections
            ? new int[,] { { -1, -1 }, { -1, 0 }, { -1, 1 }, { 0, -1 }, { 0, 1 }, { 1, -1 }, { 1, 0 }, { 1, 1 } }
            : new int[,] { { -1, 0 }, { 1, 0 }, { 0, -1 }, { 0, 1 } };

        for (int i = 0; i < directions.GetLength(0); i++)
        {
            int dx = directions[i, 0];
            int dy = directions[i, 1];
            if (gameGrid.TryGetCell(position.x + dx, position.y + dy, out Cell adjacent))
            {
                yield return FloodFillStep(adjacent, visited);
            }
        }
    }

    public void RevealCellsAroundPosition(Vector3Int centerCellPos, int radius)
    {
        if (gameGrid == null) return;

        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                Vector3Int currentPos = centerCellPos + new Vector3Int(x, y, 0);
                Cell cell = gameGrid.GetCell(currentPos.x, currentPos.y);

                if (cell != null && !cell.revealed && !cell.flagged)
                {
                    cell.revealed = true;
                    boardRenderer.DrawCell(cell);
                    gameActions.OnCellRevealed?.Invoke(cell);
                }
            }
        }
    }
    #endregion

    #region Win/Loss
    private void GenerateTrapsAfterFirstReveal(Vector3Int firstRevealPosition)
    {
        int trapCount = Mathf.RoundToInt(gridConfig.width * gridConfig.height * gridConfig.trapDensity);
        gameGrid.GenerateTraps(firstRevealPosition, trapCount);
        boardRenderer.DrawGrid();
    }

    public bool CheckWinCondition()
    {
        if (!(GameStateManager.Instance?.IsGameActive ?? false)) return false;
        if (!gameGrid.TrapsGenerated) return false;

        var allCells = gameGrid.GetAllCells();
        int totalTraps = 0, flaggedTraps = 0, unrevealedNonTraps = 0;
        List<Cell> unrevealedNonTrapCells = new List<Cell>(); // Collect cells to reveal

        foreach (Cell cell in allCells)
        {
            if (cell.type == Cell.CellType.Trap)
            {
                totalTraps++;
                if (cell.flagged) flaggedTraps++;
            }
            else if (!cell.revealed && !cell.IsProtected)
            {
                unrevealedNonTraps++;
                unrevealedNonTrapCells.Add(cell); // Add to the list
            }
        }

        bool allTrapsFlaggedCorrectly = totalTraps > 0 && flaggedTraps == totalTraps;
        bool allNonTrapsRevealed = unrevealedNonTraps == 0;

        if (allTrapsFlaggedCorrectly || allNonTrapsRevealed)
        {
            // If all traps are correctly flagged, reveal all other unrevealed cells
            if (allTrapsFlaggedCorrectly && unrevealedNonTraps > 0)
            {
                foreach (Cell cell in unrevealedNonTrapCells)
                {
                    cell.revealed = true;
                    gameActions.OnCellRevealed?.Invoke(cell);
                }
            }

            AutoFlagRemainingTraps(); // This will handle any unflagged traps if all non-traps are revealed
            boardRenderer.DrawGrid(); // Redraw the grid to show newly revealed cells and auto-flagged traps

            gameActions.OnGameWon?.Invoke();
            GameStateManager.Instance?.WinGame();
            return true;
        }

        return false;
    }

    private void AutoFlagRemainingTraps()
    {
        bool anyAutoFlagged = false;
        foreach (Cell cell in gameGrid.GetAllCells())
        {
            if (cell.type == Cell.CellType.Trap && !cell.flagged)
            {
                cell.flagged = true;
                anyAutoFlagged = true;
                gameActions.OnCellFlagged?.Invoke(cell);
            }
        }
        if (anyAutoFlagged)
        {
            boardRenderer.DrawGrid();
        }
    }

    private void RevealAllTraps()
    {
        if (gameGrid == null) return;

        foreach (Cell trap in gameGrid.GetCellsByType(Cell.CellType.Trap))
        {
            trap.revealed = true;
            gameActions.OnTrapRevealed?.Invoke(trap);
        }
        boardRenderer.DrawGrid();
    }
    #endregion

    #region Utility
    public Cell GetCellAtWorldPosition(Vector3 worldPos) => boardRenderer.GetCellAtWorldPosition(worldPos);

    private void ResetLevelObjects()
    {
        var barriers = FindObjectsOfType<MagicBarrier>();
        foreach (var barrier in barriers)
        {
            barrier.ResetBarrier();
        }
    }

    public void RefreshVisuals()
    {
        if (isInitialized)
        {
            boardRenderer.DrawGrid();
        }
    }

    public void OverrideGridSize(int w, int h)
    {
        gridConfig.width = w;
        gridConfig.height = h;
        isInitialized = false;
    }

    public void OverrideTrapDensity(float density)
    {
        gridConfig.trapDensity = Mathf.Clamp01(density);
        isInitialized = false;
    }

    public void ResetBoard()
    {
        if (gameGrid != null)
        {
            gameGrid.Reset();
            isFloodFilling = false;
            boardRenderer.DrawGrid();
        }
    }
    #endregion
}