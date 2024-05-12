using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerController : MonoBehaviour
{
    private PlayerMovement controls;
    private SpriteRenderer sr;
    private Rigidbody2D rb;
    public Game gameRules;

    [SerializeField] private Transform effectPoint;
    [SerializeField] private GameObject flagEffect;
    [SerializeField] private GameObject stepEffect;
    [SerializeField] private Tilemap groundTileMap;
    private Tilemap roomTileMap;
    [SerializeField] private Tilemap colissionTileMap;

    [SerializeField] private TextMeshProUGUI deathCount;
    [SerializeField] private TextMeshProUGUI stepsCount;

    private bool deathCountIncreased = false;
    private int restartCount = 0;

    private void Awake()
    {
        controls = new PlayerMovement();
        rb = GetComponent<Rigidbody2D>();

        // Find GameObjects with the corresponding tags and get Tilemap components
        GameObject boardObject = GameObject.FindGameObjectWithTag("Board");
        GameObject roomObject = GameObject.FindGameObjectWithTag("Room");
        GameObject wallObject = GameObject.FindGameObjectWithTag("Wall");
        GameObject gameRulesObject = GameObject.FindGameObjectWithTag("GameRules");

        if (boardObject != null)
        {
            groundTileMap = boardObject.GetComponent<Tilemap>();
        }
        else
        {
            Debug.LogWarning("No GameObject with tag 'Board' found!");
        }

        if (roomObject != null)
        {
            roomTileMap = roomObject.GetComponent<Tilemap>();
        }
        else
        {
            Debug.LogWarning("No GameObject with tag 'Room' found!");
        }

        if (wallObject != null)
        {
            colissionTileMap = wallObject.GetComponent<Tilemap>();
        }
        else
        {
            Debug.LogWarning("No GameObject with tag 'Wall' found!");
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
        deathCount = GameObject.FindGameObjectWithTag("DeathCount").GetComponent<TextMeshProUGUI>();
        if (deathCount == null)
        {
            Debug.LogWarning("TextMeshProUGUI component with the specified tag not found!");
        }
        deathCount.text = "Death count: " + CharacterManager.deathCount;
    }

    private void OnEnable()
    {
        controls.Enable();
    }

    private void OnDisable()
    {
        controls.Disable();
    }

    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        controls.Main.Movement.performed += ctx => Move(ctx.ReadValue<Vector2>());
        controls.Main.AbilityUsage.performed += ctx => FlagCell();
        Debug.Log("Start");
    }

    private void Update()
    {
        FlagCell();

        if (gameRules.gameover && !deathCountIncreased)
        {
            IncreaseDeathCount();
        }

        if (Input.GetKeyDown(KeyCode.N) || Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            deathCountIncreased = false;
            restartCount++;
        }
    }

    private void Move(Vector2 direction)
    {
        if (CanMove(direction) && !gameRules.gameover)
        {
            Vector3 stepPosition = transform.position + new Vector3(0f, -0.24f, 0f);
            Instantiate(stepEffect, stepPosition, Quaternion.identity);
            AudioManager.Instance.PlaySound(AudioManager.SoundType.FootStepSound, Random.Range(1.5f, 1.9f));

            transform.position += (Vector3)direction;

            if (direction.x > 0)
            {
                sr.flipX = true;
            }
            else if (direction.x < 0)
            {
                sr.flipX = false;
            }
        }        
    }

    private bool CanMove(Vector2 direction)
    {
        // Convert the player's potential grid position on the groundTileMap and roomTileMap
        Vector3Int groundGridPosition = groundTileMap.WorldToCell(transform.position + (Vector3)direction);

        // Check if the ground tile is present and there are no collision tiles on the groundTileMap
        bool canMoveOnGround = groundTileMap.HasTile(groundGridPosition) && !colissionTileMap.HasTile(groundGridPosition);

        // If roomTileMap is not null, check for movement on the roomTileMap
        if (roomTileMap != null)
        {
            // Convert the player's potential grid position on the roomTileMap
            Vector3Int roomGridPosition = roomTileMap.WorldToCell(transform.position + (Vector3)direction);

            // Check if the room tile is present and there are no collision tiles on the roomTileMap
            bool canMoveInRoom = roomTileMap.HasTile(roomGridPosition) && !colissionTileMap.HasTile(roomGridPosition);

            // Return true if movement is allowed on the roomTileMap
            if (canMoveInRoom)
            {
                return true;
            }
        }

        // Return true if movement is allowed on the groundTileMap
        return canMoveOnGround;
    }


    private void OnCollisionExit2D(Collision2D other)
    {
        rb.velocity = Vector3.zero;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("JumpyChest"))
        {
            Destroy(other.gameObject);
            AudioManager.Instance.PlaySound(AudioManager.SoundType.GalePickUp, Random.Range(.1f, 1.5f));
        }
    }
    public void FlagCell()
    {
        if (Input.GetMouseButtonDown(1) && gameRules.flagCount > 0 && gameRules.canFlag && !gameRules.gameover)
        {
            Instantiate(flagEffect, effectPoint.position, Quaternion.identity);
            AudioManager.Instance.PlaySound(AudioManager.SoundType.FlagSpell, Random.Range(.1f,1.5f));
            // Decrement flag count or update game state as needed
        }
    }
    private void IncreaseDeathCount()
    {
        // Increase the death count when game over
        CharacterManager.deathCount++;
        deathCountIncreased = true;

        deathCount.text = "Death count: " + CharacterManager.deathCount;
        Debug.Log("Death count increased. Total deaths: " + CharacterManager.deathCount);
    }
}
