using System;
using UnityEngine;

[Serializable]
public class RevealClosestTrapEffect : SpellEffectBase
{
    [Tooltip("Max distance to search for the closest trap.")]
    public int searchRange = 10;

    public RevealClosestTrapEffect() { }

    public override void Execute(CharacterBase caster, Cell targetCell)
    {
        if (caster == null) return;
        Cell closestTrap = FindClosestTrap(caster.GridPosition, searchRange);
        if (closestTrap != null)
        {
            GameBoard.Instance?.HandleManualCellClick(closestTrap.position.x, closestTrap.position.y); // Reveals it
            Debug.Log($"Revealed closest trap at {closestTrap.position}.");
        }
        else
        {
            Debug.Log("No unrevealed trap found within range for RevealClosestTrapEffect.");
        }
    }

    private Cell FindClosestTrap(Vector3Int startPosition, int range)
    {
        GameBoard gameBoard = GameBoard.Instance;
        if (gameBoard == null || gameBoard.Grid == null) return null;

        Cell closestTrap = null;
        float minDistanceSqr = float.MaxValue;

        foreach (Cell cell in gameBoard.Grid.GetAllCells())
        {
            if (cell.type == Cell.CellType.Trap && !cell.revealed)
            {
                float distSqr = (cell.position - startPosition).sqrMagnitude;
                if (distSqr <= range * range && distSqr < minDistanceSqr)
                {
                    minDistanceSqr = distSqr;
                    closestTrap = cell;
                }
            }
        }
        return closestTrap;
    }
}