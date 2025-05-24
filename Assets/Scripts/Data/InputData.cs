using UnityEngine;

public struct InputData
{
    public Vector2 WorldPosition;
    public bool IsLeftClick;
    public bool IsRightClick;

    public InputData(Vector2 position, bool left, bool right)
    {
        WorldPosition = position;
        IsLeftClick = left;
        IsRightClick = right;
    }
}
