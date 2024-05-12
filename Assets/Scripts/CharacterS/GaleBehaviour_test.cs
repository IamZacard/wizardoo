using UnityEngine;

public class GaleBehaviour_test : MonoBehaviour
{
    private Game gameRules; // Reference to the Game script for teleportation constraints
    private Board board; // Reference to the Board script for accessing the tilemap

    public GameObject galeIllusion; // Assigned in the inspector
    public int galeIndex;
    private bool prefabSpawned = false;

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
    }

    void Start()
    {
        galeIndex = CharacterManager.selectedCharacterIndex;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetGame();
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (!gameRules.gameover && !prefabSpawned)
            {
                Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                Vector3Int cellPosition = board.tilemap.WorldToCell(worldPosition);

                if (IsWithinBoardBounds(cellPosition))
                {
                    Vector3Int spawnPosition = ClampToBoardBounds(cellPosition);
                    InstantiatePrefabAtCell(spawnPosition);
                    prefabSpawned = true;
                }
                else
                {
                    Debug.LogWarning("Prefab can only be spawned on the board!");
                }
            }
        }
    }

    private void InstantiatePrefabAtCell(Vector3Int cellPosition)
    {
        Vector3 cellCenterWorld = board.tilemap.GetCellCenterWorld(cellPosition);
        Instantiate(galeIllusion, cellCenterWorld, Quaternion.identity);
    }

    private bool IsWithinBoardBounds(Vector3Int cellPosition)
    {
        BoundsInt bounds = board.tilemap.cellBounds;
        return bounds.Contains(cellPosition);
    }

    private Vector3Int ClampToBoardBounds(Vector3Int cellPosition)
    {
        BoundsInt bounds = board.tilemap.cellBounds;
        return new Vector3Int(
            Mathf.Clamp(cellPosition.x, bounds.xMin, bounds.xMax),
            Mathf.Clamp(cellPosition.y, bounds.yMin, bounds.yMax),
            cellPosition.z
        );
    }

    private void ResetGame()
    {
        // Remove the spawned prefab if it exists
        if (prefabSpawned)
        {
            Destroy(GameObject.FindGameObjectWithTag("GaleIllusion"));
            prefabSpawned = false;
        }

        // Reset other game-related variables or states as needed
    }
}
