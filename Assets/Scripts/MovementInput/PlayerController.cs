using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    private PlayerMovement controls;
    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private Game gameRules;
    private Shrine shrine;
    public bool activePlayer;

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

    [SerializeField] private Texture2D cellSelectionCursor;

    private void Awake()
    {
        controls = new PlayerMovement();
        rb = GetComponent<Rigidbody2D>();

        activePlayer = true;

        GameObject boardObject = GameObject.FindGameObjectWithTag("Board");
        GameObject roomObject = GameObject.FindGameObjectWithTag("Room");
        GameObject wallObject = GameObject.FindGameObjectWithTag("Wall");
        GameObject gameRulesObject = GameObject.FindGameObjectWithTag("GameRules");
        GameObject shr1ne = GameObject.FindGameObjectWithTag("Shrine");

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

        if (shr1ne != null)
        {
            shrine = shr1ne.GetComponent<Shrine>();
        }
        else
        {
            Debug.LogWarning("No GameObject with tag 'Shrine' found!");
        }

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
        if (!activePlayer) return; // Prevent any actions if the player is not active

        FlagCell();

        if (gameRules != null && gameRules.gameover && !deathCountIncreased)
        {
            IncreaseDeathCount();
        }

        if (activePlayer && (Input.GetKeyDown(KeyCode.N) || Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)))
        {
            deathCountIncreased = false;
            restartCount++;
            activePlayer = true;
        }

        if (shrine != null && shrine.usingShrine && Input.GetKeyDown(KeyCode.E) && shrine.HasCharges())
        {
            activePlayer = false;
            shrine.shrineCellSelection = true;
        }

        if (shrine != null && shrine.shrineCellSelection && shrine.HasCharges())
        {
            Vector2 cursorHotspot = new Vector2(cellSelectionCursor.width / 2, cellSelectionCursor.height / 2);
            Cursor.SetCursor(cellSelectionCursor, cursorHotspot, CursorMode.Auto);

            if (Input.GetMouseButtonDown(0))
            {
                RevealCell();
            }
        }
        else
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }

        TrackTimeOnTile();
    }

    private void TrackTimeOnTile()
    {
        if (groundTileMap == null) return;

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
                if (timeSpentOnTile >= requiredTimeOnTile && alphaCoroutine == null && gameRules != null && !gameRules.levelComplete)
                {
                    alphaCoroutine = StartCoroutine(PulseAlpha());
                }
            }
        }
    }

    private bool IsNumberTile(Vector3Int cellPosition)
    {
        if (board == null || groundTileMap == null) return false;

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
        if (sr == null) return;
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
        if (!activePlayer) return; // Prevent playerController if the player is not active

        if (shrine != null && shrine.shrineCellSelection || gameRules != null && gameRules.gameover)
        {
            return;
        }

        if (CanMove(direction))
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

    private bool CanMove(Vector2 direction)
    {
        if (groundTileMap == null || colissionTileMap == null) return false;

        Vector3Int groundGridPosition = groundTileMap.WorldToCell(transform.position + (Vector3)direction);
        bool canMoveOnGround = groundTileMap.HasTile(groundGridPosition) && !colissionTileMap.HasTile(groundGridPosition);

        if (roomTileMap != null && roomTileMap.gameObject.activeSelf)
        {
            Vector3Int roomGridPosition = roomTileMap.WorldToCell(transform.position + (Vector3)direction);
            bool canMoveInRoom = roomTileMap.HasTile(roomGridPosition) && !colissionTileMap.HasTile(roomGridPosition);

            if (canMoveInRoom)
            {
                return true;
            }
        }

        return canMoveOnGround;
    }

    private Vector3 SnapPosition(Vector3 position)
    {
        position.x = Mathf.Round(position.x * 2f) / 2f;
        position.y = Mathf.Round(position.y * 2f) / 2f;
        return position;
    }

    private IEnumerator ScaleCharacter()
    {
        transform.localScale = originalScale * scaleNumber;
        yield return new WaitForSeconds(0.3f);
        transform.localScale = originalScale;
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("MagicBlock"))
        {
            transform.position = SnapPosition(transform.position);
        }
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
        }
    }

    public void FlagCell()
    {
        if (!activePlayer) return; // Prevent flagging if the player is not active

        if (Input.GetMouseButtonDown(1) && gameRules != null && gameRules.flagCount > 0 && gameRules.canFlag && !gameRules.gameover && !gameRules.levelComplete)
        {
            Instantiate(flagEffect, effectPoint.position, Quaternion.identity);
        }
    }

    private void IncreaseDeathCount()
    {
        CharacterManager.deathCount++;
        deathCountIncreased = true;

        if (deathCount != null)
        {
            deathCount.text = "Death \ncount: " + CharacterManager.deathCount;
            StartCoroutine(ScaleText(deathCount, deathCount.transform.localScale * 2, 0.5f));
        }

        Debug.Log("Death count increased. Total deaths: " + CharacterManager.deathCount);
    }

    private IEnumerator ScaleText(TextMeshProUGUI text, Vector3 targetScale, float duration)
    {
        Vector3 originalScale = text.transform.localScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            text.transform.localScale = Vector3.Lerp(originalScale, targetScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        text.transform.localScale = targetScale;
        yield return new WaitForSeconds(0.5f);

        elapsed = 0f;
        while (elapsed < duration)
        {
            text.transform.localScale = Vector3.Lerp(targetScale, originalScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        text.transform.localScale = originalScale;
    }

    private void RevealCell()
    {
        if (groundTileMap == null || shrine == null || gameRules == null || !shrine.HasCharges()) return;

        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int cellPos = groundTileMap.WorldToCell(mouseWorldPos);

        if (groundTileMap.HasTile(cellPos) && (roomTileMap == null || !roomTileMap.HasTile(cellPos)) && !colissionTileMap.HasTile(cellPos))
        {
            Cell cell;
            if (gameRules.grid.TryGetCell(cellPos.x, cellPos.y, out cell))
            {
                if (cell.type == Cell.Type.Mine && !cell.revealed && !cell.flagged)
                {
                    Debug.Log("Flagging trap cell at: " + cellPos);

                    cell.flagged = true;
                    shrine.UseCharge();
                    board.Draw(gameRules.grid);

                    AudioManager.Instance.PlaySound(AudioManager.SoundType.BoardRevealSound, Random.Range(.8f, 1.2f));

                    shrine.shrineCellSelection = false;
                    gameRules.flagCount--;
                    Debug.Log("Flag count after flagging: " + gameRules.flagCount);
                }
                else
                {
                    Debug.Log("Revealing cell at: " + cellPos);

                    gameRules.Reveal(cell);
                    shrine.shrineCellSelection = false;
                    shrine.UseCharge();

                    AudioManager.Instance.PlaySound(AudioManager.SoundType.BoardRevealSound, Random.Range(.8f, 1.2f));

                    board.Draw(gameRules.grid);
                }

                StartCoroutine(activatePlayer());
            }
        }
    }

    IEnumerator activatePlayer()
    {
        yield return new WaitForSeconds(0.5f);
        activePlayer = true;
    }
}