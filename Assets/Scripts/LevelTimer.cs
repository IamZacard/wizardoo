using System.Collections.Generic; // For List<>
using UnityEngine; // For MonoBehaviour

public class LevelTimer : MonoBehaviour
{
    public static LevelTimer Instance;

    private float startTime;
    private float endTime;
    private List<float> levelTimes = new List<float>();
    private bool isTimerRunning;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void StartLevelTimer()
    {
        startTime = Time.time;
        isTimerRunning = true;
    }

    public void StopLevelTimer()
    {
        endTime = Time.time;
        float timeTaken = endTime - startTime;
        levelTimes.Add(timeTaken);
        Debug.Log($"Level completed in {timeTaken} seconds.");
        isTimerRunning = false; // Stop the timer
    }

    public float GetCurrentLevelTime()
    {
        if (isTimerRunning)
        {
            return Time.time - startTime;
        }
        else
        {
            return endTime - startTime;
        }
    }

    public List<float> GetLevelTimes()
    {
        return levelTimes;
    }
}

