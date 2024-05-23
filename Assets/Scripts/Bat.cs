using UnityEngine;

public class Bat : MonoBehaviour
{
    private Vector3 targetPosition; // Target position for the bat to fly towards
    public float flyingSpeed = 5f; // Speed of the bat
    public float oscillationFrequency = 2f; // Frequency of the oscillation
    public float oscillationAmplitude = 1f; // Amplitude of the oscillation
    private SpriteRenderer spriteRenderer;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Set the target position for the bat to fly towards
    public void SetTarget(Vector3 target)
    {
        targetPosition = target;
    }

    private void Update()
    {
        // Calculate the vertical oscillation using sine function
        float oscillation = oscillationAmplitude * Mathf.Sin(oscillationFrequency * Time.time);

        // Calculate the new position with oscillation applied
        Vector3 newPosition = new Vector3(targetPosition.x, targetPosition.y + oscillation, transform.position.z);

        // Move towards the target position with oscillation
        transform.position = Vector3.MoveTowards(transform.position, newPosition, flyingSpeed * Time.deltaTime);

        // If the bat reaches the target position, destroy itself
        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            Destroy(gameObject);
        }

        // Flip the sprite if the bat moves left
        if (targetPosition.x < transform.position.x)
        {
            spriteRenderer.flipX = true;
        }
        else
        {
            spriteRenderer.flipX = false;
        }
    }
}
