using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Events;

public class Genie : MonoBehaviour
{
    public GameObject activateKey;
    public GameObject notePath;
    public GameObject dialogBox;
    public TextMeshProUGUI dialogText;
    public float dialogMoveDuration = 1.0f;

    private bool playerInRange = false;
    private bool isDialogOpen = false;
    private bool dialogTriggered = false; // Indicates if the dialogue has been triggered once
    private Vector3 dialogBoxStartPosition = new Vector3(0, -925, 0);
    private Vector3 dialogBoxEndPosition = new Vector3(0, -480, 0);
    private PlayerController Player;

    private void Start()
    {
        dialogBox.transform.localPosition = dialogBoxStartPosition;
        dialogBox.SetActive(false);

        if (activateKey != null)
        {
            activateKey.SetActive(false);
        }

        Player = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerController>();
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

            if (notePath != null)
            {
                notePath.SetActive(false);
            }
        }

        if (isDialogOpen)
        {
            Player.activePlayer = false;
        }
        else
        {
            Player.activePlayer = true;
        }

        if (Input.GetKeyDown(KeyCode.N) || Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            dialogBox.SetActive(false);
            //dialogTriggered = false;
            isDialogOpen = false;
            playerInRange = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInRange = true;

            // Show the activateKey only if the dialogue has not been triggered yet
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

            if (activateKey != null)
            {
                activateKey.SetActive(false);
            }
        }
    }

    private IEnumerator OpenDialog()
    {
        dialogText.text = "Hello, traveler! I won't do your three wishes, but you can choose one power-up.";
        dialogTriggered = true; // Set dialogTriggered to true once the dialogue is triggered

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

        // Trigger the UpgradeReady event
        UpgradeManager.Instance.OnUpgradeReady.Invoke();
    }
}
