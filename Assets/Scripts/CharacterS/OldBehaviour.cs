using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class OldBehaviour : MonoBehaviour
{
    [SerializeField] private GameObject revealEffect;
    public float defaultRevealRadius = 1.5f; // Default radius within which cells will be revealed
    public float increasedRevealRadius = 2.5f; // Increased radius within which cells will be revealed
    public int currentCastCount = 0; // Current number of times the ability has been cast

    [SerializeField] private Image spellIcon; // Drag your spell icon Image here in the Inspector
    public Color readyColor = Color.white; // Bright color when ready
    public Color notReadyColor = Color.grey; // Grey color when not ready
    private float pulseDuration = 2f; // Duration of one pulse cycle

    private Coroutine pulseCoroutine;
    private Game gameRules;
    private PlayerController movement;
    private Board board; // Reference to the Board script for accessing the tilemap

    private TextMeshProUGUI charactersText;

    public int charges; // Number of charges
    private float currentRevealRadius; // Current reveal radius

    // References for the upgrade panel and buttons
    public GameObject upgradePanel;
    public Button upgradeButton1;
    public Button upgradeButton2;
    public Button upgradeButton3;

    private Vector3 originalScale;
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

        movement = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerController>();
        if (charactersText == null)
        {
            Debug.LogWarning("No GameObject with tag 'Player' found!");
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

        // For debugging purposes, clear the saved charges
        // Uncomment the next line to clear PlayerPrefs during debugging
        PlayerPrefs.DeleteKey("revealCharges");
        // Load the number of charges from PlayerPrefs if available, otherwise use default
        charges = PlayerPrefs.GetInt("revealCharges", 1);

        // Set the default reveal radius
        currentRevealRadius = defaultRevealRadius;

        upgradePanel.SetActive(false);

        UpdateSpellIcon();
        SetButtonLabels();

        // Listen to the UpgradeReady event from the UpgradeManager
        UpgradeManager.Instance.OnUpgradeReady.AddListener(OpenUpgradePanel);

        // Initialize originalScale
        if (upgradeButton1 != null)
        {
            originalScale = upgradeButton1.GetComponent<RectTransform>().localScale;
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && (currentCastCount < charges) && (gameRules.canFlag) && !EventSystem.current.IsPointerOverGameObject() && !gameRules.gameover && movement.activePlayer)
        {
            RevealCellsAroundCharacter();
            Instantiate(revealEffect, transform.position, Quaternion.identity);
            ScreenShake.Instance.TriggerShake(.2f, 1f);
            AudioManager.Instance.PlaySound(AudioManager.SoundType.SageReveal, Random.Range(.9f, 1.1f));
        }

        if (Input.GetKeyDown(KeyCode.N) || Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            currentCastCount = 0;
            charges = PlayerPrefs.GetInt("revealCharges", 1);
            charactersText.text = "Activate ability to reveal cells around";
            spellIcon.color = readyColor;
            if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
            pulseCoroutine = StartCoroutine(PulseIcon());
        }

        // Spell Icon color
        UpdateSpellIcon();
    }

    private void RevealCellsAroundCharacter()
    {
        currentCastCount++;
        int newFlagsAdded = 0; // Count the number of new flags added

        // Get the position of the character
        Vector3 characterPosition = transform.position;

        int radius = (int)currentRevealRadius; // Use the current reveal radius

        // Loop through nearby cells within the reveal radius
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                // Calculate the position of the cell relative to the character
                Vector3Int cellPosition = new Vector3Int(Mathf.FloorToInt(characterPosition.x) + x, Mathf.FloorToInt(characterPosition.y) + y, 0);

                // Check if the cell is within the grid boundaries
                if (IsWithinGridBounds(cellPosition))
                {
                    // Try to get the cell at the calculated position
                    if (gameRules.grid.TryGetCell(cellPosition.x, cellPosition.y, out Cell cell))
                    {
                        if (cell.type == Cell.Type.Mine && !cell.revealed && !cell.flagged)
                        {
                            // Automatically flag the mine if it's not revealed and not already flagged
                            cell.flagged = true;
                            newFlagsAdded++; // Increase new flag count
                            board.Draw(gameRules.grid);
                        }
                        else if (!cell.revealed)
                        {
                            // Reveal the cell if it's not a mine and not already revealed
                            gameRules.Reveal(cell);
                            board.Draw(gameRules.grid);
                        }
                    }
                }
            }
        }

        gameRules.flagCount -= newFlagsAdded; // Deduct the number of new flags added from the flag count

        gameRules.CheckWinConditionFlags();
        gameRules.CheckWinCondition();
        charactersText.text = "Reveal used";
    }

    void UpdateSpellIcon()
    {
        if (currentCastCount < charges)
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

    private void TwoCharges()
    {
        PlayerPrefs.SetInt("revealCharges", 2); // Save the new default value
        PlayerPrefs.Save();
        Debug.Log("Older now has 2 charges of reveal");
    }

    private void MoreCells()
    {
        currentRevealRadius = increasedRevealRadius; // Change the reveal radius to the increased value
        Debug.Log("Ability now reveals cells in a radius of 2");
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
        // Unsubscribe from the UpgradeReady event when the object is destroyed
        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.OnUpgradeReady.RemoveListener(OpenUpgradePanel);
        }
    }

    private void SetButtonLabels()
    {
        upgradeButton1.GetComponentInChildren<TextMeshProUGUI>().text = "You can use your spell 2 times";
        upgradeButton2.GetComponentInChildren<TextMeshProUGUI>().text = "Reveal cells in a radius of 2 cells around";
        upgradeButton3.GetComponentInChildren<TextMeshProUGUI>().text = "Disable Magic Block";
    }

    private void OpenUpgradePanel()
    {
        upgradePanel.SetActive(true);
        upgradeButton1.onClick.AddListener(() => SelectUpgrade(1));
        upgradeButton2.onClick.AddListener(() => SelectUpgrade(2));
        upgradeButton3.onClick.AddListener(() => SelectUpgrade(3));
    }

    private void SelectUpgrade(int index)
    {
        Debug.Log("Upgrade option " + index + " selected.");
        //AudioManager.Instance.PlaySound(AudioManager.SoundType.ShuffProc, 1f);
        upgradePanel.SetActive(false);

        // Remove all listeners to avoid duplicate calls
        upgradeButton1.onClick.RemoveAllListeners();
        upgradeButton2.onClick.RemoveAllListeners();
        upgradeButton3.onClick.RemoveAllListeners();

        // Apply the selected upgrade logic
        switch (index)
        {
            case 1:
                TwoCharges();
                break;
            case 2:
                MoreCells();
                break;
            case 3:
                DisableMagicBlock();
                break;
        }
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
            float scaleDuration = 0.2f; // Duration of the scaling

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
