using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LevelUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI levelTimeText;

    /*private void Update()
    {
        if (LevelTimer.Instance != null && levelTimeText != null)
        {
            // Get the current level time and format it as minutes and seconds
            float currentLevelTime = LevelTimer.Instance.GetCurrentLevelTime();
            string formattedTime = FormatTime(currentLevelTime);

            // Get the counts from the CharacterManager
            int deathCount = CharacterManager.deathCount;
            int coinCount = CharacterManager.coinCount;
            int stepsCount = CharacterManager.stepsCount;
            int restartCount = CharacterManager.restartCount;

            // Display all the data in the UI
            levelTimeText.text = $"Time: {formattedTime}\nDeaths: {deathCount}\nCoins: {coinCount}\nSteps: {stepsCount}\nRestarts: {restartCount}";
        }
    }*/

    private void Update()
    {
        if (LevelTimer.Instance != null && levelTimeText != null)
        {
            // Get the current level time and format it as minutes and seconds
            float currentLevelTime = LevelTimer.Instance.GetCurrentLevelTime();
            levelTimeText.text = FormatTime(currentLevelTime);
        }
    }

    // Method to format time as MM:SS
    private string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}

