using System;
using UnityEngine;

[Serializable]
public class DisarmTrapEffect : SpellEffectBase
{
    public DisarmTrapEffect() { effectDescription = "Triggers after character step on a trap."; } 

    public override void Execute(CharacterBase caster, Cell targetCell)
    {
        if (targetCell == null)
        {
            Debug.LogWarning("DisarmTrapEffect: No target cell provided.");
            return;
        }

        var characterData = caster?.CharacterData;
        if (characterData == null)
        {
            Debug.LogWarning("DisarmTrapEffect: No CharacterData found on caster.");
            return;
        }

        GameBoard.Instance?.HandleCellFlag(targetCell.position, characterData.flagEffect); // lowercase 'flagEffect'
        Debug.Log($"Disarmed trap at {targetCell.position}.");
    }

}