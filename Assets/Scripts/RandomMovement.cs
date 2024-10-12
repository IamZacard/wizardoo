using UnityEngine;
using UnityEngine.SceneManagement; // Needed for scene management

public class RandomMovement : MonoBehaviour
{
    public float moveSpeed = 5f; // Speed of the object
    public float moveSpeedIncrement = .01f; // Increment speed of the object
    public float objectTargetScale = .2f;
    public float scaleChangeSpeed = .5f; // Speed of scaling
    private Vector2 moveDirection; // Current move direction
    private Rigidbody2D rb; // Reference to Rigidbody2D
    private Collider2D col; // Reference to Collider2D
    private Vector3 initialScale; // Store the initial scale
    private Vector3 initialPos; // Store the initial scale
    private bool scalingUp = false; // Determine direction of scaling

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        initialScale = transform.localScale; // Save the initial scale of the object
        initialPos = transform.position; // Save the initial scale of the object
        SetRandomDirection(); // Set an initial random direction
    }

    void FixedUpdate()
    {
        // Check if the object collides with the cursor
        Vector2 cursorPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if (col.OverlapPoint(cursorPosition))
        {
            transform.position = initialPos;
            SetRandomDirection();
        }

        rb.velocity = moveDirection * moveSpeed;
        AnimateScale(); // Call scale animation function
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Wall"))
        {
            Vector2 normal = collision.contacts[0].normal; // Get the normal of the collision
            moveDirection = Vector2.Reflect(moveDirection, normal); // Reflect the direction

            // Increment the speed when colliding with a wall
            moveSpeed += moveSpeedIncrement;
        }
    }

    void SetRandomDirection()
    {
        float randomAngle = Random.Range(0f, 360f);
        moveDirection = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle)).normalized;
    }

    void RestartScene()
    {
        // Reload the current active scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void AnimateScale()
    {
        // Animate scale between 1 and 0.2
        if (scalingUp)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, initialScale, scaleChangeSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.localScale, initialScale) < 0.1f)
            {
                scalingUp = false; // Switch direction
            }
        }
        else
        {
            Vector3 targetScale = initialScale * objectTargetScale; // Target scale of 0.2
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, scaleChangeSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.localScale, targetScale) < 0.1f)
            {
                scalingUp = true; // Switch direction
            }
        }
    }
}
