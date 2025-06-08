// SpellTriggerBase.cs
using UnityEngine;
using System;

[Serializable]
public abstract class SpellTriggerBase
{
    [Tooltip("A brief description of what this trigger monitors.")]
    public string description = "Base Spell Trigger";

    public abstract bool Evaluate(CharacterBase caster, Cell triggeringCell); // Standardized to Evaluate
}