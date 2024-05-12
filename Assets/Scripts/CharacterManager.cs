using UnityEngine;

public static class CharacterManager
{
    public static GameObject SelectedCharacterPrefab { get; set; }
    public static int selectedCharacterIndex { get; set; }

    public static int deathCount = 0;
}
