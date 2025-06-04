using System.Collections.Generic;
using UnityEngine;
using System; // Required for [Serializable]

[Serializable]
public class CharacterSpellEntry
{
    public SpellData spellData;
    //public KeyCode activationKey = KeyCode.Mouse0;
}

[CreateAssetMenu(fileName = "New Character", menuName = "Arcane Delvers/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("Basic Info")]
    public string characterName = "Unnamed Character";
    [TextArea(3, 5)]
    public string description = "";
    public GameObject prefab;

    [Header("UI")]
    public Sprite icon;
    public GameObject flagEffect;

    [Header("Stats")]
    [Range(1, 10)]
    public int lightRadius = 1;
    public int startingGold = 0;

    [Header("Interaction")]
    public float interactionRange = 1.5f;
    public KeyCode interactionKey = KeyCode.E;
    public float interactionDuration = 0f; // 0 for instant interactions

    [Header("Visual & Audio")]
    public AudioClip soundEffect;
    public Color effectColor = Color.white;

    [Header("Abilities")]
    public List<CharacterSpellEntry> characterSpells = new List<CharacterSpellEntry>();
}
