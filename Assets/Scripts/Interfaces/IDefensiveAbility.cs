// IDefensiveAbility.cs
public interface IDefensiveAbility
{
    // Checks if the entity is currently invulnerable (e.g., to traps or any instant-fail condition)
    bool IsInvulnerable();

    // Called when a potential "game over" event (like stepping on a trap) is prevented by a defensive ability.
    // This method doesn't take damage types or sources, as we're dealing with binary 'prevent/don't prevent'.
    void OnDefenseTriggered();
}