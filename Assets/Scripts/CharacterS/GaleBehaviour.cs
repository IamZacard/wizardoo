using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GaleBehaviour : MonoBehaviour
{
    public int galeIndex;
    private PlayerController movement;

    [Header("Drop Items")]
    public int numOfItems;
    public int requiredNumOfItems = 3;
    public float dropSpawnChance = .7f;
    public GameObject[] dropPrefab;

    [SerializeField] private Transform effectPoint;
    public GameObject blastEffect;

    [SerializeField] private Image spellIcon;
    public Color readyColor = Color.white;
    public Color notReadyColor = Color.grey;
    public float pulseDuration = 2f;

    private Coroutine pulseCoroutine;
    private TextMeshProUGUI charactersText;
    private Game gameRules;
    private Board board;
    private List<Vector3Int> usedSpawnPositions = new List<Vector3Int>();

    [Header("Upgrade Panel")]
    public GameObject upgradePanel;
    public Button upgradeButton1;
    public Button upgradeButton2;
    public Button upgradeButton3;

    private Vector3 originalScale;

    private void Awake()
    {
        if (!GameObject.FindGameObjectWithTag("Board").TryGetComponent(out board))
        {
            Debug.LogWarning("No GameObject with tag 'Board' found!");
        }

        if (!GameObject.FindGameObjectWithTag("GameRules").TryGetComponent(out gameRules))
        {
            Debug.LogWarning("No GameObject with tag 'GameRules' found!");
        }

        charactersText = GameObject.FindGameObjectWithTag("AbilityText")?.GetComponent<TextMeshProUGUI>();
        if (charactersText == null)
        {
            Debug.LogWarning("TextMeshProUGUI component with the specified tag not found!");
        }

        movement = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerController>();
        if (movement == null)
        {
            Debug.LogWarning("No GameObject with tag 'Player' found!");
        }
    }

    private void Start()
    {
        galeIndex = CharacterManager.selectedCharacterIndex;
        UpdateCharacterText();
        UpdateSpellIcon();

        upgradePanel.SetActive(false);
        SetButtonLabels();

        // Set the original scale for buttons
        originalScale = upgradeButton1.transform.localScale;

        // Register to the upgrade ready event
        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.OnUpgradeReady.AddListener(OpenUpgradePanel);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.N) || Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            ResetPrefabNum();
        }

        if (numOfItems >= requiredNumOfItems && Input.GetMouseButtonDown(0) && !gameRules.levelComplete && !EventSystem.current.IsPointerOverGameObject() && movement.activePlayer)
        {
            FlagRandomTrap();
            Instantiate(blastEffect, effectPoint.transform.position, Quaternion.identity);
            ScreenShake.Instance.TriggerShake(1f, 1f);
            AudioManager.Instance.PlaySound(AudioManager.SoundType.GaleBlast, 1f);
            Debug.Log("blastEffect!");
            gameRules.CheckWinConditionFlags();
        }

        UpdateSpellIcon();
    }

    private void FlagRandomTrap()
    {
        if (gameRules == null)
        {
            Debug.LogWarning("GameRules script is not assigned!");
            return;
        }

        numOfItems -= requiredNumOfItems;
        gameRules.flagCount -= 1;
        UpdateCharacterText();

        Vector3Int characterPosition = Vector3Int.FloorToInt(transform.position);
        List<Vector3Int> nearbyMinePositions = new List<Vector3Int>();

        for (int x = -10; x <= 10; x++)
        {
            for (int y = -10; y <= 10; y++)
            {
                Vector3Int cellPosition = characterPosition + new Vector3Int(x, y, 0);
                if (IsWithinGridBounds(cellPosition) && gameRules.grid.TryGetCell(cellPosition.x, cellPosition.y, out Cell cell) && cell.type == Cell.Type.Mine && !cell.flagged)
                {
                    nearbyMinePositions.Add(cellPosition);
                }
            }
        }

        if (nearbyMinePositions.Count > 0)
        {
            Vector3Int selectedMinePosition = nearbyMinePositions[UnityEngine.Random.Range(0, nearbyMinePositions.Count)];
            Cell selectedMineCell = gameRules.grid.GetCell(selectedMinePosition.x, selectedMinePosition.y);
            selectedMineCell.flagged = true;
            board.Draw(gameRules.grid);
        }
    }

    private bool IsWithinGridBounds(Vector3Int position)
    {
        return position.x >= 0 && position.x < gameRules.width && position.y >= 0 && position.y < gameRules.height;
    }

    public void SpawnItemPrefab()
    {
        if (dropPrefab.Length == 0)
        {
            Debug.LogWarning("Drop prefab array is empty!");
            return;
        }

        Vector3Int[] revealedCellPositions = GetRevealedCellPositions();
        Vector3Int characterCellPosition = Vector3Int.FloorToInt(transform.position);
        List<Vector3Int> validSpawnPositions = new List<Vector3Int>();

        foreach (Vector3Int cellPosition in revealedCellPositions)
        {
            if (cellPosition != characterCellPosition && !usedSpawnPositions.Contains(cellPosition))
            {
                validSpawnPositions.Add(cellPosition);
            }
        }

        if (validSpawnPositions.Count > 0)
        {
            Vector3Int randomSpawnPosition = validSpawnPositions[UnityEngine.Random.Range(0, validSpawnPositions.Count)];
            Vector3 cellWorldPosition = board.tilemap.GetCellCenterWorld(randomSpawnPosition);
            Instantiate(dropPrefab[UnityEngine.Random.Range(0, dropPrefab.Length)], cellWorldPosition, Quaternion.identity);
            usedSpawnPositions.Add(randomSpawnPosition);
        }
        else
        {
            Debug.LogWarning("No valid cells available to spawn the prefab!");
        }
    }

    private Vector3Int[] GetRevealedCellPositions()
    {
        BoundsInt bounds = board.tilemap.cellBounds;
        List<Vector3Int> revealedCellPositions = new List<Vector3Int>();

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int cellPosition = new Vector3Int(x, y, 0);
                if (IsCellRevealedAndEmpty(cellPosition))
                {
                    revealedCellPositions.Add(cellPosition);
                }
            }
        }

        return revealedCellPositions.ToArray();
    }

    private bool IsCellRevealedAndEmpty(Vector3Int cellPosition)
    {
        Cell.Type cellType = board.GetCellType(cellPosition);
        return cellType == Cell.Type.Empty && board.IsCellRevealed(cellPosition);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("GalesDrop"))
        {
            numOfItems += 1;
            Destroy(other.gameObject);
            UpdateCharacterText();
            AudioManager.Instance.PlaySound(AudioManager.SoundType.GalePickUp, UnityEngine.Random.Range(.9f, 1.1f));
        }
    }

    public void ResetPrefabNum()
    {
        numOfItems = 0;
        UpdateCharacterText();

        GameObject[] spawnedPrefabs = GameObject.FindGameObjectsWithTag("GalesDrop");
        foreach (GameObject prefab in spawnedPrefabs)
        {
            Destroy(prefab);
        }

        usedSpawnPositions.Clear();
        movement.activePlayer = true;
    }

    private void UpdateSpellIcon()
    {
        if (numOfItems >= requiredNumOfItems)
        {
            spellIcon.color = readyColor;
            if (pulseCoroutine == null)
            {
                pulseCoroutine = StartCoroutine(PulseIcon());
            }
        }
        else
        {
            spellIcon.color = notReadyColor;
            if (pulseCoroutine != null)
            {
                StopCoroutine(pulseCoroutine);
                pulseCoroutine = null;
            }
        }
    }

    private IEnumerator PulseIcon()
    {
        while (true)
        {
            float elapsedTime = 0f;
            while (elapsedTime < pulseDuration)
            {
                spellIcon.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 1.2f, Mathf.PingPong(elapsedTime, pulseDuration / 2));
                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }
    }

    private void UpdateCharacterText()
    {
        charactersText.text = "Shards: " + numOfItems + "/3";
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
                IncreaseChanceOfSpawnShards();
                break;
            case 2:
                DecreaseTrapCount();
                break;
            case 3:
                DisableMagicBlock();
                break;
        }
    }
    private void IncreaseChanceOfSpawnShards()
    {
        dropSpawnChance = .8f;
        Debug.Log("Increased chance of spawning shards");
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
        upgradeButton1.GetComponentInChildren<TextMeshProUGUI>().text = "Increase chance of spawning shards from 70 to 80";
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
