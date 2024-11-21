using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Light2DFlickerController : MonoBehaviour
{
    private Light2D light2D;
    private float originalIntensity;
    private float targetIntensity;

    // Random flicker parameters
    private float flickerIntensityRange;
    private float flickerIntensitySpeed;

    public float minIntensity = .1f;
    public float maxIntensity = .8f;

    void Start()
    {
        light2D = GetComponent<Light2D>();
        if (light2D == null)
        {
            Debug.LogError("Light2D component not found on the GameObject.");
            return;
        }

        // Initialize original and target intensity
        originalIntensity = light2D.intensity;
        targetIntensity = originalIntensity;

        // Randomize flicker parameters
        flickerIntensityRange = Random.Range(0.5f, 1.0f); // Random flicker intensity range
        flickerIntensitySpeed = Random.Range(0.8f, 1.0f); // Random flicker intensity speed

        StartCoroutine(FlickerCoroutine());
    }

    private IEnumerator FlickerCoroutine()
    {
        while (true)
        {
            // Calculate new intensity based on flicker effect
            float newIntensity = originalIntensity + Mathf.Sin(Time.time * flickerIntensitySpeed) * flickerIntensityRange; // Example of a sinusoidal flicker
            newIntensity = Mathf.Clamp(newIntensity, minIntensity, maxIntensity); // Clamp the intensity to the range [minIntensity, maxIntensity]

            targetIntensity = Mathf.Lerp(targetIntensity, newIntensity, Time.deltaTime * 5f); // Smoothly transition to the new intensity

            // Apply the flicker effect to the Light2D component
            light2D.intensity = targetIntensity;

            yield return null;
        }
    }
}
