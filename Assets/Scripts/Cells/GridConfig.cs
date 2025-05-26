using UnityEngine;

[CreateAssetMenu(fileName = "New GridConfig", menuName = "ArcaneDelvers/GridConfig")]
public class GridConfig : ScriptableObject
{
    public int width;
    public int height;
    [Range(0f, 1f)] public float trapDensity;
}
