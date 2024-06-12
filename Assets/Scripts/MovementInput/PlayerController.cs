using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    private PlayerMovement controls;
    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private Game gameRules;

    [SerializeField] private Transform effectPoint;
    [SerializeField] private GameObject flagEffect;
    [SerializeField] private GameObject stepEffect;
    [SerializeField] private GameObject pickUpEffect;
    private Tilemap groundTileMap;
    private Tilemap roomTileMap;
    private Tilemap colissionTileMap;

    private TextMeshProUGUI deathCount;
    private TextMeshProUGUI stepsCount;

    private bool deathCountIncreased = false;
    private int restartCount = 0;

    private Vector3Int currentCellPosition;
    private float timeSpentOnTile = 0f;
    private float requiredTimeOnTile = 3f;

    private Vector3 originalScale;
    public float scaleNumber = 1.2f;

    private Board board;
    private Coroutine alphaCoroutine;

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
            board = boardObject.GetComponent<Board>();
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
        deathCount = GameObject.FindGameObjectWithTag("DeathCount")?.GetComponent<TextMeshProUGUI>();
        if (deathCount != null)
        {
            deathCount.text = "Death count: " + CharacterManager.deathCount;
        }
        else
        {
            Debug.LogWarning("TextMeshProUGUI component with the specified tag not found!");
        }
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

        originalScale = transform.localScale;
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

        TrackTimeOnTile();
    }

    private void TrackTimeOnTile()
    {
        Vector3Int newCellPosition = groundTileMap.WorldToCell(transform.position);

        if (newCellPosition != currentCellPosition)
        {
            if (alphaCoroutine != null)
            {
                StopCoroutine(alphaCoroutine);
                ResetCharacterAlpha();
            }
            currentCellPosition = newCellPosition;
            timeSpentOnTile = 0f;
            SetCharacterAlpha(255);
        }
        else
        {
            if (IsNumberTile(currentCellPosition))
            {
                timeSpentOnTile += Time.deltaTime;
                if (timeSpentOnTile >= requiredTimeOnTile && alphaCoroutine == null && !gameRules.levelComplete)
                {
                    alphaCoroutine = StartCoroutine(PulseAlpha());
                }
            }
        }
    }

    private bool IsNumberTile(Vector3Int cellPosition)
    {
        if (board == null) return false;

        TileBase tile = groundTileMap.GetTile(cellPosition);
        return tile == board.tileNum1 || tile == board.tileNum2 || tile == board.tileNum3 ||
               tile == board.tileNum4 || tile == board.tileNum5 || tile == board.tileNum6 ||
               tile == board.tileNum7 || tile == board.tileNum8;
    }

    private void ResetCharacterAlpha()
    {
        SetCharacterAlpha(255);
    }

    private void SetCharacterAlpha(byte alpha)
    {
        Color color = sr.color;
        color.a = alpha / 255f;
        sr.color = color;
    }

    private IEnumerator PulseAlpha()
    {
        while (true)
        {
            SetCharacterAlpha(30);
            yield return new WaitForSeconds(1f);
            SetCharacterAlpha(255);
            yield return new WaitForSeconds(1.5f);
        }
    }

    private void Move(Vector2 direction)
    {
        if (CanMove(direction) && !gameRules.gameover)
        {
            if (alphaCoroutine != null)
            {
                StopCoroutine(alphaCoroutine);
                ResetCharacterAlpha();
                alphaCoroutine = null;
            }

            Vector3 stepPosition = transform.position + new Vector3(0f, -0.24f, 0f);
            Instantiate(stepEffect, stepPosition, Quaternion.identity);
            AudioManager.Instance.PlaySound(AudioManager.SoundType.FootStepSound, Random.Range(1.5f, 1.9f));

            transform.position += (Vector3)direction;
            transform.position = SnapPosition(transform.position); // Apply snapping here

            StartCoroutine(ScaleCharacter()); // Scale character on move

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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("MagicBlock"))
        {
            transform.position = SnapPosition(transform.position); // Apply snapping on collision
        }
    }


    private bool CanMove(Vector2 direction)
    {
        // Convert the player's potential grid position on the groundTileMap
        Vector3Int groundGridPosition = groundTileMap.WorldToCell(transform.position + (Vector3)direction);

        // Check if the ground tile is present and there are no collision tiles on the groundTileMap
        bool canMoveOnGround = groundTileMap.HasTile(groundGridPosition) && !colissionTileMap.HasTile(groundGridPosition);

        // If roomTileMap is not null and active, check for movement on the roomTileMap
        if (roomTileMap != null && roomTileMap.gameObject.activeSelf)
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

    private Vector3 SnapPosition(Vector3 position)
    {
        position.y = Mathf.Round(position.y * 2f) / 2f;
        return position;
    }

    private IEnumerator ScaleCharacter()
    {
        // Scale down to scaleNumber
        transform.localScale = originalScale * scaleNumber;

        // Wait for N seconds
        yield return new WaitForSeconds(0.3f);

        // Scale back to original
        transform.localScale = originalScale;
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
            Instantiate(pickUpEffect, effectPoint.position, Quaternion.identity);
            AudioManager.Instance.PlaySound(AudioManager.SoundType.GalePickUp, Random.Range(.1f, 1.5f));

            // Load the mini-game scene
            //SceneManager.LoadScene("Mini-Game_Scene", LoadSceneMode.Additive);
        }
    }

    public void FlagCell()
    {
        if (Input.GetMouseButtonDown(1) && gameRules.flagCount > 0 && gameRules.canFlag && !gameRules.gameover && !gameRules.levelComplete)
        {
            Instantiate(flagEffect, effectPoint.position, Quaternion.identity);
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
