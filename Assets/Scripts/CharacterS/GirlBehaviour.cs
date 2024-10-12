using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GirlBehaviour : MonoBehaviour
{
    private Game gameRules; // Reference to the Game script for teleportation constraints
    private Board board; // Reference to the Board script for accessing the tilemap
    private PlayerController movement;

    [Header("Teleport")]
    public bool hasTeleported = false;
    public bool safeTp = false;    
    public bool safeTpUpgradeSelected = false;

    [SerializeField] private GameObject tpEffect;
    private TextMeshProUGUI charactersText;

    [Header("UI")]
    // References for the upgrade panel and buttons
    public GameObject upgradePanel;
    public Button upgradeButton1;
    public Button upgradeButton2;
    public Button upgradeButton3;

    private Vector3 originalScale;

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
                break;
            }
        }

        movement = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerController>();
        if (movement == null)
        {
            Debug.LogWarning("No PlayerController component found on the Player GameObject!");
        }

        // Log a warning if the TextMeshProUGUI component is not found
        if (charactersText == null)
        {
            Debug.LogWarning("TextMeshProUGUI component with the specified tag not found!");
        }
        else
        {
            charactersText.text = "Click on the cell to teleport there";
        }

        upgradePanel.SetActive(false);
        SetButtonLabels();

        // Set the original scale for buttons
        originalScale = upgradeButton1.transform.localScale;
    }

    private void Start()
    {
        // Listen to the UpgradeReady event from the UpgradeManager
        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.OnUpgradeReady.AddListener(OpenUpgradePanel);
        }

        if (safeTpUpgradeSelected)
        {
            safeTp = true;
        }
        else
        {
            safeTp = false;
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject() && movement != null && movement.activePlayer)
        {
            // Get the world position of the mouse click
            Vector3 worldPosition = Camera.main != null ? Camera.main.ScreenToWorldPoint(Input.mousePosition) : Vector3.zero;
            Vector3Int cellPosition = board.tilemap.WorldToCell(worldPosition);

            // Check if teleportation is allowed using the new helper method
            if (CanTeleport(cellPosition))
            {
                // Teleport the girl to the clicked cell
                transform.position = board.tilemap.GetCellCenterWorld(cellPosition);

                hasTeleported = true;

                Instantiate(tpEffect, transform.position, Quaternion.identity);
                ScreenShake.Instance.TriggerShake(.1f, 5f);
                AudioManager.Instance.PlaySound(AudioManager.SoundType.VioletTeleport, Random.Range(.7f, 1f));

                gameRules.CheckWinCondition();
            }
            else
            {
                // Play an error sound and log the appropriate message
                Debug.LogWarning("Teleportation to a 'Floor' cell or outside of the board is not allowed!");
                AudioManager.Instance.PlaySound(AudioManager.SoundType.ErrorSound, Random.Range(.7f, 1f));
            }
        }

        if ((Input.GetKeyDown(KeyCode.N) || Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) && !gameRules.levelComplete)
        {
            hasTeleported = false;  // Reset the teleport status

            if (safeTpUpgradeSelected)
            {
                safeTp = true;  // Reset safeTp to true if the upgrade was selected
            }
        }
    }

    private bool CanTeleport(Vector3Int cellPosition)
    {
        return !gameRules.gameover
               && movement != null
               && movement.activePlayer
               && IsWithinBoardBounds(cellPosition)
               && board.tilemap.HasTile(cellPosition)
               && IsTeleportationAllowed(cellPosition);
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
        Debug.Log($"Cell Position: {cellPosition}, Cell Type: {cellType}");
        // Check if the cell type is not "Floor"
        return cellType != Cell.Type.Floor;
    }
    public void ResetTeleportStatus()
    {
        hasTeleported = false;
    }

    public void RestartLevel()
    {
        hasTeleported = false;  // Reset the teleport status

        if (safeTpUpgradeSelected)
        {
            safeTp = true;  // Reset safeTp to true if the upgrade was selected
        }
    }

    private void SelectUpgrade(int index)
    {
        Debug.Log("Upgrade option " + index + " selected.");
        upgradePanel.SetActive(false);
        ResetUpgradeButtons();

        switch (index)
        {
            case 1:
                SafeTeleport();
                break;
            case 2:
                DecreaseTrapCount();
                break;
            case 3:
                DisableMagicBlock();
                break;
        }
    }

    private void ResetUpgradeButtons()
    {
        upgradeButton1.onClick.RemoveAllListeners();
        upgradeButton2.onClick.RemoveAllListeners();
        upgradeButton3.onClick.RemoveAllListeners();
    }

    private void SafeTeleport()
    {
        safeTp = true;
        safeTpUpgradeSelected = true;
        Debug.Log("First time teleport on a trap will disable it");
    }

    private void DecreaseTrapCount()
    {
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
        upgradeButton1.GetComponentInChildren<TextMeshProUGUI>().text = "First teleport on a trap will disable it";
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
