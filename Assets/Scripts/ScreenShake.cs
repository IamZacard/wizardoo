using System.Collections;
using UnityEngine;
using Cinemachine;

public class ScreenShake : MonoBehaviour
{
    // Singleton instance
    public static ScreenShake Instance { get; private set; }

    // Default duration and magnitude of the shake
    public float defaultShakeDuration = 0.5f;
    public float defaultShakeMagnitude = 0.5f;

    private CinemachineVirtualCamera cinemachineVirtualCamera;
    private CinemachineBasicMultiChannelPerlin cinemachinePerlin;
    private float initialAmplitude;
    private float initialFrequency;

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
        SetVirtualCameraReference();
    }

    private void SetVirtualCameraReference()
    {
        // Automatically find the CinemachineVirtualCamera in the scene
        cinemachineVirtualCamera = FindObjectOfType<CinemachineVirtualCamera>();

        if (cinemachineVirtualCamera != null)
        {
            cinemachinePerlin = cinemachineVirtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();

            if (cinemachinePerlin != null)
            {
                initialAmplitude = cinemachinePerlin.m_AmplitudeGain;
                initialFrequency = cinemachinePerlin.m_FrequencyGain;
                Debug.Log("CinemachineBasicMultiChannelPerlin found and set up correctly.");
            }
            else
            {
                Debug.LogWarning("CinemachineBasicMultiChannelPerlin is not set up correctly on the CinemachineVirtualCamera.");
            }
        }
        else
        {
            Debug.LogWarning("No CinemachineVirtualCamera found in the scene.");
        }
    }

    public void TriggerShake(float duration, float magnitude)
    {
        if (cinemachineVirtualCamera == null || cinemachinePerlin == null)
        {
            SetVirtualCameraReference();
        }

        if (cinemachinePerlin != null)
        {
            StartCoroutine(Shake(duration, magnitude));
        }
        else
        {
            Debug.LogWarning("Cinemachine Perlin Noise is not set up correctly.");
        }
    }

    private IEnumerator Shake(float duration, float magnitude)
    {
        float elapsed = 0f;

        // Set initial shake values
        cinemachinePerlin.m_AmplitudeGain = magnitude;
        cinemachinePerlin.m_FrequencyGain = magnitude;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Reset to initial values after shaking
        cinemachinePerlin.m_AmplitudeGain = initialAmplitude;
        cinemachinePerlin.m_FrequencyGain = initialFrequency;
    }
}
