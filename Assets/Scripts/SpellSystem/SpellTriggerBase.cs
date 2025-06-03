// SpellTriggerBase.cs
using UnityEngine;
using System; // Ensure this is present for [System.Serializable]

[System.Serializable]
public abstract class SpellTriggerBase
{
    [Tooltip("A brief description of what this trigger monitors.")]
    public string description = "Base Spell Trigger"; // Using 'description' for consistency

    /// <summary>
    /// Evaluates if the trigger condition is met. This method is usually called by the CharacterSpellManager
    /// when a relevant game event occurs.
    /// </summary>
    /// <param name="caster">The character whose spell might be triggered.</param>
    /// <param name="triggeringCell">The cell relevant to the event that might cause the trigger.</param>
    /// <returns>True if the spell should be triggered, false otherwise.</returns>
    public abstract bool Evaluate(CharacterBase caster, Cell triggeringCell); // Standardized to Evaluate
}