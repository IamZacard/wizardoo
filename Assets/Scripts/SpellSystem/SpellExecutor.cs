// SpellExecutor.cs
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A static class responsible for executing the actual effects of a spell.
/// It acts as the central "engine" that interprets SpellData and applies changes to the game world.
/// </summary>
public static class SpellExecutor
{
    /// <summary>
    /// Executes a given spell's effects for a specific caster.
    /// </summary>
    /// <param name="spellData">The SpellData ScriptableObject defining the spell.</param>
    /// <param name="caster">The CharacterBase instance that is casting the spell.</param>
    public static void ExecuteSpell(SpellData spellData, CharacterBase caster)
    {
        if (spellData == null || caster == null)
        {
            Debug.LogError("SpellData or Caster is null. Cannot execute spell.");
            return;
        }

        Cell targetCell = ResolveTarget(spellData, caster);

        Debug.Log($"Executing spell: {spellData.spellName} by {caster.CharacterData.characterName} on target: {targetCell?.position ?? Vector3Int.zero}", caster);

        // Execute each effect defined in the SpellData
        foreach (SpellEffectBase effect in spellData.effects)
        {
            if (effect != null)
            {
                effect.Execute(caster, targetCell);
            }
        }

        // Handle resource consumption if the spell consumes charges
        if (spellData.consumesCharges && spellData.resourceType == ResourceType.Charges)
        {
            // This assumes CharacterSpellManager handles charges internally.
            // A more robust system might have a direct way to decrement charges here if they are global/shared.
            // For now, CharacterSpellManager will manage this.
        }

        // After effects, refresh visuals if necessary
        GameBoard.Instance?.RefreshVisuals();
    }

    /// <summary>
    /// Resolves the target cell based on the spell's TargetType.
    /// </summary>
    /// <param name="spellData">The SpellData defining the targeting.</param>
    /// <param name="caster">The CharacterBase instance that is casting the spell.</param>
    /// <returns>The target Cell, or null if no valid target could be found.</returns>
    private static Cell ResolveTarget(SpellData spellData, CharacterBase caster)
    {
        GameGrid gameGrid = GameBoard.Instance?.Grid;
        if (gameGrid == null)
        {
            Debug.LogError("GameGrid is not available for spell targeting.");
            return null;
        }

        switch (spellData.targetType)
        {
            case TargetType.Self:
            case TargetType.CurrentPosition:
                return gameGrid.GetCell(caster.GridPosition.x, caster.GridPosition.y);

            case TargetType.MousePosition:
                // For MousePosition, we need the current mouse world position
                // This typically needs to be passed from the input handler,
                // but for a static executor, we can try to get it from Camera.main.
                // NOTE: This might be less precise than passing it directly from CharacterSpellManager.
                Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                return GameBoard.Instance?.GetCellAtWorldPosition(mouseWorldPos);

            case TargetType.ClosestTrap:
                return FindClosestTrap(caster.GridPosition, gameGrid, spellData.range);

            case TargetType.RandomCell:
                return FindRandomUnrevealedCell(gameGrid);

            case TargetType.AreaAroundSelf:
                // For AreaAroundSelf, the target cell is typically the caster's current position,
                // and the effect itself will handle the "area" part.
                return gameGrid.GetCell(caster.GridPosition.x, caster.GridPosition.y);

            default:
                Debug.LogWarning($"Unknown TargetType: {spellData.targetType}");
                return null;
        }
    }

    /// <summary>
    /// Finds the closest unrevealed trap cell within a given range.
    /// </summary>
    private static Cell FindClosestTrap(Vector3Int startPos, GameGrid grid, int range)
    {
        Cell closestTrap = null;
        float minDistanceSqr = float.MaxValue;

        // Iterate through all cells, prioritizing unrevealed traps within range
        foreach (Cell cell in grid.GetAllCells())
        {
            if (cell.type == Cell.CellType.Trap && !cell.revealed)
            {
                float distSqr = (cell.position - startPos).sqrMagnitude;
                if (distSqr <= range * range && distSqr < minDistanceSqr)
                {
                    minDistanceSqr = distSqr;
                    closestTrap = cell;
                }
            }
        }
        return closestTrap;
    }

    /// <summary>
    /// Finds a random unrevealed, non-protected cell.
    /// </summary>
    private static Cell FindRandomUnrevealedCell(GameGrid grid)
    {
        List<Cell> availableCells = grid.GetAllCells()
                                        .Where(c => !c.revealed && !c.IsProtected)
                                        .ToList();

        if (availableCells.Count == 0) return null;

        return availableCells[Random.Range(0, availableCells.Count)];
    }
}