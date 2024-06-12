using System.Collections;
using UnityEngine;

public class ScreenShake : MonoBehaviour
{
    // Singleton instance
    public static ScreenShake Instance { get; private set; }

    // Default duration and magnitude of the shake
    public float defaultShakeDuration = 0.5f;
    public float defaultShakeMagnitude = 0.5f;

    private Transform cameraTransform;
    private Vector3 initialPosition;

    private void Awake()
    {
        // Implement singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Make it persistent between scenes
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        SetCameraReference();
    }

    private void SetCameraReference()
    {
        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
            initialPosition = cameraTransform.position;
        }
        else
        {
            Debug.LogWarning("Main Camera not found. Make sure a camera has the tag 'MainCamera'.");
        }
    }

    public void TriggerShake(float duration, float initialMagnitude)
    {
        if (cameraTransform == null)
        {
            SetCameraReference();
        }

        if (cameraTransform != null)
        {
            StartCoroutine(Shake(duration, initialMagnitude));
        }
        else
        {
            Debug.LogWarning("Camera transform is still null. Screen shake will not occur.");
        }
    }

    private IEnumerator Shake(float duration, float initialMagnitude)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (cameraTransform == null)
            {
                yield break;
            }

            float magnitude = Mathf.Lerp(initialMagnitude, 0f, elapsed / duration);
            Vector3 randomPoint = initialPosition + (Vector3)Random.insideUnitCircle * magnitude;
            cameraTransform.position = new Vector3(randomPoint.x, randomPoint.y, initialPosition.z);

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (cameraTransform != null)
        {
            cameraTransform.position = initialPosition;
        }
    }
}
