using UnityEngine;

public class RotateOnY : MonoBehaviour
{
    public float rotationRange = 10f; // Range of rotation from -10 to 10 degrees
    public float duration = 2f; // Duration for one complete rotation cycle
    private float timeElapsed = 0f; // Time elapsed since start

    void Update()
    {
        // Update the elapsed time
        timeElapsed += Time.deltaTime;

        // Calculate the fraction of the duration that has passed
        float phase = Mathf.PingPong(timeElapsed, duration) / duration;

        // Calculate the current rotation angle based on the phase
        float currentRotationY = Mathf.Lerp(-rotationRange, rotationRange, phase);

        // Apply the rotation to the object
        transform.rotation = Quaternion.Euler(0, currentRotationY, 0);
    }
}
