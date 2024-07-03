using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GoblinBehaviour : MonoBehaviour
{
    public int goblinIndex;
    public float goblinsLuck = 0.5f;
    private TextMeshProUGUI charactersText;

    // References for the upgrade panel and buttons
    public GameObject upgradePanel;
    public Button upgradeButton1;
    public Button upgradeButton2;
    public Button upgradeButton3;

    private Game gameRules;

    private Vector3 originalScale;

    private void Awake()
    {
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

    private void Start()
    {
        goblinIndex = CharacterManager.selectedCharacterIndex;

        upgradePanel.SetActive(false);

        TextMeshProUGUI[] textComponents = FindObjectsOfType<TextMeshProUGUI>();
        foreach (TextMeshProUGUI textComponent in textComponents)
        {
            if (textComponent.CompareTag("AbilityText"))
            {
                charactersText = textComponent;
                break;
            }
        }

        if (charactersText == null)
        {
            Debug.LogWarning("TextMeshProUGUI component with the specified tag not found!");
        }

        charactersText.text = (goblinsLuck * 100) + "% chance to flag trap";

        // Listen to the UpgradeReady event from the UpgradeManager
        UpgradeManager.Instance.OnUpgradeReady.AddListener(OpenUpgradePanel);

        // Set button texts
        SetButtonLabels();

        // Initialize originalScale
        if (upgradeButton1 != null)
        {
            originalScale = upgradeButton1.GetComponent<RectTransform>().localScale;
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
        upgradeButton1.GetComponentInChildren<TextMeshProUGUI>().text = "Gambler's Grace luck increased to 55%";
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

    private void SelectUpgrade(int index)
    {
        Debug.Log("Upgrade option " + index + " selected.");
        AudioManager.Instance.PlaySound(AudioManager.SoundType.ShuffProc, 1f);
        upgradePanel.SetActive(false);

        // Remove all listeners to avoid duplicate calls
        upgradeButton1.onClick.RemoveAllListeners();
        upgradeButton2.onClick.RemoveAllListeners();
        upgradeButton3.onClick.RemoveAllListeners();

        // Apply the selected upgrade logic
        switch (index)
        {
            case 1:
                IncreaseLuck();
                break;
            case 2:
                DecreaseTrapCount();
                break;
            case 3:
                DisableMagicBlock();
                break;
        }
    }

    private void IncreaseLuck()
    {
        goblinsLuck = 0.55f;
        Debug.Log("Gambler's Grace luck increased to 55%");
        charactersText.text = (goblinsLuck * 100) + "% chance to flag trap";
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
