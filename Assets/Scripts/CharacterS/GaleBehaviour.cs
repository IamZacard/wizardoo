using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GaleBehaviour : MonoBehaviour
{
    public int galeIndex;

    [Header("DropItems")]
    public int numOfItems;
    public int requiredNumOfItems = 3;
    public float dropSpawnChance = .7f;
    public GameObject[] dropPrefab;

    [SerializeField] private Transform effectPoint;
    public GameObject blastEffect;

    [SerializeField] private Image spellIcon; // Drag your spell icon Image here in the Inspector
    public Color readyColor = Color.white; // Bright color when ready
    public Color notReadyColor = Color.grey; // Grey color when not ready
    public float pulseDuration = 2f; // Duration of one pulse cycle

    private Coroutine pulseCoroutine;
    private TextMeshProUGUI charactersText;
    private Game gameRules; // Reference to the Game script for accessing the tilemap
    private Board board; // Reference to the Board script for accessing the tilemap
    private List<Vector3Int> usedSpawnPositions = new List<Vector3Int>(); // List to store used spawn positions

    private void Awake()
    {
        // Initialize references
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
    }

    private void Start()
    {
        galeIndex = CharacterManager.selectedCharacterIndex;
        UpdateCharacterText();
        UpdateSpellIcon();
    }

    private void Update()
    {
        // Reset item number on specific key press
        if (Input.GetKeyDown(KeyCode.N) || Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            ResetPrefabNum();
        }

        // Trigger blast effect when conditions are met
        if (numOfItems >= requiredNumOfItems && Input.GetMouseButtonDown(0) && !gameRules.levelComplete && !EventSystem.current.IsPointerOverGameObject())
        {
            FlagRandomTrap();
            Instantiate(blastEffect, effectPoint.transform.position, Quaternion.identity);
            ScreenShake.Instance.TriggerShake(1f, 1f);
            AudioManager.Instance.PlaySound(AudioManager.SoundType.GaleBlast, 1f);
            Debug.Log("blastEffect!");
            gameRules.CheckWinConditionFlags();
        }

        // Update spell icon color
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

        // Find nearby mine positions within a certain radius
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

        // Flag a random nearby mine position if available
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

        // Find valid spawn positions excluding character's current cell and used positions
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
            usedSpawnPositions.Add(randomSpawnPosition); // Add the new spawn position to the list
        }
        else
        {
            Debug.LogWarning("No valid cells available to spawn the prefab!");
        }
    }

    private bool IsCellOccupied(Vector3 worldPosition)
    {
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
            charactersText.text = "Shards: " + numOfItems + "/3";
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

        usedSpawnPositions.Clear(); // Clear the used positions list
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
}
