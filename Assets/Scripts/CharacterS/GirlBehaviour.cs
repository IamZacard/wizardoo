using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GirlBehaviour : MonoBehaviour
{
    private Game gameRules; // Reference to the Game script for teleportation constraints
    private Board board; // Reference to the Board script for accessing the tilemap
    private PlayerController movement;

    [SerializeField] private GameObject tpEffect;
    //[Header("UI")]
    private TextMeshProUGUI charactersText;

    // References for the upgrade panel and buttons
    public GameObject upgradePanel;
    public Button upgradeButton1;
    public Button upgradeButton2;
    public Button upgradeButton3;

    private Vector3 originalScale;
    private bool chanceToRevealCellSelected = false;
    public float chanceNumber = 0.3f; //30% Chance to reveal
    private void Awake()
    {
        // Find GameObjects with the corresponding tags
        GameObject boardObject = GameObject.FindGameObjectWithTag("Board");
        GameObject gameRulesObject = GameObject.FindGameObjectWithTag("GameRules");

        // Get the components from the GameObjects
        if (boardObject != null)
        {
            board = boardObject.GetComponent<Board>();
        }
        else
        {
            Debug.LogWarning("No GameObject with tag 'Board' found!");
        }

        if (gameRulesObject != null)
        {
            gameRules = gameRulesObject.GetComponent<Game>();
        }
        else
        {
            Debug.LogWarning("No GameObject with tag 'GameRules' found!");
        }

        // Find the TextMeshProUGUI component with the specified tag
        TextMeshProUGUI[] textComponents = FindObjectsOfType<TextMeshProUGUI>();
        foreach (TextMeshProUGUI textComponent in textComponents)
        {
            if (textComponent.CompareTag("AbilityText"))
            {
                charactersText = textComponent;
                break; // Exit the loop once the desired text is found
            }
        }

        movement = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerController>();
        if (charactersText == null)
        {
            Debug.LogWarning("No GameObject with tag 'Player' found!");
        }

        // Log a warning if the TextMeshProUGUI component is not found
        if (charactersText == null)
        {
            Debug.LogWarning("TextMeshProUGUI component with the specified tag not found!");
        }

        charactersText.text = "Click on the cell to teleport there";

        upgradePanel.SetActive(false);
        SetButtonLabels();
        // Set the original scale for buttons
        originalScale = upgradeButton1.transform.localScale;
    }

    private void Start()
    {
        // Listen to the UpgradeReady event from the UpgradeManager
        UpgradeManager.Instance.OnUpgradeReady.AddListener(OpenUpgradePanel);
    }

    private void Update()
    {
        // Check if the left mouse button is pressed
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject() && movement.activePlayer)
        {
            // Get the world position of the mouse click
            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            // Convert the world position to grid position
            Vector3Int cellPosition = board.tilemap.WorldToCell(worldPosition);
            if (!gameRules.gameover && movement.activePlayer)
            {
                // Check if the cell position is within the bounds of the board
                if (IsWithinBoardBounds(cellPosition) && board.tilemap.HasTile(cellPosition))
                {
                    // Check if the cell position is valid and not a "Floor" cell
                    if (IsTeleportationAllowed(cellPosition))
                    {
                        // Teleport the girl to the clicked cell
                        transform.position = board.tilemap.GetCellCenterWorld(cellPosition);
                        Instantiate(tpEffect, transform.position, Quaternion.identity);
                        ScreenShake.Instance.TriggerShake(.05f, .5f);
                        AudioManager.Instance.PlaySound(AudioManager.SoundType.VioletTeleport, Random.Range(.7f, 1f));

                        // Check for chance to reveal adjacent cell
                        if (chanceToRevealCellSelected && Random.value <= chanceNumber && !IsCellRevealed(cellPosition))
                        {
                            Debug.Log("Chance to reveal cell triggered.");
                            TryRevealAdjacentCell();
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Teleportation to a 'Floor' cell is not allowed!");
                        AudioManager.Instance.PlaySound(AudioManager.SoundType.ErrorSound, Random.Range(.7f, 1f));
                    }
                }
                else
                {
                    Debug.LogWarning("Teleportation outside of the board is not allowed!");
                    AudioManager.Instance.PlaySound(AudioManager.SoundType.ErrorSound, Random.Range(.7f, 1f));
                }
            }
        }
    }

    private bool IsWithinBoardBounds(Vector3Int cellPosition)
    {
        // Get the bounds of the tilemap
        BoundsInt bounds = board.tilemap.cellBounds;

        // Check if the cell position is within the bounds of the tilemap
        return bounds.Contains(cellPosition);
    }

    private bool IsTeleportationAllowed(Vector3Int cellPosition)
    {
        // Get the type of cell at the given cell position
        Cell.Type cellType = board.GetCellType(cellPosition);

        // Check if the cell type is not "Floor"
        return cellType != Cell.Type.Floor;
    }

    private void SelectUpgrade(int index)
    {
        Debug.Log("Upgrade option " + index + " selected.");
        //AudioManager.Instance.PlaySound(AudioManager.SoundType.ShuffProc, 1f);
        upgradePanel.SetActive(false);

        upgradeButton1.onClick.RemoveAllListeners();
        upgradeButton2.onClick.RemoveAllListeners();
        upgradeButton3.onClick.RemoveAllListeners();

        switch (index)
        {
            case 1:
                chanceToRevealCellSelected = true;
                break;
            case 2:
                DecreaseTrapCount();
                break;
            case 3:
                DisableMagicBlock();
                break;
        }
    }

    private void TryRevealAdjacentCell()
    {
        int newFlagsAdded = 0; // Count the number of new flags added

        // Get the position of the character
        Vector3 characterPosition = transform.position;

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
                    if (gameRules.grid.TryGetCell(cellPosition.x, cellPosition.y, out Cell cell))
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
            if (gameRules.grid.TryGetCell(randomCellPosition.x, randomCellPosition.y, out Cell randomCell))
            {
                if (randomCell.type == Cell.Type.Mine && !randomCell.revealed && !randomCell.flagged)
                {
                    // Automatically flag the mine if it's not revealed and not already flagged
                    randomCell.flagged = true;
                    newFlagsAdded++; // Increase new flag count
                }
                else if (!randomCell.revealed)
                {
                    // Reveal the cell if it's not a mine and not already revealed
                    gameRules.Reveal(randomCell);
                }

                // Update the board
                board.Draw(gameRules.grid);
            }
        }

        gameRules.flagCount -= newFlagsAdded; // Deduct the number of new flags added from the flag count
    }

    public bool IsCellRevealed(Vector3Int cellPosition)
    {
        // Assuming cellPosition is already in grid coordinates, if not, convert it
        Vector3Int gridCellPosition = cellPosition;

        // Check if the cell exists in the game grid and return its revealed state
        if (gameRules.grid.TryGetCell(gridCellPosition.x, gridCellPosition.y, out Cell cell))
        {
            return cell.revealed;
        }

        // If the cell doesn't exist, return false
        return false;
    }


    private void RevealCell(Vector3Int cellPosition)
    {
        // Assuming you have a method in the Board class to reveal a cell
        board.RevealCell(cellPosition);

        // Convert the cell position to world position
        Vector3 worldPosition = board.tilemap.GetCellCenterWorld(cellPosition);

        // Instantiate the teleport effect at the world position
        Instantiate(tpEffect, worldPosition, Quaternion.identity);

        Debug.Log("Revealed cell at position: " + cellPosition);
    }


    private void DecreaseTrapCount()
    {
        // Assuming there's a GameManager or LevelManager that handles the trap count
        gameRules.trapCountIncrease = -2;
        Debug.Log("Trap count decreased by 2 for the next levels");
    }

    private void DisableMagicBlock()
    {
        GameObject magicBlock = GameObject.FindGameObjectWithTag("MagicBlock");
        if (magicBlock != null)
        {
            magicBlock.SetActive(false);
            Debug.Log("Magic block disabled");
        }
    }

    private void OnDestroy()
    {
        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.OnUpgradeReady.RemoveListener(OpenUpgradePanel);
        }
    }

    private void SetButtonLabels()
    {
        upgradeButton1.GetComponentInChildren<TextMeshProUGUI>().text = "Chance To Reveal Cell Next to Violet";
        upgradeButton2.GetComponentInChildren<TextMeshProUGUI>().text = "Reduce traps in the next levels by 2";
        upgradeButton3.GetComponentInChildren<TextMeshProUGUI>().text = "Disable Magic Block";
    }

    private void OpenUpgradePanel()
    {
        upgradePanel.SetActive(true);
        upgradeButton1.onClick.AddListener(() => SelectUpgrade(1));
        upgradeButton2.onClick.AddListener(() => SelectUpgrade(2));
        upgradeButton3.onClick.AddListener(() => SelectUpgrade(3));
    }

    public void ScaleOn(Button button)
    {
        StartCoroutine(ScaleButton(button.gameObject, originalScale, 1.25f));
        AudioManager.Instance.PlaySound(AudioManager.SoundType.ButtonClick, 1f);
    }

    public void ScaleOff(Button button)
    {
        StartCoroutine(ScaleButton(button.gameObject, originalScale, 1f));
    }

    private IEnumerator ScaleButton(GameObject button, Vector3 originalScale, float scaleMultiplier)
    {
        RectTransform rectTransform = button.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            Vector3 targetScale = originalScale * scaleMultiplier;
            Vector3 startScale = rectTransform.localScale;
            float elapsedTime = 0f;
            float scaleDuration = 0.2f;

            while (elapsedTime < scaleDuration)
            {
                rectTransform.localScale = Vector3.Lerp(startScale, targetScale, elapsedTime / scaleDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            rectTransform.localScale = targetScale;
        }
    }
}
