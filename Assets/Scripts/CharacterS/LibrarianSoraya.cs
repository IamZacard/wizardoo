using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LibrarianSoraya : MonoBehaviour
{
    public GameObject activateKey;
    public GameObject dialogBox;
    public TextMeshProUGUI dialogText;
    public float dialogMoveDuration = 1.0f;
    public float typingSpeed = 0.03f;
    public List<AudioClip> soundClips; // Added sound clips list

    private bool playerInRange = false;
    private bool isDialogOpen = false;
    private bool dialogTriggered = false;
    private Vector3 dialogBoxStartPosition = new Vector3(0, -925, 0);
    private Vector3 dialogBoxEndPosition = new Vector3(0, -480, 0);
    private PlayerController Player;
    private Animator anim;
    private SpriteRenderer spriteRenderer;

    private void Start()
    {
        dialogBox.transform.localPosition = dialogBoxStartPosition;
        dialogBox.SetActive(false);

        if (activateKey != null)
        {
            activateKey.SetActive(false);
        }

        Player = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerController>();

        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E) && !isDialogOpen && !dialogTriggered)
        {
            StartCoroutine(OpenDialog());

            if (activateKey != null)
            {
                activateKey.SetActive(false);
            }
        }

        if (Player != null)
        {
            Player.activePlayer = isDialogOpen ? false : true;
        }

        if (isDialogOpen && (Input.GetKeyDown(KeyCode.N) || Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)))
        {
            dialogBox.SetActive(false);
            isDialogOpen = false;
            playerInRange = false;
        }

        if (playerInRange)
        {
            FlipTowardsPlayer();
        }
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
        AudioManager.Instance.PlaySound(AudioManager.SoundType.Success, .9f);

        isDialogOpen = true;
        dialogBox.SetActive(true);

        float elapsedTime = 0f;
        while (elapsedTime < dialogMoveDuration)
        {
            dialogBox.transform.localPosition = Vector3.Lerp(dialogBoxStartPosition, dialogBoxEndPosition, elapsedTime / dialogMoveDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        dialogBox.transform.localPosition = dialogBoxEndPosition;

        string[] dialogTexts = new string[]
        {
            "Welcome, traveler, I am Soraya, the Mystic Librarian. I sense you seek knowledge and power. Come closer, there is much to learn here.",
            "Since you're here, I can grant you some knowledge that will help you finish your trial."
        };

        foreach (string text in dialogTexts)
        {
            yield return StartCoroutine(TypeSentence(text));
            yield return new WaitUntil(() => Input.GetMouseButtonDown(0));
        }

        yield return new WaitUntil(() => Input.GetMouseButtonDown(0));

        isDialogOpen = false;
        elapsedTime = 0f;
        while (elapsedTime < dialogMoveDuration)
        {
            dialogBox.transform.localPosition = Vector3.Lerp(dialogBoxEndPosition, dialogBoxStartPosition, elapsedTime / dialogMoveDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        dialogBox.transform.localPosition = dialogBoxStartPosition;
        dialogBox.SetActive(false);

        UpgradeManager.Instance.SorayaTrigger.Invoke();
    }

    private IEnumerator TypeSentence(string sentence)
    {
        dialogText.text = "";
        int characterCount = 0;

        foreach (char letter in sentence.ToCharArray())
        {
            dialogText.text += letter;
            characterCount++;

            if (characterCount % 3 == 0 && soundClips != null && soundClips.Count > 0)
            {
                int randomIndex = Random.Range(0, soundClips.Count);
                float randomPitch = Random.Range(0.7f, 1.3f);

                AudioManager.Instance.PlaySound(soundClips[randomIndex], randomPitch);
            }

            yield return new WaitForSeconds(typingSpeed);
        }
    }

    private void TalkState()
    {
        anim.SetBool("Talking", true);
    }

    private void DefaultState()
    {
        anim.SetBool("Talking", false);
    }

    private void FlipTowardsPlayer()
    {
        if (Player != null)
        {
            Vector3 playerPosition = Player.transform.position;
            Vector3 sorayaPosition = transform.position;

            if (playerPosition.x > sorayaPosition.x)
            {
                spriteRenderer.flipX = true; // Face right
            }
            else if (playerPosition.x < sorayaPosition.x)
            {
                spriteRenderer.flipX = false; // Face left
            }
        }
    }
}
