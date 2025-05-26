using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Core cell data structure
public class Cell
{
    public enum CellType
    {
        Empty,
        Trap,
        Number,
        Pillar,
        Shrine
    }

    public Vector3Int position;
    public CellType type;
    public int number;
    public bool revealed;
    public bool flagged;
    public bool exploded;

    public Cell(Vector3Int pos)
    {
        position = pos;
        type = CellType.Empty;
        number = 0;
        revealed = false;
        flagged = false;
        exploded = false;
    }

    public bool IsWalkable => type != CellType.Pillar;
    public bool CanPlaceTrap => type == CellType.Empty && !IsProtected;
    public bool IsProtected => type == CellType.Pillar || type == CellType.Shrine;
}