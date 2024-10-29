using UnityEngine;
using System.Collections;

public class GameRules : MonoBehaviour
{
    [Header("Board Setup")]
    [SerializeField]
    public int Width = 8, Height = 8;
    [SerializeField]
    public int trapCount;
    [SerializeField]
    public float difficulty;

    [Header("Bools")]
    public bool gameover;
    public bool levelComplete = false;
    public bool canFlag = false;
    public bool generated;

    [Header("Player's Stats")]
    private GameObject player;
    private CharacterBase _playerBase;
    public Vector2 startPos;
    public float flagCount;

    [Header("Portal Block")]
    [SerializeField] private GameObject magicBlock;

    private BoardCreation _board;
    public CellGrid _grid;

    private void OnValidate()
    {
        trapCount = Mathf.Clamp(Mathf.RoundToInt((Width * Height) * difficulty), 0, Width * Height);
        flagCount = trapCount;
    }

    private void Awake()
    {

    }

    private void Start()
    {        
        player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            _playerBase = player.GetComponent<CharacterBase>();
            if (_playerBase != null)
            {
                Debug.Log("Player object found: " + player.name);
                // Now you can use playerBase properties
                NewGame();
            }
            else
            {
                Debug.LogWarning("CharacterBase component not found on Player!");
            }
        }
        else
        {
            Debug.LogWarning("Player GameObject not found!");
        }

    }

    void Update()
    {
        if ((Input.GetKeyDown(KeyCode.R) && !levelComplete))
        {
            NewGame();
            return;
        }
    }

    #region NewGame
    public void NewGame()
    {
        StopAllCoroutines();        

        gameover = false;
        levelComplete = false;
        generated = false;

        _playerBase.isActive = true; 

        _grid = new CellGrid(Width, Height);

        // Ensure _board is initialized
        _board = FindObjectOfType<BoardCreation>();
        if (_board != null)
        {
            _board.Draw(_grid);
        }
        else
        {
            Debug.LogError("BoardCreation component not found. Please assign _board or check if it's in the scene.");
        }

        if (!magicBlock.activeSelf)
        {
            magicBlock.SetActive(true);
        }

        player.transform.position = startPos;
        //skullImage.localPosition = initialSkullPosition;
        //skullImage.localScale = initialSkullScale;
        //StartCoroutine(AnimateSkull(initialSkullPosition, initialSkullScale, 0.5f));
        flagCount = trapCount;
        //UpdateTrapFlagText();
        canFlag = false;
        //lostPanel.SetActive(false);
        //solvedPanel.SetActive(false);
    }
    #endregion

    #region RevealingCells
    public void Reveal()
    {
        if (TryGetCellAtPlayerPosition(out Cell cell))
        {
            if (!generated)
            {
                _grid.GenerateTraps(cell, trapCount);
                _grid.GenerateNumbers();
                generated = true;
                canFlag = true;
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
                StartCoroutine(Flood(cell));
                break;
            default:
                cell.revealed = true;
                break;
        }

        CheckWinCondition();

        _board.Draw(_grid);
    }
    #endregion

    #region FlagingCells
    public void Flag()
    {
        if (!TryGetCellAtMousePosition(out Cell cell) || cell.revealed) return;

        if (cell.flagged)
        {
            cell.flagged = false;
            flagCount += 1;
        }
        else if (flagCount > 0)
        {
            cell.flagged = true;
            flagCount -= 1;            
        }

        //UpdateTrapFlagText();

        _board.Draw(_grid);

        CheckWinConditionFlags();
    }

    public bool TryGetCellAtMousePosition(out Cell cell)
    {
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int cellPosition = _board.tilemap.WorldToCell(worldPosition);
        return _grid.TryGetCell(cellPosition.x, cellPosition.y, out cell);
    }

    public bool TryGetCellAtPlayerPosition(out Cell cell)
    {
        Vector3 worldPosition = player.transform.position;
        Vector3Int cellPosition = _board.tilemap.WorldToCell(worldPosition);
        return _grid.TryGetCell(cellPosition.x, cellPosition.y, out cell);
    }
    #endregion

    #region FloodingCells
    private IEnumerator Flood(Cell cell)
    {
        if (gameover || cell.revealed || cell.type == Cell.Type.Trap) yield break;

        cell.revealed = true;
        _board.Draw(_grid);
        yield return null;

        if (cell.type == Cell.Type.Empty)
        {
            FloodAdjacentCells(cell.position);
        }
    }

    private void FloodAdjacentCells(Vector3Int position)
    {
        if (_grid.TryGetCell(position.x - 1, position.y, out Cell left)) StartCoroutine(Flood(left));
        if (_grid.TryGetCell(position.x + 1, position.y, out Cell right)) StartCoroutine(Flood(right));
        if (_grid.TryGetCell(position.x, position.y - 1, out Cell down)) StartCoroutine(Flood(down));
        if (_grid.TryGetCell(position.x, position.y + 1, out Cell up)) StartCoroutine(Flood(up));
    }
    #endregion

    #region Exploding
    public void Explode(Cell cell)
    {
        TriggerGameOver(cell);
    }

    private void RevealAllMines()
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                Cell currentCell = _grid[x, y];
                if (currentCell.type == Cell.Type.Trap)
                {
                    currentCell.revealed = true;
                }
            }
        }
    }

    public void TriggerGameOver(Cell cell)
    {
        _playerBase.isActive = false;
        gameover = true;
        Debug.Log("Game Over!");

        ScreenShake.Instance.TriggerShake(1f, 6f);

        cell.exploded = true;
        cell.revealed = true;

        RevealAllMines();       

        //StartCoroutine(GameOverPanel());
    }

    #endregion

    #region CheckForWin
    public void CheckWinCondition()
    {
        bool allRevealed = true;

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                Cell cell = _grid[x, y];

                // Ignore flagged cells and only check non-trap cells
                if (cell.type != Cell.Type.Trap && !cell.revealed && !cell.flagged)
                {
                    Debug.Log($"Cell at {x},{y} is not revealed yet.");
                    allRevealed = false;
                    break; // stop checking further
                }
            }

            // If any non-trap cell isn't revealed, exit loop early
            if (!allRevealed)
            {
                break;
            }
        }

        if (allRevealed && !levelComplete)
        {
            WinGame();
        }
    }

    public void CheckWinConditionFlags()
    {
        bool allMinesFlagged = true;

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                Cell cell = _grid[x, y];

                // If a mine cell is not flagged, set allMinesFlagged to false and exit the loop
                if (cell.type == Cell.Type.Trap && !cell.flagged)
                {
                    allMinesFlagged = false;
                    Debug.Log($"Mine at {x},{y} is not flagged yet.");
                    break;
                }
            }

            // If any mine cell is not flagged, exit the outer loop
            if (!allMinesFlagged)
            {
                break;
            }
        }

        // If all mine cells are correctly flagged, the player wins
        if (allMinesFlagged && !levelComplete)
        {
            WinGame();
        }
    }
    #endregion

    #region WinState
    private void WinGame()
    {
        Debug.Log("Winner!");

        ScreenShake.Instance.TriggerShake(2f, 5f);
        magicBlock.SetActive(false);

        //solvedPanel.SetActive(true);
        levelComplete = true;
        //AudioManager.Instance.PlaySound(AudioManager.SoundType.LevelComplete, 1f);

        //LevelTimer.Instance.StopLevelTimer();

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                Cell cell = _grid[x, y];
                if (cell.type == Cell.Type.Trap)
                {
                    cell.flagged = true;
                }
            }
        }
    }
    #endregion
}
