public enum CharacterState
{
    Idle,        // Default state; player can move or interact
    Moving,      // Character is animating to a new cell; no inputs processed
    Interacting, // Character is engaged with an entity; movement disabled
    Disabled,    // Character is stunned or incapacitated; no actions allowed
    Dead,        // Character is defeated; game-specific logic applies
    Invulnerable
}