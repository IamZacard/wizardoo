using UnityEngine;

public class Rat : MonoBehaviour
{
    private Vector3 targetPosition; // Target position for the rat to move towards
    public float movingSpeed = 5f; // Speed of the rat
    public float oscillationFrequency = 2f; // Frequency of the oscillation
    public float oscillationAmplitude = 1f; // Amplitude of the oscillation
    private SpriteRenderer spriteRenderer;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            // Set initial alpha to 0 (invisible)
            SetAlpha(0);
        }
    }

    // Set the target position for the rat to move towards
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
        transform.position = Vector3.MoveTowards(transform.position, newPosition, movingSpeed * Time.deltaTime);

        // If the rat reaches the target position within a tolerance, destroy itself
        if (Vector3.Distance(transform.position, new Vector3(targetPosition.x, transform.position.y, transform.position.z)) < 0.1f &&
            Mathf.Abs(targetPosition.y - transform.position.y) < 0.1f)
        {
            Destroy(gameObject);
        }

        // Flip the sprite if the rat moves left
        if (targetPosition.x < transform.position.x)
        {
            spriteRenderer.flipX = true;
        }
        else
        {
            spriteRenderer.flipX = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Board"))
        {
            SetAlpha(1); // Set alpha to 1 (visible)
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Board"))
        {
            SetAlpha(0); // Set alpha to 0 (invisible)
        }
    }

    private void SetAlpha(float alpha)
    {
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;
        }
    }
}
