using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class TeleportEffect : SpellEffectBase
{
    public TeleportEffect() { effectDescription = "Teleports the character to the target cell."; }

    public override void Execute(CharacterBase caster, Cell targetCell)
    {
        if (caster == null)
        {
            Debug.LogError("TeleportEffect: Caster is null, cannot teleport.");
            return;
        }

        if (targetCell != null)
        {
            caster.MoveCharacterTo(targetCell.position); // Uses CharacterBase's method to move to grid position
            Debug.Log($"Teleported {caster.CharacterData.characterName} to {targetCell.position}.");
        }
        else
        {
            Debug.LogWarning("TeleportEffect: No valid target cell provided. Teleport failed. Ensure spell's TargetType is correctly set.");
        }
    }
}