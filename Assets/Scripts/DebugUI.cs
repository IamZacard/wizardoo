using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class DebugUI : MonoBehaviour
{
    private PlayerController Player;
    private Game gameRules;

    [SerializeField] private TextMeshProUGUI recordTimesText;
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private TextMeshProUGUI playerBools;

    private void Start()
    {
        // Try to get the Player object, but don't break if it's missing
        Player = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerController>();
        if (Player == null)
        {
            Debug.LogWarning("PlayerController component not found on the Player GameObject. Skipping player-related updates.");
        }

        // Assign the gameRules object
        FindGameRules();

        UpdateRecordTimes();
    }

    private void Update()
    {
        // Continuously update the variables each frame
        int deathCount = CharacterManager.deathCount;
        int coinCount = CharacterManager.coinCount;
        int stepsCount = CharacterManager.stepsCount;
        int restartCount = CharacterManager.restartCount;

        // Check again if gameRules is still valid or if we need to find it again
        if (gameRules == null)
        {
            FindGameRules();
        }

        // Ensure we're accessing the updated value of successfulReveals
        int successfulReveals = gameRules != null ? gameRules.successfulReveals : 0;

        bool firstPick = CharacterManager.increasedLight;
        bool secondPick = CharacterManager.RevealCellForStep;

        // Update UI with latest values
        statsText.text = $"Deaths: {deathCount}\nSteps: {stepsCount}\nRestarts: {restartCount}\nCoins: {coinCount}";
        playerBools.text = $"Sora1stPick: {firstPick}\nSora2ndPick: {secondPick}\nSuccessful Reveals: {successfulReveals}";

        // Only perform player-related updates if Player is not null
        if (Player != null)
        {
            // Add any Player-related updates here (if needed)
        }
    }

    // Helper method to find the GameRules object
    private void FindGameRules()
    {
        GameObject gameRulesObject = GameObject.FindGameObjectWithTag("GameRules");
        if (gameRulesObject != null)
        {
            gameRules = gameRulesObject.GetComponent<Game>();
        }
        else
        {
            Debug.LogWarning("No GameObject with tag 'GameRules' found!");
        }
    }

    private void UpdateRecordTimes()
    {
        // Check if the LevelTimer instance exists
        if (LevelTimer.Instance != null)
        {
            List<float> levelTimes = LevelTimer.Instance.GetLevelTimes();

            // Build a string to display all the level times
            string displayText = "Level Times:\n";
            for (int i = 0; i < levelTimes.Count; i++)
            {
                displayText += $"Level {i + 1}: {levelTimes[i]:F2} seconds\n"; // Format to 2 decimal places
            }

            // Display the string in the UI text element
            recordTimesText.text = displayText;
        }
        else
        {
            // Fallback if LevelTimer instance is missing
            recordTimesText.text = "No level times found.";
        }
    }
}
