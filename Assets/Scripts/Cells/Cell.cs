using UnityEngine;

public class Cell
{
    public enum Type
    {
        Empty,
        Mine,
        Number,
        Floor,
        Pillar,
        Shrine,
    }

    public Vector3Int position;
    public Type type;
    public int number;
    public bool revealed;
    public bool flagged;
    public bool exploded;
    public bool used;
}
