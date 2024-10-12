using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class MystBehaviour : MonoBehaviour
{
    public bool invincible;
    public int stepsOfInvi = 7; // Default steps
    private int moveCount;
    public int charges; // Number of charges
    private Game gameRules;
    private PlayerController playerController;

    [SerializeField] private Image spellIcon;
    public Color readyColor = Color.white;
    public Color notReadyColor = Color.grey;
    public float pulseDuration = 2f;

    private Coroutine pulseCoroutine;
    private TextMeshProUGUI charactersText;
    private Animator anim;

    public GameObject inviEffectPrefab;
    private GameObject inviEffectInstance;

    // References for the upgrade panel and buttons
    public GameObject upgradePanel;
    public Button upgradeButton1;
    public Button upgradeButton2;
    public Button upgradeButton3;

    private Vector3 originalScale;

    private void Start()
    {
        invincible = false;
        moveCount = 0;

        // For debugging purposes, clear the saved charges
        // Uncomment the next line to clear PlayerPrefs during debugging
        PlayerPrefs.DeleteKey("MystCharges");

        // Load the number of charges from PlayerPrefs, default to 1 if not set
        charges = PlayerPrefs.GetInt("MystCharges", 1);

        upgradePanel.SetActive(false);

        anim = GetComponent<Animator>();
        gameRules = GameObject.FindGameObjectWithTag("GameRules")?.GetComponent<Game>();

        charactersText = FindTextComponentWithTag("AbilityText");

        if (charactersText != null)
        {
            charactersText.text = "Activate ability to become INVINCIBLE";
        }

        if (gameRules == null)
        {
            Debug.LogWarning("No GameObject with tag 'GameRules' found!");
        }

        if (charactersText == null)
        {
            Debug.LogWarning("TextMeshProUGUI component with the specified tag not found!");
        }

        playerController = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerController>();
        if (charactersText == null)
        {
            Debug.LogWarning("No GameObject with tag 'Player' found!");
        }

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
        if (invincible && IsMovementKeyPressed())
        {
            moveCount++;
            charactersText.text = "Steps left: " + (stepsOfInvi - moveCount);

            if (moveCount >= stepsOfInvi)
            {
                DisableInvincibility();
            }

            UpdateSpellIcon();
        }

        // Ensure the shrine is not active before activating the ability
        if (Input.GetMouseButtonDown(0) && playerController.activePlayer && !EventSystem.current.IsPointerOverGameObject() && !invincible && charges > 0 && !gameRules.gameover && !gameRules.levelComplete)
        {
            ActivateInvincibility();
        }

        if ((Input.GetKeyDown(KeyCode.N) || Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) && !gameRules.levelComplete)
        {
            ResetAbility();
        }
    }

    private void ActivateInvincibility()
    {
        invincible = true;
        moveCount = 0;
        charges--;

        anim.SetBool("Invincible", true);
        AudioManager.Instance.PlaySound(AudioManager.SoundType.MysticInvincible, 1f);
        charactersText.text = "You are INVINCIBLE NOW!";

        if (inviEffectPrefab != null)
        {
            inviEffectInstance = Instantiate(inviEffectPrefab, transform);
            inviEffectInstance.transform.localPosition = Vector3.zero;
        }

        UpdateSpellIcon();
    }

    private void DisableInvincibility()
    {
        invincible = false;
        moveCount = 0;

        anim.SetBool("Invincible", false);

        if (inviEffectInstance != null)
        {
            Destroy(inviEffectInstance);
        }

        if (charges <= 0)
        {
            charactersText.text = "Care! You are not invincible anymore!";
        }
    }

    private TextMeshProUGUI FindTextComponentWithTag(string tag)
    {
        TextMeshProUGUI[] textComponents = FindObjectsOfType<TextMeshProUGUI>();
        foreach (TextMeshProUGUI textComponent in textComponents)
        {
            if (textComponent.CompareTag(tag))
            {
                return textComponent;
            }
        }
        return null;
    }

    private bool IsMovementKeyPressed()
    {
        return Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow) ||
               Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow) ||
               Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow) ||
               Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow);
    }

    public void ResetAbility()
    {
        invincible = false;
        moveCount = 0;
        charges = PlayerPrefs.GetInt("MystCharges", 1); // Reset charges to saved value

        anim.SetBool("Invincible", false);
        charactersText.text = "Activate ability to become INVINCIBLE";

        if (inviEffectInstance != null)
        {
            Destroy(inviEffectInstance);
        }

        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }

        UpdateSpellIcon();
    }

    private void UpdateSpellIcon()
    {
        spellIcon.color = (charges > 0) ? readyColor : notReadyColor;

        if (charges > 0 && pulseCoroutine == null)
        {
            pulseCoroutine = StartCoroutine(PulseIcon());
        }
        else if (charges <= 0 && pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
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

    private void TwoCharges()
    {
        charges = 2;
        PlayerPrefs.SetInt("MystCharges", charges); // Save the new default value
        PlayerPrefs.Save();
        Debug.Log("Mystic now has 2 charges of invincibility");
    }

    private void MoreSteps()
    {
        stepsOfInvi = 12;
        Debug.Log("Mystic now has 12 steps of invincibility");
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
        upgradeButton1.GetComponentInChildren<TextMeshProUGUI>().text = "You can become invincible twice!";
        upgradeButton2.GetComponentInChildren<TextMeshProUGUI>().text = "You have 12 steps of invincibility";
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
                MoreSteps();
                break;
            case 3:
                DisableMagicBlock();
                break;
        }

        // Ensure the pulse coroutine is managed correctly
        UpdateSpellIcon();
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
