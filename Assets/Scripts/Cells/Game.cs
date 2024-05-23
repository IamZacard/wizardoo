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

    //[Header("PlayerObject")]
    //[SerializeField] private GameObject player;
    private GameObject player;

    [Header("Player's stats")]
    public Vector2 startPos;
    public float flagCount;
    //violet
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
    public bool levelcomplete = false;
    public bool canFlag = false;
    private bool generated;
    private Board board;
    public CellGrid grid;

    //myst && gale
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
        Debug.Log("CharacterManager.selectedCharacterIndex: " + CharacterManager.selectedCharacterIndex);
        if (player != null)
        {
            if (CharacterManager.selectedCharacterIndex == 1)
            {
                // Attempt to get the MystBehaviour component
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
            else
            {
                Debug.LogWarning("CharacterManager.selectedCharacterIndex is not 1.");
            }

            if (CharacterManager.selectedCharacterIndex == 3)
            {
                // Attempt to get the GaleBehaviour component
                gale = player.GetComponent<GaleBehaviour>();
                if (myst != null)
                {
                    Debug.Log("Gale object found: " + myst.gameObject.name);
                }
                else
                {
                    Debug.LogWarning("GaleBehaviour not found on player GameObject!");
                }
            }
            else
            {
                Debug.LogWarning("CharacterManager.selectedCharacterIndex is not 3.");
            }

            NewGame();
            Debug.Log("Player object found: " + player.name);            
        }
        else
        {
            Debug.LogWarning("Player GameObject not found!");
        }
    }
    public void NewGame()
    {
        StopAllCoroutines();

        Camera.main.transform.position = new Vector3(width / 2f, height / 2f, -10f);

        gameover = false;
        levelcomplete = false;
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

        // Initialize myst if selectedCharacterIndex is 1 and myst is not already initialized
        if (CharacterManager.selectedCharacterIndex == 1 && myst == null)
        {
            myst = player.GetComponent<MystBehaviour>();
            if (myst != null)
            {
                Debug.Log("MystBehaviour component found on player GameObject.");
            }
            else
            {
                Debug.LogWarning("MystBehaviour component not found on player GameObject!");
            }
        }

        // Reset invincible if myst is initialized
        if (myst != null)
        {
            myst.invincible = false;
        }

        if(CharacterManager.selectedCharacterIndex == 3)
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
            if (Input.GetMouseButtonDown(1) && (canFlag) && !gameover && !levelcomplete)
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
        if (cell.revealed) return;
        if (cell.flagged) return;

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

        // Check if the selected character index is 3
        if (CharacterManager.selectedCharacterIndex == 3)
        {
            // Generate a random number between 0 and 1
            float randomValue = Random.value;

            // Check if the random value is less than or equal to 0.3 (60% chance)
            if (randomValue <= 0.6f)
            {
                // Call the SpawnItemPrefab() method
                gale.SpawnItemPrefab();
            }
        }

        board.Draw(grid);
    }

    private IEnumerator Flood(Cell cell)
    {
        if (gameover) yield break;
        if (cell.revealed) yield break;
        if (cell.type == Cell.Type.Mine) yield break;

        cell.revealed = true;
        board.Draw(grid);

        yield return null;

        if (cell.type == Cell.Type.Empty)
        {
            if (grid.TryGetCell(cell.position.x - 1, cell.position.y, out Cell left)) {
                StartCoroutine(Flood(left));
            }
            if (grid.TryGetCell(cell.position.x + 1, cell.position.y, out Cell right)) {
                StartCoroutine(Flood(right));
            }
            if (grid.TryGetCell(cell.position.x, cell.position.y - 1, out Cell down)) {
                StartCoroutine(Flood(down));
            }
            if (grid.TryGetCell(cell.position.x, cell.position.y + 1, out Cell up)) {
                StartCoroutine(Flood(up));
            }
        }
    }

    private void Flag()
    {
        if (!TryGetCellAtMousePosition(out Cell cell)) return;

        // Check if the cell is revealed before allowing the player to flag it
        if (cell.revealed) return;

        // If the cell is already flagged, remove the flag and increase flagCount
        if (cell.flagged)
        {
            cell.flagged = false;
            flagCount += 1;
            UpdateMineFlagText();

            AudioManager.Instance.PlaySound(AudioManager.SoundType.FlagSpell, Random.Range(.1f, 1.5f));
        }
        else // If the cell is not flagged
        {
            // Check if flagCount is greater than 0 before allowing the player to place a flag
            if (flagCount > 0)
            {
                cell.flagged = true;
                flagCount -= 1;
                UpdateMineFlagText();

                AudioManager.Instance.PlaySound(AudioManager.SoundType.FlagSpell, Random.Range(.1f, 1.5f));
            }
        }

        board.Draw(grid);

        // Check for win conditions
        CheckWinConditionFlags();
    }

    private void Explode(Cell cell)
    {
        if (CharacterManager.selectedCharacterIndex == 1 && myst.invincible) //Mystic
        {
            cell.flagged = true; // Flag the cell
            cell.revealed = true; // Mark the cell as revealed
            //myst.invincible = true; // Set the flag to true to indicate that the action has been performed
            flagCount -= 1;
            CheckWinConditionFlags();
        }
        else if (CharacterManager.selectedCharacterIndex == 4) //Goblin
        {
            // 50% chance to either explode or flag the mine
            if (Random.Range(0, 2) == 0)
            {
                Debug.Log("Trap Exploded!");
                AudioManager.Instance.PlaySound(AudioManager.SoundType.ShuffExplotion, Random.Range(.9f, 1.1f));
                lostPanel.SetActive(true);
                gameover = true;

                // Set the mine as exploded
                cell.exploded = true;
                cell.revealed = true;

                CheckWinConditionFlags();

                // Reveal all other mines
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
            else
            {
                // 50% chance to flag the mine
                Debug.Log("Mine Flagged!");
                AudioManager.Instance.PlaySound(AudioManager.SoundType.ShuffProc, Random.Range(.9f, 1.1f));
                cell.flagged = true;
                flagCount -= 1;
                CheckWinConditionFlags();
            }
        }
        else
        {
            Debug.Log("Game Over!");
            lostPanel.SetActive(true);
            gameover = true;
            AudioManager.Instance.PlaySound(AudioManager.SoundType.LoseStepOnTrap, Random.Range(.9f, 1.1f));

            // Set the mine as exploded
            cell.exploded = true;
            cell.revealed = true;

            // Reveal all other mines
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
                    return; // no win, continue checking other cells
                }
            }
        }

        if (!levelcomplete)
        {
            Debug.Log("Winner!");
            magicBlock.SetActive(false);
            solvedPanel.SetActive(true);

            levelcomplete = true;
            AudioManager.Instance.PlaySound(AudioManager.SoundType.LevelComplete, Random.Range(.9f, 1.1f));

            // Flag all the mines
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
        
    }

    public void CheckWinConditionFlags()
    {
        bool allMinesFlagged = true;

        // Check if all mine cells are flagged
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Cell cell = grid[x, y];

                // If a mine cell is not flagged, set allMinesFlagged to false and exit the loop
                if (cell.type == Cell.Type.Mine && !cell.flagged)
                {
                    allMinesFlagged = false;
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
        if (allMinesFlagged && !levelcomplete)
        {
            Debug.Log("Winner Miner!");
            AudioManager.Instance.PlaySound(AudioManager.SoundType.LevelComplete, Random.Range(.9f, 1.1f));
            levelcomplete = true;            
            
            magicBlock.SetActive(false);
            canFlag = false;
            solvedPanel.SetActive(true);
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
        // Convert the player's world position to grid position
        Vector3 worldPosition = player.transform.position;
        Vector3Int cellPosition = board.tilemap.WorldToCell(worldPosition);
    
        // Try to get the cell at the player's position
        return grid.TryGetCell(cellPosition.x, cellPosition.y, out cell);
    }

    private void UpdateMineFlagText()
    {
        // Update the text to display mine and flag counts
        trapText.text = "Traps: " + trapCount;
        flagText.text = "Flags: " + flagCount;
    }
}
