using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [SerializeField] public Image characterIcon;
    [SerializeField] public TextMeshProUGUI characterName;
    [SerializeField] public TextMeshProUGUI dialogueArea;
    [SerializeField] public List<AudioClip> soundClips;

    private Queue<DialogueLine> lines;

    private bool isDisplaying = false; // Added flag to track whether dialog is being displayed

    public bool isDialogueActive = false;

    public float typingSpeed = 0.03f;

    [Header("DialogBox")]
    [SerializeField] private RectTransform boxImage;
    private Vector3 initialBoxPosition;
    private Vector3 initialBoxScale;
    private Vector3 targetBoxPos;

    // Callback to be triggered when dialogue ends
    public System.Action OnDialogueEnd;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;

        lines = new Queue<DialogueLine>();

        boxImage.localPosition = new Vector3(0, -385, 0);
        targetBoxPos = new Vector3(0, 50, 0);

        initialBoxPosition = boxImage.localPosition;
        initialBoxScale = boxImage.localScale;
    }

    private void Update()
    {
        // Check for SPACE key press only when not currently displaying dialog
        if (isDialogueActive && !isDisplaying && Input.GetKeyDown(KeyCode.Space))
        {
            DisplayNextDialogueLine();
        }
    }

    public void StartDialogue(Dialogue dialogue)
    {
        isDialogueActive = true;

        // Ensure the GameObject is active before animating
        gameObject.SetActive(true);

        // Start the show animation using AnimateBox
        StartCoroutine(AnimateBox(targetBoxPos, initialBoxScale, 0.5f));

        lines.Clear();

        foreach (DialogueLine dialogueLine in dialogue.dialogueLines)
        {
            lines.Enqueue(dialogueLine);
        }

        DisplayNextDialogueLine();
    }

    public void DisplayNextDialogueLine()
    {
        if (lines.Count == 0)
        {
            EndDialogue();
            return;
        }

        DialogueLine currentLine = lines.Dequeue();

        characterIcon.sprite = currentLine.character.icon;
        characterName.text = currentLine.character.name;

        StopAllCoroutines();
        StartCoroutine(TypeSentence(currentLine));
    }

    IEnumerator TypeSentence(DialogueLine dialogueLine)
    {
        isDisplaying = true; // Set the flag when dialog is being displayed
        dialogueArea.text = "";
        int characterCount = 0;

        foreach (char letter in dialogueLine.line.ToCharArray())
        {
            dialogueArea.text += letter;
            characterCount++;

            if (characterCount % 3 == 0 && soundClips != null && soundClips.Count > 0)
            {
                int randomIndex = Random.Range(0, soundClips.Count);
                float randomPitch = Random.Range(0.8f, 1.2f);

                // Assuming you have a generic SoundType for individual clips
                //AudioManager.SoundType soundType = AudioManager.SoundType.talkSound; // Replace with the appropriate SoundType
                //AudioManager.Instance.PlaySound(soundType, randomPitch);
            }

            yield return new WaitForSeconds(typingSpeed);
        }

        isDisplaying = false; // Reset the flag when dialog is finished
    }

    void EndDialogue()
    {
        isDialogueActive = false;

        // Start the hide animation using AnimateBox
        StartCoroutine(AnimateBox(initialBoxPosition, new Vector3(0, 0, 0), 0.5f));

        // Deactivate the GameObject after animation completes
        StartCoroutine(DeactivateAfterAnimation(0.5f));

        // Perform actions like changing collider size or destroying it
        OnDialogueEnd?.Invoke();
    }

    private IEnumerator AnimateBox(Vector3 targetPosition, Vector3 targetScale, float duration)
    {
        Vector3 startPosition = boxImage.localPosition;
        Vector3 startScale = boxImage.localScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            boxImage.localPosition = Vector3.Lerp(startPosition, targetPosition, elapsed / duration);
            boxImage.localScale = Vector3.Lerp(startScale, targetScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        boxImage.localPosition = targetPosition;
        boxImage.localScale = targetScale;
    }

    private IEnumerator DeactivateAfterAnimation(float delay)
    {
        yield return new WaitForSeconds(delay);
        gameObject.SetActive(false);
    }
}
