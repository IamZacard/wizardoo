using UnityEngine;

public class OrbMovement : MonoBehaviour
{
    public float amplitude = 0.1f; // The distance to move up and down
    public float speed = 1f; // The speed of the playerController

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private bool movingUp = true;

    void Start()
    {
        startPosition = transform.position;
        targetPosition = startPosition + new Vector3(0, amplitude, 0);
    }

    void Update()
    {
        if (movingUp)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, Random.Range(1f,1.5f) * Time.deltaTime);
            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                movingUp = false;
                targetPosition = startPosition - new Vector3(0, amplitude, 0);
            }
        }
        else
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, Random.Range(1f, 1.5f) * Time.deltaTime);
            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                movingUp = true;
                targetPosition = startPosition + new Vector3(0, amplitude, 0);
            }
        }
    }
}
