using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GaleBehaviour : MonoBehaviour
{
    public int galeIndex;

    [Header("DropItems")]
     public int numOfItems;
    public int requirednumOfItems = 3;
    [SerializeField] private Transform effectPoint;
    public GameObject blastEffect;
    public GameObject[] dropPrefab;

    //[Header("UI")]
    private TextMeshProUGUI charactersText;
    private Game gameRules; // Reference to the Game script for accessing the tilemap
    private Board board; // Reference to the Board script for accessing the tilemap

    private void Awake()
    {
        // Find the Board GameObject and get its Board component
        GameObject boardObject = GameObject.FindGameObjectWithTag("Board");
        if (boardObject != null)
        {
            board = boardObject.GetComponent<Board>();
        }
        else
        {
            Debug.LogWarning("No GameObject with tag 'Board' found!");
        }

        GameObject gameRulesObject = GameObject.FindGameObjectWithTag("GameRules");

        if (gameRulesObject != null)
        {
            gameRules = gameRulesObject.GetComponent<Game>();
        }
        else
        {
            Debug.LogWarning("No GameObject with tag 'GameRules' found!");
        }

        // Find the TextMeshProUGUI component with the specified tag
        charactersText = GameObject.FindGameObjectWithTag("AbilityText").GetComponent<TextMeshProUGUI>();
        if (charactersText == null)
        {
            Debug.LogWarning("TextMeshProUGUI component with the specified tag not found!");
        }
    }
    private void Start()
    {
        galeIndex = CharacterManager.selectedCharacterIndex;       

        UpdateCharacterText();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.N) || Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            ResetPrefabNum();
        }

        // Check if numOfItems is 3 and the left mouse button is pressed
        if (numOfItems >= requirednumOfItems && Input.GetMouseButtonDown(0))
        {
            // Call the method to flag a random trap (mine) on the board
            FlagRandomTrap();
            Instantiate(blastEffect, effectPoint.transform.position, Quaternion.identity);

            AudioManager.Instance.PlaySound(AudioManager.SoundType.GaleBlast, UnityEngine.Random.Range(.9f, 1.1f));

            Debug.Log("blastEffect!");

            gameRules.CheckWinConditionFlags();
        }
    }

    private void FlagRandomTrap()
    {
        numOfItems -= requirednumOfItems;
        gameRules.flagCount -= 1;

        UpdateCharacterText();

        // Ensure gameRules is not null before proceeding
        if (gameRules != null)
        {
            Vector3Int characterPosition = Vector3Int.FloorToInt(transform.position);

            List<Vector3Int> nearbyMinePositions = new List<Vector3Int>();

            // Loop through nearby cells within the reveal radius
            for (int x = -10; x <= 10; x++)
            {
                for (int y = -10; y <= 10; y++)
                {
                    // Calculate the position of the cell relative to the character
                    Vector3Int cellPosition = characterPosition + new Vector3Int(x, y, 0);

                    // Check if the cell is within the grid boundaries
                    if (IsWithinGridBounds(cellPosition))
                    {
                        // Try to get the cell at the calculated position
                        if (gameRules.grid.TryGetCell(cellPosition.x, cellPosition.y, out Cell cell))
                        {
                            // Check if it's a mine cell
                            if (cell.type == Cell.Type.Mine && !cell.flagged)
                            {
                                // Add the position to the list of nearby mine positions
                                nearbyMinePositions.Add(cellPosition);
                            }
                        }
                    }
                }
            }

            // If there are nearby mine positions, randomly select one to flag
            if (nearbyMinePositions.Count > 0)
            {
                Vector3Int selectedMinePosition = nearbyMinePositions[UnityEngine.Random.Range(0, nearbyMinePositions.Count)];

                // Flag the selected mine cell
                Cell selectedMineCell = gameRules.grid.GetCell(selectedMinePosition.x, selectedMinePosition.y);
                selectedMineCell.flagged = true;

                // Redraw the board to update the flagged cell
                board.Draw(gameRules.grid);
            }
        }
        else
        {
            Debug.LogWarning("GameRules script is not assigned!");
        }
    }

    private bool IsWithinGridBounds(Vector3Int position)
    {
        return position.x >= 0 && position.x < gameRules.width && position.y >= 0 && position.y < gameRules.height;
    }

    public void SpawnItemPrefab()
    {
        // Check if the dropPrefab array is not empty
        if (dropPrefab.Length > 0)
        {
            // Get all revealed cell positions on the board
            Vector3Int[] revealedCellPositions = GetRevealedCellPositions();

            // Get the character's current cell position
            Vector3Int characterCellPosition = Vector3Int.FloorToInt(transform.position);

            // Create a list to hold valid spawn positions
            List<Vector3Int> validSpawnPositions = new List<Vector3Int>();

            // Iterate through the revealed cell positions
            foreach (Vector3Int cellPosition in revealedCellPositions)
            {
                // Exclude the character's current cell position
                if (cellPosition != characterCellPosition)
                {
                    validSpawnPositions.Add(cellPosition);
                }
            }

            // Check if there are any valid spawn positions available
            if (validSpawnPositions.Count > 0)
            {
                // Select a random valid spawn position
                Vector3Int randomSpawnPosition = validSpawnPositions[UnityEngine.Random.Range(0, validSpawnPositions.Count)];

                // Get the world position of the selected cell
                Vector3 cellWorldPosition = board.tilemap.GetCellCenterWorld(randomSpawnPosition);

                // Instantiate the randomly selected prefab at the valid spawn position
                Instantiate(dropPrefab[UnityEngine.Random.Range(0, dropPrefab.Length)], cellWorldPosition, Quaternion.identity);
            }
            else
            {
                Debug.LogWarning("No valid cells available to spawn the prefab!");
            }
        }
        else
        {
            Debug.LogWarning("Drop prefab array is empty!");
        }
    }

    private bool IsCellOccupied(Vector3 worldPosition)
    {
        // Check for other items in the same cell
        Collider2D[] colliders = Physics2D.OverlapCircleAll(worldPosition, 0.1f);
        foreach (Collider2D collider in colliders)
        {
            if (collider.CompareTag("GalesDrop"))
            {
                return true; // The cell is occupied
            }
        }
        return false; // The cell is not occupied
    }


    private Vector3Int[] GetRevealedCellPositions()
    {
        // Get all cell positions on the board
        BoundsInt bounds = board.tilemap.cellBounds;
        List<Vector3Int> revealedCellPositions = new List<Vector3Int>();

        // Iterate through all cell positions
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int cellPosition = new Vector3Int(x, y, 0);

                // Check if the cell is revealed and empty
                if (IsCellRevealedAndEmpty(cellPosition))
                {
                    // Add the revealed and empty cell position to the list
                    revealedCellPositions.Add(cellPosition);
                }
            }
        }

        return revealedCellPositions.ToArray();
    }

    private bool IsCellRevealedAndEmpty(Vector3Int cellPosition)
    {
        // Get the type of cell at the given cell position
        Cell.Type cellType = board.GetCellType(cellPosition);

        // Check if the cell type is "Empty" and revealed
        return cellType == Cell.Type.Empty && board.IsCellRevealed(cellPosition);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("GalesDrop"))
        {
            numOfItems += 1;
            Destroy(other.gameObject);

            charactersText.text = "Shards: " + numOfItems + "/3";

            AudioManager.Instance.PlaySound(AudioManager.SoundType.GalePickUp, UnityEngine.Random.Range(.9f, 1.1f));
        }
    }

    public void ResetPrefabNum()
    {
        numOfItems = 0;

        UpdateCharacterText();

        /*GameObject spawnedPrefab = GameObject.FindGameObjectWithTag("GalesDrop");
        if (spawnedPrefabs != null)
        {
            Destroy(spawnedPrefabs);
        }*/
        GameObject[] spawnedPrefabs = GameObject.FindGameObjectsWithTag("GalesDrop");
        foreach (GameObject prefab in spawnedPrefabs)
        {
            Destroy(prefab);
        }
        
        // Remove the spawned prefab if it exists
        /*if (prefabSpawned)
        {
            GameObject spawnedPrefab = GameObject.FindGameObjectWithTag("GalesDrop");
            if (spawnedPrefab != null)
            {
                Destroy(spawnedPrefab);
            }
            prefabSpawned = false;
        }*/
        // Reset other game-related variables or states as needed
    }

    private void UpdateCharacterText()
    {
        charactersText.text = "Shards: " + numOfItems + "/3";
    }
}
