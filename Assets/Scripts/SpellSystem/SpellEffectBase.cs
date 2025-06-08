// SpellEffectBase.cs
using UnityEngine;
using System;

[Serializable]
public abstract class SpellEffectBase
{
    [Tooltip("Optional: A short description for this specific effect instance.")]
    public string effectDescription = "Base effect";

    public abstract void Execute(CharacterBase caster, Cell targetCell);
}