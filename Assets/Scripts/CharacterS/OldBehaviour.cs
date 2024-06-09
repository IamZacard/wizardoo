using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class OldBehaviour : MonoBehaviour
{
    [SerializeField] private GameObject revealEffect;
    public float revealRadius = 1.5f; // Radius within which cells will be revealed
    public int maxCastCount = 3; // Maximum number of times the ability can be cast
    public int currentCastCount = 0; // Current number of times the ability has been cast

    [SerializeField] private Image spellIcon; // Drag your spell icon Image here in the Inspector
    public Color readyColor = Color.white; // Bright color when ready
    public Color notReadyColor = Color.grey; // Grey color when not ready
    public float pulseDuration = 2f; // Duration of one pulse cycle

    private Coroutine pulseCoroutine;
    private Game gameRules;
    private Board board; // Reference to the Board script for accessing the tilemap

    //[Header("UI")]
    private TextMeshProUGUI charactersText;

    private void Awake()
    {
        GameObject gameRulesObject = GameObject.FindGameObjectWithTag("GameRules");
        GameObject boardObject = GameObject.FindGameObjectWithTag("Board");

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

    private void Start()
    {
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

        charactersText.text = "Activate ability to reveal cells around";
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && (currentCastCount < maxCastCount) && (gameRules.canFlag) && !EventSystem.current.IsPointerOverGameObject())
        {
            RevealCellsAroundCharacter();
            Instantiate(revealEffect, transform.position, Quaternion.identity);
            AudioManager.Instance.PlaySound(AudioManager.SoundType.SageReveal, Random.Range(.9f, 1.1f));
        }

        if (Input.GetKeyDown(KeyCode.N) || Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            currentCastCount = 0;
            charactersText.text = "Activate ability to reveal cells around";
            spellIcon.color = readyColor;
            if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
            pulseCoroutine = StartCoroutine(PulseIcon());
        }

        //Spell Icon color
        UpdateSpellIcon();
    }

    private void RevealCellsAroundCharacter()
    {
        currentCastCount++;
        int flagsRemoved = 0; // Count the number of flags removed

        // Get the position of the character
        Vector3 characterPosition = transform.position;

        // Loop through nearby cells within the reveal radius
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                // Calculate the position of the cell relative to the character
                Vector3Int cellPosition = new Vector3Int(Mathf.FloorToInt(characterPosition.x) + x, Mathf.FloorToInt(characterPosition.y) + y, 0);

                // Check if the cell is within the grid boundaries
                if (IsWithinGridBounds(cellPosition))
                {
                    // Try to get the cell at the calculated position
                    if (gameRules.grid.TryGetCell(cellPosition.x, cellPosition.y, out Cell cell))
                    {
                        // Check if it's a mine cell
                        if (cell.type == Cell.Type.Mine && !cell.revealed)
                        {
                            // Automatically flag the mine if it's not revealed
                            cell.flagged = true;
                            flagsRemoved++; // Increase flag count
                            board.Draw(gameRules.grid);
                        }
                        else
                        {
                            // Reveal the cell
                            gameRules.Reveal(cell);
                            board.Draw(gameRules.grid);
                        }
                    }
                }
            }
        }

        gameRules.flagCount -= flagsRemoved; // Deduct the number of flags removed from the flag count

        gameRules.CheckWinConditionFlags();
        gameRules.CheckWinCondition();
        charactersText.text = "Reveal used";
    }

    void UpdateSpellIcon()
    {
        if (currentCastCount < maxCastCount)
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

    private bool IsWithinGridBounds(Vector3Int cellPosition)
    {
        // Check if the cell position is within the grid boundaries
        return cellPosition.x >= 0 && cellPosition.x < gameRules.grid.Width &&
               cellPosition.y >= 0 && cellPosition.y < gameRules.grid.Height;
    }
}
