using UnityEngine;

public class Cell
{
    public enum Type
    {
        Empty,
        Trap,
        Number,
        Floor,
        Pillar,
        Shrine,
        Wall,
    }

    public Vector3Int position;
    public Type type;
    public int number;
    public bool revealed;
    public bool flagged;
    public bool exploded;
    public bool used;
}
