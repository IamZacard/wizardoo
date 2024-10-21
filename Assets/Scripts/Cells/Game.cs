using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Game : MonoBehaviour
{
    [Header("Width")]
    public int width = 16;

    [Header("Height")]
    public int height = 16;

    [Header("Trap Difficulty")]
    public float difficulty = .2f;
    public int trapCount;

    private GameObject player;

    [Header("Player's Stats")]
    public Vector2 startPos;
    public float flagCount;

    public int successfulReveals = 0; // To track number of successful reveals
    public int requiredReveals = 5; // N - Number of reveals before revealing an additionl cell

    [Header("Adjustment for Violet")]
    public int trapCountIncrease = 1;

    [Header("Portal Block")]
    [SerializeField] private GameObject magicBlock;

    [Header("UI")]
    public TextMeshProUGUI trapText;
    public TextMeshProUGUI flagText;

    [Header("Skull")]
    [SerializeField] private RectTransform skullImage;
    private Vector3 initialSkullPosition;
    private Vector3 initialSkullScale;

    [Header("Panels")]
    [SerializeField] private GameObject lostPanel;
    [SerializeField] private GameObject solvedPanel;

    [Header("Bools")]
    public bool gameover;
    public bool levelComplete = false;
    public bool canFlag = false;
    public bool generated;
    private Board board;
    public CellGrid grid;

    [Header("Info")]
    public int currentLevel;

    [Header("Effects")]
    [SerializeField] private GameObject revealEffect;
    [SerializeField] private GameObject TrapExplotionEffect;
    [SerializeField] private GameObject startEffect;
    [SerializeField] private GameObject blockDestroyEffect;

    private int characterIndex = CharacterManager.selectedCharacterIndex;
    private MystBehaviour myst;
    private GaleBehaviour gale;
    private GoblinBehaviour goblin;
    private GirlBehaviour violet;

    private void OnValidate()
    {
        // Calculate the trapCount based on board size and difficulty
        trapCount = Mathf.Clamp(Mathf.RoundToInt((width * height) * difficulty), 0, width * height);
    }

    private void Awake()
    {
        Application.targetFrameRate = 60;
        board = GetComponentInChildren<Board>();

        // Calculate trapCount and flagCount
        trapCount = Mathf.RoundToInt((width * height) * difficulty);
        flagCount = trapCount;

        // If the selected character is index 0 (Violet), apply the trapCountIncrease
        if (CharacterManager.selectedCharacterIndex == 0)
        {
            trapCount += trapCountIncrease;
            flagCount += trapCountIncrease;
        }

        initialSkullPosition = skullImage.localPosition;
        initialSkullScale = skullImage.localScale;
    }
    private void Start()
    {
        UpdateTrapFlagText();

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

        LevelTimer.Instance.StartLevelTimer();
    }

    private void InitializeCharacter()
    {
        if (characterIndex == 0) //Violet
        {
            violet = player.GetComponent<GirlBehaviour>();
            if (violet != null)
            {
                Debug.Log("Violet object found: " + violet.gameObject.name);
            }
            else
            {
                Debug.LogWarning("Violet not found on player GameObject!");
            }
        }
        else if (characterIndex == 1) //Mystic
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
        else if (characterIndex == 3) //Gale
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
        else if (characterIndex == 4) //Goblin
        {
            goblin = player.GetComponent<GoblinBehaviour>();
            if (goblin != null)
            {
                Debug.Log("goblin object found: " + goblin.gameObject.name);
            }
            else
            {
                Debug.LogWarning("GoblinBehaviour not found on player GameObject!");
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

        gameover = false;
        levelComplete = false;
        generated = false;

        grid = new CellGrid(width, height);
        board.Draw(grid);

        if (!magicBlock.activeSelf)
        {
            magicBlock.SetActive(true);
        }

        player.transform.position = startPos;
        Instantiate(startEffect, startPos, Quaternion.identity);
        AudioManager.Instance.PlaySound(AudioManager.SoundType.LevelStartSound, Random.Range(1f, 1.1f));

        //skullImage.localPosition = initialSkullPosition;
        //skullImage.localScale = initialSkullScale;
        StartCoroutine(AnimateSkull(initialSkullPosition, initialSkullScale, 0.5f));

        flagCount = trapCount;

        UpdateTrapFlagText();
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
        
        if (goblin != null && characterIndex == 4) //Goblin
        {
            goblin.goblinsLuck = goblin.goblinsLuckBasic;
            goblin.charactersText.text = (goblin.goblinsLuck * 100) + "% chance to flag trap";
        }
    }

    private void Update()
    {
        if ((Input.GetKeyDown(KeyCode.N) || Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) && !levelComplete)
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
                grid.GenerateTraps(cell, trapCount);
                grid.GenerateNumbers();
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

        // Increment successfulReveals after a successful reveal
        if (CharacterManager.RevealCellForStep && (cell.type != Cell.Type.Trap && !cell.flagged))
        {
            successfulReveals++;

            // Check if enough reveals have been made
            if (successfulReveals >= requiredReveals)
            {
                // Reveal random cells
                TryRevealAdjacentCell();
                successfulReveals = 0;  // Reset after revealing random cells
            }
        }

        // Gale logic (index 3)
        if (CharacterManager.selectedCharacterIndex == 3 && Random.value <= gale?.dropSpawnChance) //Gale
        {
            gale?.SpawnItemPrefab();
            ScreenShake.Instance.TriggerShake(.1f, 1f);
        }

        Instantiate(revealEffect, (cell.position + new Vector3(.5f, .5f)), Quaternion.identity);
        AudioManager.Instance.PlaySound(AudioManager.SoundType.PlateRevealSound, Random.Range(.8f, 1.2f));

        board.Draw(grid);
    }

    private void TryRevealAdjacentCell()
    {
        // Get the position of the character
        Vector3 characterPosition = player.transform.position;

        // List to store valid cell positions
        List<Vector3Int> validCellPositions = new List<Vector3Int>();

        // Loop through nearby cells within the reveal radius
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                // Calculate the position of the cell relative to the character
                Vector3Int cellPosition = new Vector3Int(Mathf.FloorToInt(characterPosition.x) + x, Mathf.FloorToInt(characterPosition.y) + y, 0);

                // Check if the cell is within the grid boundaries and not the current character's cell
                if (IsWithinBoardBounds(cellPosition) && cellPosition != new Vector3Int(Mathf.FloorToInt(characterPosition.x), Mathf.FloorToInt(characterPosition.y), 0))
                {
                    // Try to get the cell at the calculated position
                    if (grid.TryGetCell(cellPosition.x, cellPosition.y, out Cell cell))
                    {
                        if (!cell.revealed)
                        {
                            // Add the valid cell position to the list
                            validCellPositions.Add(cellPosition);
                        }
                    }
                }
            }
        }

        if (validCellPositions.Count > 0)
        {
            // Select a random cell from the list of valid cell positions
            Vector3Int randomCellPosition = validCellPositions[Random.Range(0, validCellPositions.Count)];

            // Get the cell at the random position
            if (grid.TryGetCell(randomCellPosition.x, randomCellPosition.y, out Cell randomCell))
            {
                if (randomCell.type == Cell.Type.Trap && !randomCell.revealed && !randomCell.flagged)
                {
                    // Automatically flag the Trap if it's not revealed and not already flagged
                    randomCell.flagged = true;
                }
                else if (!randomCell.revealed)
                {
                    // Reveal the cell if it's not a Trap and not already revealed
                    Reveal(randomCell);
                }

                // Update the board
                board.Draw(grid);
            }
        }
    }

    public bool IsCellRevealed(Vector3Int cellPosition)
    {
        // Assuming cellPosition is already in grid coordinates, if not, convert it
        Vector3Int gridCellPosition = cellPosition;

        // Check if the cell exists in the game grid and return its revealed state
        if (grid.TryGetCell(gridCellPosition.x, gridCellPosition.y, out Cell cell))
        {
            return cell.revealed;
        }

        // If the cell doesn't exist, return false
        return false;
    }

    private bool IsWithinBoardBounds(Vector3Int cellPosition)
    {
        // Get the bounds of the tilemap
        BoundsInt bounds = board.tilemap.cellBounds;

        // Check if the cell position is within the bounds of the tilemap
        return bounds.Contains(cellPosition);
    }

    private IEnumerator Flood(Cell cell)
    {
        if (gameover || cell.revealed || cell.type == Cell.Type.Trap) yield break;

        cell.revealed = true;
        board.Draw(grid);
        yield return null;

        if (cell.type == Cell.Type.Empty)
        {
            FloodAdjacentCells(cell.position);
            Instantiate(revealEffect, (cell.position + new Vector3(.5f, .5f)), Quaternion.identity);
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

        UpdateTrapFlagText();
        
        board.Draw(grid);

        CheckWinConditionFlags();
    }

    private void Explode(Cell cell)
    {
        if (CharacterManager.selectedCharacterIndex == 0 && violet.safeTp && violet.hasTeleported) //Violet
        {
            FlagCell(cell);
            violet.safeTp = false;
            ScreenShake.Instance.TriggerShake(.1f, 3f);
            //AudioManager.Instance.PlaySound(AudioManager.SoundType.ShuffExplotion, 1f);
        }
        else if (CharacterManager.selectedCharacterIndex == 1 && myst.invincible) //Mystic
        {
            FlagCell(cell);
            ScreenShake.Instance.TriggerShake(.1f, 3f);
        }
        else if (CharacterManager.selectedCharacterIndex == 4) //Goblin
        {
            float randomValue = Random.Range(0f, 1f); // Generate a random float between 0 and 1
            if (randomValue > goblin.goblinsLuck)
            {
                TriggerGameOver(cell);
                ScreenShake.Instance.TriggerShake(1f, 5f);
                AudioManager.Instance.PlaySound(AudioManager.SoundType.ShuffExplotion, 1f);                
            }
            else
            {
                FlagCell(cell);
                ScreenShake.Instance.TriggerShake(.5f, 5f);
                AudioManager.Instance.PlaySound(AudioManager.SoundType.ShuffProc, 1f);

                goblin.goblinsLuck -= 0.01f;
                goblin.charactersText.text = (goblin.goblinsLuck * 100) + "% chance to flag trap";
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
        CheckWinCondition();
    }

    public void TriggerGameOver(Cell cell)
    {
        gameover = true;
        Debug.Log("Game Over!");

        ScreenShake.Instance.TriggerShake(1f, 6f);
        AudioManager.Instance.PlaySound(AudioManager.SoundType.LoseStepOnTrap, Random.Range(0.9f, 1.1f));

        if (TrapExplotionEffect != null)
        {
            // Convert Vector3Int to Vector3 and add the Vector2 offset
            Vector3 explosionPosition = cell.position + new Vector3(0.5f, 0.5f, 0);
            Instantiate(TrapExplotionEffect, explosionPosition, Quaternion.identity);
        }

        cell.exploded = true;
        cell.revealed = true;

        RevealAllMines();

        StartCoroutine(GameOverPanel());
    }

    private void RevealAllMines()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Cell currentCell = grid[x, y];
                if (currentCell.type == Cell.Type.Trap)
                {
                    currentCell.revealed = true;
                }
            }
        }
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

        if (allRevealed && !levelComplete)
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
        if (allMinesFlagged && !levelComplete)
        {
            WinGame();
        }
    }
    private void WinGame()
    {
        Debug.Log("Winner!");
        
        ScreenShake.Instance.TriggerShake(2f, 5f);
        AudioManager.Instance.PlaySound(AudioManager.SoundType.MagicBlockReleaseSound, 1f);
        Instantiate(blockDestroyEffect, magicBlock.transform.position, Quaternion.identity);
        magicBlock.SetActive(false);

        solvedPanel.SetActive(true);
        levelComplete = true;
        AudioManager.Instance.PlaySound(AudioManager.SoundType.LevelComplete, 1f);

        LevelTimer.Instance.StopLevelTimer();

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

    public bool TryGetCellAtMousePosition(out Cell cell)
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

    private void UpdateTrapFlagText()
    {
        if (CharacterManager.showNumberOfTraps)
        {
            trapText.text = "Traps: " + trapCount;
        }
        else
        {
            trapText.text = "";
            //trapText.text = "Traps: ?";
        }

        //trapText.text = "Traps: " + trapCount;
        flagText.text = "Flags: " + flagCount;
    }

    private IEnumerator AnimateSkull(Vector3 targetPosition, Vector3 targetScale, float duration)
    {
        Vector3 startPosition = skullImage.localPosition;
        Vector3 startScale = skullImage.localScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            skullImage.localPosition = Vector3.Lerp(startPosition, targetPosition, elapsed / duration);
            skullImage.localScale = Vector3.Lerp(startScale, targetScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        skullImage.localPosition = targetPosition;
        skullImage.localScale = targetScale;
    }

    private IEnumerator GameOverPanel()
    {
        yield return new WaitForSeconds(1f); // Wait for N seconds
        lostPanel.SetActive(true);
        StartCoroutine(AnimateSkull(new Vector3(0, 0, 0), initialSkullScale * 3, 0.5f));
    }
}
