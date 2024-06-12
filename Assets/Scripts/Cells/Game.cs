using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Game : MonoBehaviour
{
    [Header("Width")]
    public int width = 16;

    [Header("Height")]
    public int height = 16;

    [Header("Trap Count")]
    public int trapCount = 4;

    private GameObject player;

    [Header("Player's Stats")]
    public Vector2 startPos;
    public float flagCount;
    public int trapCountIncrease = 1;

    [Header("Portal Block")]
    [SerializeField] private GameObject magicBlock;

    [Header("UI")]
    public TextMeshProUGUI trapText;
    public TextMeshProUGUI flagText;

    [Header("Panels")]
    [SerializeField] private GameObject lostPanel;
    [SerializeField] private GameObject solvedPanel;

    [Header("Bools")]
    public bool gameover;
    public bool levelComplete = false;
    public bool canFlag = false;
    private bool generated;
    private Board board;
    public CellGrid grid;

    private MystBehaviour myst;
    private GaleBehaviour gale;

    private void OnValidate()
    {
        trapCount = Mathf.Clamp(trapCount, 0, width * height);
    }

    private void Awake()
    {
        Application.targetFrameRate = 60;
        board = GetComponentInChildren<Board>();

        // Update trapCount and flagCount based on selectedCharacterIndex
        if (CharacterManager.selectedCharacterIndex == 0)
        {
            trapCount += trapCountIncrease;
            flagCount += trapCountIncrease;
        }
    }

    private void Start()
    {
        UpdateMineFlagText();

        player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Debug.Log("Player object found: " + player.name);
            InitializeCharacter();
            NewGame();
        }
        else
        {
            Debug.LogWarning("Player GameObject not found!");
        }
    }

    private void InitializeCharacter()
    {
        int characterIndex = CharacterManager.selectedCharacterIndex;

        if (characterIndex == 1) //Mystic
        {
            myst = player.GetComponent<MystBehaviour>();
            if (myst != null)
            {
                Debug.Log("Myst object found: " + myst.gameObject.name);
            }
            else
            {
                Debug.LogWarning("MystBehaviour not found on player GameObject!");
            }
        }
        else if (characterIndex == 3) //Goblin
        {
            gale = player.GetComponent<GaleBehaviour>();
            if (gale != null)
            {
                Debug.Log("Gale object found: " + gale.gameObject.name);
            }
            else
            {
                Debug.LogWarning("GaleBehaviour not found on player GameObject!");
            }
        }
        else
        {
            Debug.LogWarning("CharacterManager.selectedCharacterIndex is not 1 or 3.");
        }
    }

    public void NewGame()
    {
        StopAllCoroutines();

        Camera.main.transform.position = new Vector3(width / 2f, height / 2f, -10f);

        gameover = false;
        levelComplete = false;
        generated = false;

        grid = new CellGrid(width, height);
        board.Draw(grid);

        magicBlock.SetActive(true);
        player.transform.position = startPos;

        flagCount = trapCount;

        UpdateMineFlagText();
        canFlag = false;
        lostPanel.SetActive(false);
        solvedPanel.SetActive(false);

        if (myst != null) //Mystic
        {
            myst.invincible = false;
        }

        if (gale != null) //Gale
        {
            gale.ResetPrefabNum();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.N) || Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            NewGame();
            return;
        }

        if (!gameover)
        {
            Reveal();
            if (Input.GetMouseButtonDown(1) && canFlag && !gameover && !levelComplete)
            {
                Flag();
            }
        }
    }

    private void Reveal()
    {
        if (TryGetCellAtPlayerPosition(out Cell cell))
        {
            if (!generated)
            {
                grid.GenerateMines(cell, trapCount);
                grid.GenerateNumbers();
                generated = true;
                canFlag = true;
            }

            Reveal(cell);
        }
    }

    public void Reveal(Cell cell)
    {
        if (cell.revealed || cell.flagged) return;

        switch (cell.type)
        {
            case Cell.Type.Mine:
                Explode(cell);
                break;
            case Cell.Type.Empty:
                StartCoroutine(Flood(cell));
                CheckWinCondition();
                break;
            default:
                cell.revealed = true;
                CheckWinCondition();
                break;
        }

        if (CharacterManager.selectedCharacterIndex == 3 && Random.value <= gale?.dropSpawnChance) //Gale
        {
            gale?.SpawnItemPrefab();
            ScreenShake.Instance.TriggerShake(.05f, .05f);
        }

        board.Draw(grid);
    }

    private IEnumerator Flood(Cell cell)
    {
        if (gameover || cell.revealed || cell.type == Cell.Type.Mine) yield break;

        cell.revealed = true;
        board.Draw(grid);
        yield return null;

        if (cell.type == Cell.Type.Empty)
        {
            FloodAdjacentCells(cell.position);
        }
    }

    private void FloodAdjacentCells(Vector3Int position)
    {
        if (grid.TryGetCell(position.x - 1, position.y, out Cell left)) StartCoroutine(Flood(left));
        if (grid.TryGetCell(position.x + 1, position.y, out Cell right)) StartCoroutine(Flood(right));
        if (grid.TryGetCell(position.x, position.y - 1, out Cell down)) StartCoroutine(Flood(down));
        if (grid.TryGetCell(position.x, position.y + 1, out Cell up)) StartCoroutine(Flood(up));
    }

    private void Flag()
    {
        if (!TryGetCellAtMousePosition(out Cell cell) || cell.revealed) return;

        if (cell.flagged)
        {
            cell.flagged = false;
            flagCount += 1;

            AudioManager.Instance.PlaySound(AudioManager.SoundType.FlagSpell, Random.Range(0.1f, 1.5f));
        }
        else if (flagCount > 0)
        {
            cell.flagged = true;
            flagCount -= 1;

            AudioManager.Instance.PlaySound(AudioManager.SoundType.FlagSpell, Random.Range(0.1f, 1.5f));
        }

        UpdateMineFlagText();
        
        board.Draw(grid);

        CheckWinConditionFlags();
    }

    private void Explode(Cell cell)
    {
        if (CharacterManager.selectedCharacterIndex == 1 && myst.invincible) //Mystic
        {
            FlagCell(cell);
            ScreenShake.Instance.TriggerShake(.1f, .1f);
        }
        else if (CharacterManager.selectedCharacterIndex == 4) //Goblin
        {
            if (Random.Range(0, 2) == 0)
            {
                TriggerGameOver(cell);
                AudioManager.Instance.PlaySound(AudioManager.SoundType.ShuffExplotion, 1f);
            }
            else
            {
                FlagCell(cell);
                ScreenShake.Instance.TriggerShake(.1f, .1f);
                AudioManager.Instance.PlaySound(AudioManager.SoundType.ShuffProc, 1f);
            }
        }
        else
        {
            TriggerGameOver(cell);
        }
    }

    private void FlagCell(Cell cell)
    {
        cell.flagged = true;
        cell.revealed = true;
        flagCount -= 1;
        CheckWinConditionFlags();
    }

    private void TriggerGameOver(Cell cell)
    {
        Debug.Log("Game Over!");
        lostPanel.SetActive(true);
        ScreenShake.Instance.TriggerShake(.2f, .3f);
        gameover = true;
        AudioManager.Instance.PlaySound(AudioManager.SoundType.LoseStepOnTrap, Random.Range(0.9f, 1.1f));
        cell.exploded = true;
        cell.revealed = true;
        RevealAllMines();
    }

    private void RevealAllMines()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Cell currentCell = grid[x, y];
                if (currentCell.type == Cell.Type.Mine)
                {
                    currentCell.revealed = true;
                }
            }
        }
    }

    public void CheckWinCondition()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Cell cell = grid[x, y];

                // All non-mine cells must be revealed to have won
                if (cell.type != Cell.Type.Mine && !cell.revealed)
                {
                    Debug.Log($"Cell at {x},{y} is not revealed yet.");
                    return; // no win, continue checking other cells
                }
            }
        }

        if (!levelComplete)
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
                if (cell.type == Cell.Type.Mine && !cell.flagged)
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

    private void WinGame()
    {
        Debug.Log("Winner!");
        magicBlock.SetActive(false);
        ScreenShake.Instance.TriggerShake(.1f, .1f);
        solvedPanel.SetActive(true);
        levelComplete = true;
        AudioManager.Instance.PlaySound(AudioManager.SoundType.LevelComplete, 1f);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Cell cell = grid[x, y];
                if (cell.type == Cell.Type.Mine)
                {
                    cell.flagged = true;
                }
            }
        }
    }

    private bool TryGetCellAtMousePosition(out Cell cell)
    {
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int cellPosition = board.tilemap.WorldToCell(worldPosition);
        return grid.TryGetCell(cellPosition.x, cellPosition.y, out cell);
    }

    private bool TryGetCellAtPlayerPosition(out Cell cell)
    {
        Vector3 worldPosition = player.transform.position;
        Vector3Int cellPosition = board.tilemap.WorldToCell(worldPosition);
        return grid.TryGetCell(cellPosition.x, cellPosition.y, out cell);
    }

    private void UpdateMineFlagText()
    {
        trapText.text = "Traps: " + trapCount;
        flagText.text = "Flags: " + flagCount;
    }
}
