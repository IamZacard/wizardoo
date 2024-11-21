using TMPro;
using UnityEngine;

public class FPSDisplay : MonoBehaviour
{
    public float updateInterval = 0.5f; // Interval at which FPS is updated
    private float accum = 0.0f; // FPS accumulated over the interval
    private int frames = 0; // Frames drawn over the interval
    private float timeleft; // Left time for current interval

    [SerializeField] private TextMeshProUGUI fpsText;
    private void Start()
    {
        // Initialize timeleft with updateInterval
        timeleft = updateInterval;
    }

    private void Update()
    {
        timeleft -= Time.deltaTime;
        accum += Time.timeScale / Time.deltaTime;
        frames++;

        // Calculate FPS every updateInterval seconds
        if (timeleft <= 0.0f)
        {
            // Display the average FPS over the interval
            float fps = accum / frames;
            fpsText.text = "FPS: " + Mathf.RoundToInt(fps);

            // Reset variables for the next interval
            timeleft = updateInterval;
            accum = 0.0f;
            frames = 0;
        }
    }
}
