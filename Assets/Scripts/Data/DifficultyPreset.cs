// DifficultyPreset.cs
using UnityEngine;

[CreateAssetMenu(menuName = "ArcaneDelvers/DifficultyPreset")]
public class DifficultyPreset : ScriptableObject
{
    public int BaseTraps;

    public static int CalculateTrapCount(int width, int height, float multiplier)
    {
        return Mathf.RoundToInt((width * height) * multiplier);
    }
}