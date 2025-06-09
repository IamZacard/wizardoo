using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal; // Corrected namespace

public class FlickerController : MonoBehaviour
{
    private Light2D light2D;
    private float originalIntensity;

    // Perlin noise flicker parameters
    private float flickerOffset; // Used to sample different parts of the Perlin noise curve

    [Header("Flicker Settings")]
    [Tooltip("Minimum intensity the light can reach during flicker.")]
    [SerializeField] private float minIntensity = 0.1f;
    [Tooltip("Maximum intensity the light can reach during flicker.")]
    [SerializeField] private float maxIntensity = 0.8f;
    [Tooltip("How fast the flicker oscillates. Higher values mean faster changes.")]
    [SerializeField] private float flickerSpeed = 5.0f;
    [Tooltip("How much the flicker deviates from the original intensity. Higher values mean more dramatic flickering.")]
    [SerializeField] private float flickerAmount = 0.5f;
    [Tooltip("Speed at which the light intensity smoothly transitions to the new target.")]
    [SerializeField] private float intensitySmoothSpeed = 10f;

    void Awake() // Changed from Start to Awake for more consistent initialization
    {
        light2D = GetComponent<Light2D>();
        if (light2D == null)
        {
            Debug.LogError("Light2D component not found on the GameObject.", this);
            enabled = false; // Disable script if Light2D is missing
            return;
        }

        originalIntensity = light2D.intensity;

        // Initialize flicker offset with a random value for unique flicker patterns per light
        flickerOffset = Random.Range(0f, 1000f);

        StartCoroutine(FlickerCoroutine());
    }

    private IEnumerator FlickerCoroutine()
    {
        while (true)
        {
            // Sample Perlin noise based on time and a unique offset
            float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, flickerOffset);

            // Map noise (0-1) to desired intensity range relative to original intensity
            // A simple approach: noise * flickerAmount to get deviation, then add to original
            float targetFlickerIntensity = originalIntensity + (noise - 0.5f) * flickerAmount; // (noise - 0.5f) centers the deviation around 0

            // Clamp the intensity to the defined min/max range
            targetFlickerIntensity = Mathf.Clamp(targetFlickerIntensity, minIntensity, maxIntensity);

            // Smoothly transition the light's intensity
            light2D.intensity = Mathf.Lerp(light2D.intensity, targetFlickerIntensity, Time.deltaTime * intensitySmoothSpeed);

            yield return null;
        }
    }

    /// <summary>
    /// Resets the light to its original intensity and stops flickering.
    /// </summary>
    public void StopFlicker()
    {
        StopAllCoroutines();
        light2D.intensity = originalIntensity;
        enabled = false; // Optionally disable the script
    }

    /// <summary>
    /// Starts the flicker effect.
    /// </summary>
    public void StartFlicker()
    {
        if (enabled) return; // Already flickering
        enabled = true;
        StartCoroutine(FlickerCoroutine());
    }
}