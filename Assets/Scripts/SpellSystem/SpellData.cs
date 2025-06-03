using UnityEngine;
using System.Collections.Generic;
using System;

[Serializable]
public enum SpellType
{
    Passive,
    Active,
    Triggered
}

[Serializable]
public enum TargetType
{
    Self,
    CurrentPosition,
    MousePosition,
    ClosestTrap,
    RandomCell,
    AreaAroundSelf
}

[Serializable]
public enum ResourceType
{
    None,
    MagicShards,
    Mana,
    Charges
}

[CreateAssetMenu(fileName = "New Spell", menuName = "Arcane Delvers/Spell")]
public class SpellData : ScriptableObject
{
    [Header("Basic Info")]
    public string spellName;
    [TextArea(2, 4)]
    public string description;
    public Sprite icon;
    public SpellType spellType;

    [Header("Cost & Resources")]
    public ResourceType resourceType = ResourceType.None;
    public int resourceCost = 0;
    public int maxCharges = 1;
    public bool consumesCharges = false;

    [Header("Targeting")]
    public TargetType targetType;
    public int range = 1;
    public int areaSize = 1;

    [Header("Effects (What the spell does)")]
    // Use [SerializeReference] to allow polymorphic serialization in the Inspector
    [SerializeReference]
    public List<SpellEffectBase> effects = new List<SpellEffectBase>();

    [Header("Trigger Conditions (When passive/triggered spells activate)")]
    [SerializeReference]
    public List<SpellTriggerBase> triggers = new List<SpellTriggerBase>();

    // Duration for continuous effects (e.g., invulnerability)
    [Header("Duration (for active spells)")]
    [Tooltip("How long this spell's active effects last, if applicable.")]
    public float activeDuration = 0f;

    [Header("Visual & Audio")]
    public GameObject visualEffect;
    public AudioClip soundEffect;
    public Color effectColor = Color.white;
}