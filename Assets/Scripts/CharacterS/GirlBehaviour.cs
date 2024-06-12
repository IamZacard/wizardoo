using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class GirlBehaviour : MonoBehaviour
{
    private Game gameRules; // Reference to the Game script for teleportation constraints
    private Board board; // Reference to the Board script for accessing the tilemap

    [SerializeField] private GameObject tpEffect;
    //[Header("UI")]
    private TextMeshProUGUI charactersText;

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

        // Log a warning if the TextMeshProUGUI component is not found
        if (charactersText == null)
        {
            Debug.LogWarning("TextMeshProUGUI component with the specified tag not found!");
        }

        charactersText.text = "Click on the cell to teleport there";
    }

    private void Update()
    {
        // Check if the left mouse button is pressed
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            // Get the world position of the mouse click
            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            // Convert the world position to grid position
            Vector3Int cellPosition = board.tilemap.WorldToCell(worldPosition);
            if (!gameRules.gameover)
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
                        ScreenShake.Instance.TriggerShake(.05f, .1f);
                        AudioManager.Instance.PlaySound(AudioManager.SoundType.VioletTeleport, Random.Range(.7f, 1f));
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
}
