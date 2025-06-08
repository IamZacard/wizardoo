using System;
using UnityEngine;

[Serializable]
public class MakeInvulnerableEffect : SpellEffectBase
{
    [Tooltip("Number of steps/turns the character remains invulnerable.")]
    public int invulnerableSteps = 7;

    public MakeInvulnerableEffect() { effectDescription = "Makes the character invulnerable for a number of steps."; }

    public override void Execute(CharacterBase caster, Cell targetCell)
    {
        if (caster == null) return;

        // Get the MysticModule from the caster and activate the shield
        MysticModule mysticModule = caster.GetModule<MysticModule>();
        if (mysticModule != null)
        {
            mysticModule.ActivateEtherealShield(invulnerableSteps);
        }
        else
        {
            Debug.LogWarning($"Character '{caster.CharacterData.characterName}' tried to use Ethereal Shield but does not have a MysticModule attached.");
        }
    }
}