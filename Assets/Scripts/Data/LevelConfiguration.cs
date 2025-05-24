// LevelConfiguration.cs
using UnityEngine;

[CreateAssetMenu(menuName = "ArcaneDelvers/LevelConfiguration")]
public class LevelConfiguration : ScriptableObject
{
    public int Width;
    public int Height;
    public float DifficultyMultiplier;
    public string Theme;
}