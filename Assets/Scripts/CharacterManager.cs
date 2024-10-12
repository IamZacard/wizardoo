using UnityEngine;

public static class CharacterManager
{
    public static GameObject SelectedCharacterPrefab { get; set; }
    public static int selectedCharacterIndex { get; set; }

    [Header("Player's stats")]
    public static int deathCount = 0;
    public static int coinCount = 0;
    public static int stepsCount = 0;
    public static int restartCount = 0;

    public static bool showNumberOfTraps = false;
    //public static bool showNumberOfTraps = true;

    [Header("SorayaPick1")]
    public static bool increasedLight = false;
    public static float increasedLightRadius = 2;

    [Header("SorayaPick2")]
    //public static bool RevealCellForStep = true;
    public static bool RevealCellForStep = false;

    [Header("SorayaPick3")]
    //public static bool RevealCellForStep = true;
    public static bool pick3 = false;
}
