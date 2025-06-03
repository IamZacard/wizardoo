// SpellEffects.cs (or separate files for each effect)
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

// --- CONCRETE SPELL EFFECTS ---

[Serializable]
public class RevealAreaEffect : SpellEffectBase
{
    [Tooltip("Radius of the area to reveal around the target cell.")]
    public int radius = 1;

    public RevealAreaEffect() { }

    public override void Execute(CharacterBase caster, Cell targetCell)
    {
        if (targetCell == null)
        {
            Debug.LogWarning("RevealAreaEffect: No target cell provided. Defaulting to caster's position.");
            if (caster == null) return;
            targetCell = GameBoard.Instance?.Grid.GetCell(caster.GridPosition.x, caster.GridPosition.y);
            if (targetCell == null) return;
        }
        GameBoard.Instance?.RevealCellsAroundPosition(targetCell.position, radius);
        Debug.Log($"Revealed area around {targetCell.position} with radius {radius}.");
    }
}