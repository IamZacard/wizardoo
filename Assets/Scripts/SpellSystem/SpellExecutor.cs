// SpellExecutor.cs
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;


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

        // Execute all effects associated with the spell
        foreach (SpellEffectBase effect in spellData.effects)
        {
            effect.Execute(caster, targetCell); // Pass the target cell for effects that need it
        }
      
        if (spellData.visualEffect != null)
        {
            // Instantiate the visual effect. You might want to pool these for performance!
            GameObject instantiatedEffect = GameObject.Instantiate(spellData.visualEffect);
            // You might need to adjust this based on spell.targetType.
            instantiatedEffect.transform.position = caster.transform.position;
            ApplyEffectColor(instantiatedEffect, spellData.effectColor);
            ParticleSystem ps = instantiatedEffect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Play();
                GameObject.Destroy(instantiatedEffect, ps.main.duration + ps.main.startLifetime.constantMax);
            }
            else
            {
                // If it's just a static visual, destroy it after a short time
                GameObject.Destroy(instantiatedEffect, spellData.activeDuration > 0 ? spellData.activeDuration : 2f); // Or a default duration
            }

            Debug.Log($"Played visual effect for spell: {spellData.spellName}");
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

    private static void ApplyEffectColor(GameObject effect, Color color)
    {
        // This is a simple example. You might need to target specific renderers or particle systems.
        SpriteRenderer sr = effect.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = color;
        }

        ParticleSystem ps = effect.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            var main = ps.main;
            main.startColor = color;
        }
    }
}