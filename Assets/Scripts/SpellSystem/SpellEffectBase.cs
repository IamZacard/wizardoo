// SpellEffectBase.cs
using UnityEngine;
using System;

/// <summary>
/// Base class for all spell effects.
/// Each concrete spell effect (e.g., RevealArea, Invulnerability) will inherit from this.
/// This allows SpellData to hold a list of polymorphic effects.
/// </summary>
[Serializable] // Make it serializable so it can be embedded in ScriptableObjects
public abstract class SpellEffectBase
{
    [Tooltip("Optional: A short description for this specific effect instance.")]
    public string effectDescription = "Base effect";

    /// <summary>
    /// Executes the spell effect.
    /// </summary>
    /// <param name="caster">The CharacterBase casting the spell.</param>
    /// <param name="targetCell">The primary target cell of the spell, if applicable (can be null).</param>
    public abstract void Execute(CharacterBase caster, Cell targetCell);
}