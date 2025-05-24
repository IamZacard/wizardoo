using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Board/CellVisualData")]
public class CellVisualData : ScriptableObject
{
    [Header("General States")]
    public Sprite hiddenSprite;
    public Sprite flaggedSprite;
    public Sprite trapSprite;
    public Sprite revealedEmptySprite;

    [Header("Number Sprites (1–8)")]
    public List<Sprite> numberSprites; // Index 0 = 1 trap, Index 7 = 8 traps

    public Sprite GetSprite(GridCell cell)
    {
        if (!cell.IsRevealed)
        {
            return cell.IsFlagged ? flaggedSprite : hiddenSprite;
        }

        if (cell.IsTrapped)
        {
            return trapSprite; // Use sprite instead of null
        }

        if (cell.AdjacentTrapCount == 0)
        {
            return revealedEmptySprite;
        }

        int index = Mathf.Clamp(cell.AdjacentTrapCount - 1, 0, numberSprites.Count - 1);
        return numberSprites[index];
    }
}
