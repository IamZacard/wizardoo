using UnityEngine;

[CreateAssetMenu(menuName = "ArcaneDelvers/GameStateData")]
public class GameStateData : ScriptableObject
{
    public int totalDeaths;
    public float bestTime;
    public int bestScore;
}
