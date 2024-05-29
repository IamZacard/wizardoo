using UnityEngine;
using TMPro;

public class MystBehaviour : MonoBehaviour
{
    // Myst boolean for tracking moves
    public bool invincible;
    public int stepsOfInvi = 5;
    private int moveCount;
    private bool abilityUsed; // Track whether the ability has been used in the current life
    private Game gameRules;

    //[Header("UI")]
    private TextMeshProUGUI charactersText;

    private Animator anim;

    // Reference to the invincibility effect prefab
    public GameObject inviEffectPrefab;
    private GameObject inviEffectInstance;

    private void Start()
    {
        // Initialize variables
        invincible = false;
        moveCount = 0;
        abilityUsed = false;

        anim = GetComponent<Animator>();
        GameObject gameRulesObject = GameObject.FindGameObjectWithTag("GameRules");

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

        if (gameRulesObject != null)
        {
            gameRules = gameRulesObject.GetComponent<Game>();
        }
        else
        {
            Debug.LogWarning("No GameObject with tag 'GameRules' found!");
        }

        // Log a warning if the TextMeshProUGUI component is not found
        if (charactersText == null)
        {
            Debug.LogWarning("TextMeshProUGUI component with the specified tag not found!");
        }

        charactersText.text = "Activate ability to become INVINCIBLE";
    }

    private void Update()
    {
        // Check if invincible and movement keys are pressed
        if (invincible && IsMovementKeyPressed())
        {
            moveCount++;
            charactersText.text = "Steps left: " + (stepsOfInvi - moveCount);

            // If stepsOfInvi moves are made, disable invincibility
            if (moveCount >= stepsOfInvi)
            {
                invincible = false;
                moveCount = 0;

                anim.SetBool("Invincible", false);
                charactersText.text = "Care! You are not invincible anymore!";

                if (inviEffectInstance != null)
                {
                    Destroy(inviEffectInstance);
                }
            }
        }

        // Check if the left mouse button is pressed and the ability has not been used in the current life
        if (Input.GetMouseButtonDown(0) && !invincible && !abilityUsed && !gameRules.gameover)
        {
            // Start counting moves if not already invincible
            invincible = true;
            moveCount = 0;
            charactersText.text = "Steps left: " + (stepsOfInvi - moveCount);
            abilityUsed = true; // Set abilityUsed to true

            anim.SetBool("Invincible", true);
            AudioManager.Instance.PlaySound(AudioManager.SoundType.MysticInvincible, 1f);
            charactersText.text = "You are INVINCIBLE NOW!";

            // Instantiate invincibility effect and make it a child of the character
            if (inviEffectPrefab != null)
            {
                inviEffectInstance = Instantiate(inviEffectPrefab, transform);
                inviEffectInstance.transform.localPosition = Vector3.zero;
            }
        }

        // Check if any of the cancel keys are pressed
        if (Input.GetKeyDown(KeyCode.N) || Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            // Reset move count and stop counting moves
            moveCount = 0;
            invincible = false;
            ResetAbility();
        }
    }

    // Check if any movement key is pressed
    private bool IsMovementKeyPressed()
    {
        return Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow) ||
               Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow) ||
               Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow) ||
               Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow);
    }

    // Function to reset the player's life (can be called when respawning)
    public void ResetAbility()
    {
        abilityUsed = false; // Reset abilityUsed
        anim.SetBool("Invincible", false);
        charactersText.text = "Activate ability to become INVINCIBLE";

        // Destroy invincibility effect if it exists
        if (inviEffectInstance != null)
        {
            Destroy(inviEffectInstance);
        }
    }
}
