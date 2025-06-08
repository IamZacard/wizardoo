// SpellExecutor.cs
using System.Linq;
using UnityEngine;
using System.Collections.Generic;


public static class SpellExecutor
{
    private static Dictionary<GameObject, Queue<GameObject>> effectPools =
    new Dictionary<GameObject, Queue<GameObject>>();

    public static void ExecuteSpell(SpellData spellData, CharacterBase caster)
    {
        if (spellData == null || caster == null)
        {
            Debug.LogError("SpellExecutor: SpellData or Caster is null. Cannot execute spell.");
            CharacterSpellManager.RaiseSpellCastFailed(spellData, caster, "Spell or caster missing.");
            return;
        }

        CharacterSpellManager.RaiseSpellCastAttempted(spellData, caster);

        Cell targetCell = ResolveTarget(spellData, caster);
        if (targetCell == null)
        {
            Debug.LogWarning($"SpellExecutor: No valid target cell found for {spellData.spellName}.");
            CharacterSpellManager.RaiseSpellCastFailed(spellData, caster, "No valid target.");
            return;
        }

        Debug.Log($"[SpellExecutor] {caster.CharacterData.characterName} casts {spellData.spellName} on {targetCell.position}");

        foreach (SpellEffectBase effect in spellData.effects)
        {
            effect?.Execute(caster, targetCell);
        }

        CharacterSpellManager.RaiseSpellCastSuccess(spellData, caster);
        GameBoard.Instance?.RefreshVisuals();
    }


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

    private static GameObject GetPooledEffect(GameObject prefab)
    {
        if (!effectPools.ContainsKey(prefab))
            effectPools[prefab] = new Queue<GameObject>();

        if (effectPools[prefab].Count > 0)
            return effectPools[prefab].Dequeue();

        return Object.Instantiate(prefab);
    }
}