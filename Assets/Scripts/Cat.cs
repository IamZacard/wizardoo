using UnityEngine;

public class Cat : MonoBehaviour
{
    private Animator anim;
    private SpriteRenderer spriteRenderer;
    private Transform player;      // Reference to the player's transform
    public float playerDetectionRange = 3f; // Range within which the player can be detected
    public float catLickRange = 1.7f; // Range within which the player can be detected
    public float lickCooldown = 1f; // Cooldown time for the lick animation and sound

    private bool isPlayerNearby = false;
    private bool catlick = false;
    private float lastLickTime; // Keeps track of the last time the cat licked

    void Start()
    {
        // Find the player's transform using the PlayerController
        player = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerController>().transform;
        if (player == null)
        {
            Debug.LogError("PlayerController component not found on the Player GameObject.");
        }

        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (anim == null)
        {
            Debug.LogError("Animator component not found on the Cat GameObject.");
        }

        if (spriteRenderer == null)
        {
            Debug.LogError("SpriteRenderer component not found on the Cat GameObject.");
        }

        lastLickTime = -lickCooldown; // Initialize so that the cat can lick immediately
    }

    void Update()
    {
        if (player == null) return;  // Exit if player reference is not found

        CheckPlayerDistance();
        HandlePlayerInput();

        if (isPlayerNearby)
        {
            FlipTowardsPlayer();
        }
    }

    void CheckPlayerDistance()
    {
        // Calculate the distance between the cat and the player
        float distanceToPlayer = Vector3.Distance(player.position, transform.position);

        // If the player is within detection range, switch to "Idle"
        if (distanceToPlayer <= playerDetectionRange)
        {
            if (!isPlayerNearby)
            {
                isPlayerNearby = true;
                anim.SetBool("Idle", true);
            }
        }
        else
        {
            if (isPlayerNearby)
            {
                isPlayerNearby = false;
                anim.SetBool("Idle", false);
            }
        }

        if (distanceToPlayer <= catLickRange)
        {
            catlick = true;
        }
        else
        {
            catlick = false;
        }
    }

    void HandlePlayerInput()
    {
        // If the player clicks on the cat while nearby and cooldown has passed, switch to "Licks" animation
        if (catlick && Input.GetMouseButtonDown(0) && Time.time >= lastLickTime + lickCooldown)  // Left mouse button
        {
            anim.SetTrigger("Licks"); // Use a trigger to transition to "Licks" state

            // Play cat meow sound if AudioManager exists
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(AudioManager.SoundType.CatMeowSound, 0.9f);
            }

            // Update the last lick time to the current time
            lastLickTime = Time.time;
        }
    }

    private void FlipTowardsPlayer()
    {
        if (player != null && spriteRenderer != null)
        {
            // Check the direction to flip the sprite
            if (player.position.x < transform.position.x)
            {
                spriteRenderer.flipX = true;  // Flip the sprite to face left
            }
            else
            {
                spriteRenderer.flipX = false; // Face right
            }
        }
    }
}
