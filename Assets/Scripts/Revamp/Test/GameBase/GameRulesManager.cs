using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameRulesManager : MonoBehaviour
{
    private GameCellGrid gameCellGrid;
    private NumberGenerator numberGenerator;
    private TrapGenerator trapGenerator;
    private Stats stats;
    private GameObject player;
    private TilemapVisualizer board;
    public GameCellGrid grid;
    //private CellUtility cellUtility;

    [Header("Width")]
    public int width = 8;

    [Header("Height")]
    public int height = 8;

    [Header("Trap Difficulty")]
    public float _difficulty = .2f;
    public int _trapCount;

    [Header("Player's Stats")]
    public Vector2 startPos;

    [Header("Bools")]
    public bool _gameover;
    public bool _levelComplete = false;
    //public bool _canFlag = false;
    public bool _generated;

    [Header("Portal Block")]
    [SerializeField] private GameObject magicBlock;

    //private Board board;

    private void OnValidate()
    {
        // Calculate the trapCount based on board size and difficulty
        _trapCount = Mathf.Clamp(Mathf.RoundToInt((width * height) * _difficulty), 0, width * height);
    }

    private void Awake()
    {
        stats._flagCount = _trapCount;
    }
    // Start is called before the first frame update
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Debug.Log("Player object found: " + player.name);
            //InitializeCharacter();
            //NewGame();
        }
        else
        {
            Debug.LogWarning("Player GameObject not found!");
        }
        grid = new GameCellGrid(width, height);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void NewGame()
    {
        StopAllCoroutines();

        _gameover = false;
        _levelComplete = false;
        _generated = false;

        grid = new GameCellGrid(width, height);
        board.Draw(grid);

        if (!magicBlock.activeSelf)
        {
            magicBlock.SetActive(true);
        }

        player.transform.position = startPos;
        //Instantiate(startEffect, startPos, Quaternion.identity);
        //AudioManager.Instance.PlaySound(AudioManager.SoundType.LevelStartSound, Random.Range(1f, 1.1f));

        //StartCoroutine(AnimateSkull(initialSkullPosition, initialSkullScale, 0.5f));

        stats._flagCount = _trapCount;

        //UpdateTrapFlagText();
        
        ///lostPanel.SetActive(false);
        ///solvedPanel.SetActive(false);
    }

    public void CheckWinCondition()
    {
        bool allRevealed = true;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Cell cell = grid[x, y];

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

        if (allRevealed && !_levelComplete)
        {
            WinGame();
        }
    }

    public void CheckWinConditionFlags()
    {
        bool allMinesFlagged = true;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Cell cell = grid[x, y];

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
        if (allMinesFlagged && !_levelComplete)
        {
            WinGame();
        }
    }

    public IEnumerator Flood(Cell cell)
    {
        if (_gameover || cell.revealed || cell.type == Cell.Type.Trap) yield break;

        cell.revealed = true;
        board.Draw(grid);
        yield return null;

        if (cell.type == Cell.Type.Empty)
        {
            FloodAdjacentCells(cell.position);
        }
    }

    public void FloodAdjacentCells(Vector3Int position)
    {
        if (grid.TryGetCell(position.x - 1, position.y, out Cell left)) StartCoroutine(Flood(left));
        if (grid.TryGetCell(position.x + 1, position.y, out Cell right)) StartCoroutine(Flood(right));
        if (grid.TryGetCell(position.x, position.y - 1, out Cell down)) StartCoroutine(Flood(down));
        if (grid.TryGetCell(position.x, position.y + 1, out Cell up)) StartCoroutine(Flood(up));
    }
    public void WinGame()
    {
        Debug.Log("Winner!");

        //ScreenShake.Instance.TriggerShake(2f, 5f);
        //AudioManager.Instance.PlaySound(AudioManager.SoundType.MagicBlockReleaseSound, 1f);
        //Instantiate(blockDestroyEffect, magicBlock.transform.position, Quaternion.identity);
        //magicBlock.SetActive(false);

        //solvedPanel.SetActive(true);
        _levelComplete = true;

        //LevelTimer.Instance.StopLevelTimer();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Cell cell = grid[x, y];
                if (cell.type == Cell.Type.Trap)
                {
                    cell.flagged = true;
                }
            }
        }
    }
}
