using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Rendering.Universal; // Required for Light2D
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class LibrarianSoraya : MonoBehaviour
{
    [Header("Dialog")]
    public GameObject activateKey;
    public GameObject dialogBox;
    public TextMeshProUGUI dialogText;
    public float dialogMoveDuration = 1.0f;
    public float typingSpeed = 0.03f;
    public List<AudioClip> soundClips; // Added sound clips list

    private bool playerInRange = false;
    private bool isDialogOpen = false;
    private bool dialogTriggered = false;
    private bool isTyping = false; // New variable to check if typing is in progress
    private Vector3 dialogBoxStartPosition = new Vector3(0, -925, 0);
    private Vector3 dialogBoxEndPosition = new Vector3(0, -495, 0);

    private Game gameRules;
    private PlayerController Player;
    private Light2D playerLight;
    private Animator anim;
    private SpriteRenderer spriteRenderer;

    [Header("UI")]
    public GameObject warning;
    public float moveDurationLibUpgradePanel = 1.0f;

    [Header("Upgrades")]
    public GameObject libUpgradePanel;
    private Vector3 libUpgradePanelStartPosition = new Vector3(0, -925, 0);
    private Vector3 libUpgradePanelEndPosition = new Vector3(0, 0, 0);
    

    [Header("Pick1")]
    public Button upgradeButton1;
    public float increasedLightRadius = CharacterManager.increasedLightRadius;

    [Header("Pick2")]
    public Button upgradeButton2;

    [Header("Pick3")]
    public Button upgradeButton3;

    private const KeyCode dialogKey = KeyCode.E;
    private const KeyCode closeDialogKey = KeyCode.Space;

    private void Start()
    {
        dialogBox.transform.localPosition = dialogBoxStartPosition;
        
        dialogBox.SetActive(false);

        libUpgradePanel.transform.localPosition = libUpgradePanelStartPosition;
        
        libUpgradePanel.SetActive(false);
        warning.SetActive(false);

        SetButtonLabels();

        if (activateKey != null)
        {
            activateKey.SetActive(false);
        }

        Player = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerController>();
        if (Player == null)
        {
            Debug.LogError("PlayerController component not found on the Player GameObject.");
            return;
        }

        playerLight = Player.GetComponent<Light2D>();

        if (playerLight == null)
        {
            Debug.LogError("Light2D component not found on the Player GameObject.");
        }

        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (anim == null)
        {
            Debug.LogError("Animator component not found on LibrarianSoraya.");
        }

        if (spriteRenderer == null)
        {
            Debug.LogError("SpriteRenderer component not found on LibrarianSoraya.");
        }

        GameObject gameRulesObject = GameObject.FindGameObjectWithTag("GameRules");
        if (gameRulesObject != null)
        {
            gameRules = gameRulesObject.GetComponent<Game>();
        }
        else
        {
            Debug.LogWarning("No GameObject with tag 'GameRules' found!");
        }
    }

    private void Update()
    {
        if (playerInRange && Input.GetKeyDown(dialogKey) && !isDialogOpen && !dialogTriggered)
        {
            StartCoroutine(OpenDialog());

            if (activateKey != null)
            {
                activateKey.SetActive(false);
            }
        }

        if (Player != null)
        {
            Player.activePlayer = !isDialogOpen;
        }

        if (isDialogOpen && IsDialogCloseKeyPressed())
        {
            if (isTyping)
            {
                StopAllCoroutines();
                dialogText.text = GetCurrentFullSentence();
                isTyping = false;
            }
            else
            {
                CloseDialog();
            }
        }

        if (playerInRange)
        {
            FlipTowardsPlayer();
        }
    }

    private bool IsDialogCloseKeyPressed()
    {
        return Input.GetKeyDown(KeyCode.N) || Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(closeDialogKey);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInRange = true;
            TalkState();

            if (activateKey != null && !dialogTriggered)
            {
                activateKey.SetActive(true);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInRange = false;
            dialogTriggered = false; // Reset dialog trigger on exit
            DefaultState();

            if (activateKey != null)
            {
                activateKey.SetActive(false);
            }
        }
    }

    private IEnumerator OpenDialog()
    {
        dialogTriggered = true;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound(AudioManager.SoundType.DialogStart, 0.9f);
        }

        yield return new WaitForSeconds(1f);

        isDialogOpen = true;
        dialogBox.SetActive(true);

        yield return StartCoroutine(MoveDialogBox(dialogBoxStartPosition, dialogBoxEndPosition));

        string[] dialogTexts = new string[]
        {
        "Welcome, traveler, I am Soraya, the Mystic Librarian. I sense you seek knowledge and power. Come closer, there is much to learn here.",
        "I can grant you some knowledge that will help you finish your journey. For a coin, of course"
        };

        foreach (string text in dialogTexts)
        {
            // Start typing the sentence
            yield return StartCoroutine(TypeSentence(text));

            // Wait for user input to proceed to the next sentence
            while (!Input.GetMouseButtonDown(0))
            {
                yield return null;  // Wait for MB click
            }

            // Ensure there's a slight delay to avoid skipping to the next sentence too quickly
            yield return null;
        }

        // Wait for one last MB click to close the dialog
        while (!Input.GetMouseButtonDown(0))
        {
            yield return null;
        }

        CloseDialog();
    }

    private IEnumerator TypeSentence(string sentence)
    {
        dialogText.text = "";
        isTyping = true;  // Set typing flag to true
        int letterCount = 0;

        foreach (char letter in sentence)
        {
            dialogText.text += letter;
            letterCount++;

            // Play sound every 3 letters
            if (letterCount % 3 == 0 && soundClips != null && soundClips.Count > 0)
            {
                int randomIndex = Random.Range(0, soundClips.Count);
                float randomPitch = Random.Range(0.7f, 1.3f);

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySound(soundClips[randomIndex], randomPitch);
                }
            }

            // If the user clicks while typing, stop typing and display the full sentence
            if (Input.GetMouseButtonDown(0))
            {
                dialogText.text = sentence;  // Display full text
                break;  // Exit the loop to stop typing
            }

            yield return new WaitForSeconds(typingSpeed);
        }

        // Ensure all text is displayed and mark typing as finished
        dialogText.text = sentence;
        isTyping = false;  // Mark typing as complete

        // Wait for the next frame to allow the MB click to register properly
        yield return null;
    }

    private IEnumerator MoveDialogBox(Vector3 fromPosition, Vector3 toPosition)
    {
        float elapsedTime = 0f;
        while (elapsedTime < dialogMoveDuration)
        {
            dialogBox.transform.localPosition = Vector3.Lerp(fromPosition, toPosition, elapsedTime / dialogMoveDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        dialogBox.transform.localPosition = toPosition;
    }    

    private void CloseDialog()
    {
        StartCoroutine(MoveDialogBox(dialogBoxEndPosition, dialogBoxStartPosition));
        dialogText.text = "";
        dialogBox.SetActive(false);
        isDialogOpen = false;

        OpenUpgradePanel();
    }

    private void OpenUpgradePanel()
    {
        libUpgradePanel.SetActive(true);

        StartCoroutine(MoveUpgradeBox(libUpgradePanelStartPosition, libUpgradePanelEndPosition));

        upgradeButton1.onClick.AddListener(() => SelectUpgrade(1));
        upgradeButton2.onClick.AddListener(() => SelectUpgrade(2));
        upgradeButton3.onClick.AddListener(() => SelectUpgrade(3));
    }

    private IEnumerator MoveUpgradeBox(Vector3 fromPosition, Vector3 toPosition)
    {
        float elapsedTime = 0f;

        // Set the initial position
        libUpgradePanel.transform.localPosition = fromPosition;

        // Start the lerp animation
        while (elapsedTime < moveDurationLibUpgradePanel)
        {
            libUpgradePanel.transform.localPosition = Vector3.Lerp(fromPosition, toPosition, elapsedTime / moveDurationLibUpgradePanel);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure the panel reaches the final position
        libUpgradePanel.transform.localPosition = toPosition;
    }


    private void SelectUpgrade(int index)
    {
        if (CharacterManager.coinCount > 0)
        {
            // Proceed with the selected upgrade option if there are enough coins
            Debug.Log("Upgrade option " + index + " selected.");
            //AudioManager.Instance.PlaySound(AudioManager.SoundType.ShuffProc, 1f);

            ApplyUpgrade(index); // Apply the upgrade

            libUpgradePanel.SetActive(false); // Close the panel after applying upgrade
        }
        else
        {
            // If not enough coins, show warning and close panel
            StartCoroutine(WarningNotEnough());
            libUpgradePanel.SetActive(false); // Close the panel
        }

        // Remove listeners after selection or warning
        upgradeButton1.onClick.RemoveAllListeners();
        upgradeButton2.onClick.RemoveAllListeners();
        upgradeButton3.onClick.RemoveAllListeners();
    }

    private void ApplyUpgrade(int index)
    {
        switch (index)
        {
            case 1:
                IncreaseLightRadius();
                break;
            case 2:
                RevealCellForSteps();
                break;
            case 3:
                P3();
                break;
        }
    }

    private void SetButtonLabels()
    {
        upgradeButton1.GetComponentInChildren<TextMeshProUGUI>().text = "Increase light radius around you";
        upgradeButton2.GetComponentInChildren<TextMeshProUGUI>().text = "After walking 5 steps, reveal random adjacent cell";
        upgradeButton3.GetComponentInChildren<TextMeshProUGUI>().text = "3";
    }

    private void IncreaseLightRadius()
    {
        if (CharacterManager.coinCount > 0)
        {
            Player.DecreaseCoinCount(1); // Decrease 1 coin

            if (playerLight != null)
            {
                // Increase the light radius and save it
                CharacterManager.increasedLight = true;
                playerLight.pointLightOuterRadius += increasedLightRadius;
            }
        }
        else
        {
            StartCoroutine(WarningNotEnough());
        }
    }


    private void RevealCellForSteps()
    {
        if (CharacterManager.coinCount > 1)
        {
            Player.DecreaseCoinCount(2);

            CharacterManager.RevealCellForStep = true;
        }
        else
        {
            StartCoroutine(WarningNotEnough());
        }
    }

    private void P3()
    {
        if (CharacterManager.coinCount > 0)
        {
            Player.DecreaseCoinCount(1);
        }
        else
        {
            StartCoroutine(WarningNotEnough());
        }
    }

    private IEnumerator WarningNotEnough()
    {
        warning.SetActive(true); // Show warning
        AudioManager.Instance.PlaySound(AudioManager.SoundType.ErrorSound, Random.Range(.7f, 1f));

        yield return new WaitForSeconds(2f); // Wait for N seconds

        warning.SetActive(false); // Hide warning
    }



    private void TalkState()
    {
        if (anim != null)
        {
            anim.SetBool("Talking", true);
        }
    }

    private void DefaultState()
    {
        if (anim != null)
        {
            anim.SetBool("Talking", false);
        }
    }

    private void FlipTowardsPlayer()
    {
        if (Player != null && spriteRenderer != null)
        {
            Vector3 playerPosition = Player.transform.position;
            Vector3 sorayaPosition = transform.position;

            spriteRenderer.flipX = playerPosition.x > sorayaPosition.x;
        }
    }

    private string GetCurrentFullSentence()
    {
        // You can extend this method to store and return the current full sentence during typing if needed.
        // For now, it just returns the current text in the dialog box.
        return dialogText.text;
    }
}
