using System;
using UnityEngine;

[Serializable]
public class MakeInvulnerableEffect : SpellEffectBase
{
    [Tooltip("Number of steps/turns the character remains invulnerable.")]
    public int invulnerableSteps = 1;

    public MakeInvulnerableEffect() { effectDescription = "Makes the character invulnerable for a number of steps."; }

    public override void Execute(CharacterBase caster, Cell targetCell)
    {
        if (caster == null) return;
        caster.SetEtherealShieldSteps(invulnerableSteps); // Uses CharacterBase's new method
        Debug.Log($"{caster.CharacterData.characterName} is now invulnerable for {invulnerableSteps} steps.");
    }
}