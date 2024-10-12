using UnityEngine;
using UnityEngine.UI; // For using UI elements
using System.Collections;
using System.Collections.Generic; // Add this to use List<>
using TMPro;

public class DisplayLevelTimes : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI recordTimesText;
    [SerializeField] private TextMeshProUGUI statsText;

    private void Start()
    {
        // Check if the LevelTimer instance exists
        if (LevelTimer.Instance != null)
        {
            List<float> levelTimes = LevelTimer.Instance.GetLevelTimes();

            int deathCount = CharacterManager.deathCount;
            int coinCount = CharacterManager.coinCount;
            int stepsCount = CharacterManager.stepsCount;
            int restartCount = CharacterManager.restartCount;

            // Build a string to display all the level times
            string displayText = "Level Times:\n";
            for (int i = 0; i < levelTimes.Count; i++)
            {
                displayText += $"Level {i + 1}: {levelTimes[i]:F2} seconds\n"; // Format to 2 decimal places
            }

            // Display the string in the UI text element
            recordTimesText.text = displayText;
            statsText.text = $"Deaths: {deathCount}\nSteps: {stepsCount}\nRestarts: {restartCount}\nCoins: {coinCount}";
        }
        else
        {
            // Fallback if LevelTimer instance is missing
            recordTimesText.text = "No level times found.";
        }
    }
}
