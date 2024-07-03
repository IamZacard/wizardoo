using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DialogueCharacter
{
    public string name;
    public Sprite icon;    
}

[System.Serializable]
public class DialogueLine
{
    public DialogueCharacter character;
    [TextArea(3, 10)]
    public string line;
}

[System.Serializable]
public class Dialogue
{
    public List<DialogueLine> dialogueLines = new List<DialogueLine>();
}

public class DialogueTrigger : MonoBehaviour
{
    public Dialogue dialogue;

    private BoxCollider2D BC2D;
    private CapsuleCollider2D CC2D;

    private bool hasDialoguePlayed = false;

    private void Start()
    {
        BC2D = GetComponent<BoxCollider2D>();
        CC2D = GetComponent<CapsuleCollider2D>();
        DialogueManager.Instance.OnDialogueEnd += OnDialogueEnd;
    }
    public void TriggerDialogue()
    {
        if (!hasDialoguePlayed)
        {
            DialogueManager.Instance.StartDialogue(dialogue);
            hasDialoguePlayed = true; // Set the flag to true after playing the dialogue
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            TriggerDialogue();
        }
    }

    private void OnDialogueEnd()
    {
        // Perform actions like changing collider size or destroying it
        // For example, you can access the collider component on the same GameObject
        // and change its size or destroy it
        if (BC2D != null)
        {
            // Change the collider size or destroy it
            //BC2D.size = new Vector2(0f, 0f);
            //AudioManager.Instance.PlaySound(AudioManager.SoundType.Success, 0.2f);
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from the event to avoid memory leaks
        DialogueManager.Instance.OnDialogueEnd -= OnDialogueEnd;
    }
}