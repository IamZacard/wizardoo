// MysticModule.cs
using UnityEngine;

public class MysticModule : CharacterModule, IDefensiveAbility
{
    private Animator anim;

    [Tooltip("Number of steps the character remains invulnerable.")]
    private int etherealShieldStepsRemaining;

    private void OnDisable()
    {
        if (GameBoard.Instance != null)
            GameBoard.Instance.OnCharacterStepEvent.RemoveListener(HandleCharacterStepped);
    }

    public override void Initialize(CharacterBase owner)
    {
        base.Initialize(owner);

        if (GameBoard.Instance != null)
        {
            GameBoard.Instance.OnCharacterStepEvent.AddListener(HandleCharacterStepped);
        }

        anim = GetComponent<Animator>();
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (GameBoard.Instance != null)
        {
            GameBoard.Instance.OnCharacterStepEvent.RemoveListener(HandleCharacterStepped);
        }
    }

    private void HandleCharacterStepped(CharacterBase steppedCharacter)
    {
        // Only decrement if this module belongs to the character that just stepped
        if (steppedCharacter == character)
        {
            OnCharacterStep(); // Call the modular step logic
        }
    }

    // IDefensiveAbility Implementation for Invulnerability Check
    public bool IsInvulnerable()
    {
        return etherealShieldStepsRemaining > 0;
    }

    // IDefensiveAbility Implementation for when a defense is triggered (e.g., trap avoided)
    public void OnDefenseTriggered()
    {
        // This is called when a potential game-ending event (like a trap) is prevented.
        // The invulnerability steps are NOT consumed by this, they are consumed by character movement.
        Debug.Log($"Ethereal Shield *activated* and prevented a trap! Steps remaining: {etherealShieldStepsRemaining}");
        // TODO: Play a distinct "trap averted" visual or sound effect here!
        // Maybe a flash, a temporary distortion, or a specific sound cue.
    }

    public void ActivateEtherealShield(int steps)
    {
        etherealShieldStepsRemaining = steps;
        if (steps > 0)
        {
            // Set the character's state. CharacterBase needs a SetState method.
            character.ForceSetState(CharacterState.Invulnerable);

            if (anim != null) anim.SetBool("Invincible", true);

            Debug.Log($"{character.CharacterData.characterName} is now invulnerable for {steps} steps.");
        }
    }

    public override void OnCharacterStep()
    {
        if (etherealShieldStepsRemaining > 0)
        {
            etherealShieldStepsRemaining--;
            Debug.Log($"Invulnerability steps remaining: {etherealShieldStepsRemaining}");
        }

        if (etherealShieldStepsRemaining == 0)
        {
            // Force character state if necessary
            if (character.IsInState(CharacterState.Invulnerable))
            {
                character.ForceSetState(CharacterState.Idle);
                Debug.Log("Invulnerability ended.");
            }

            // Always disable animation regardless of current state
            anim.SetBool("Invincible", false);
        }

        if (GameStateManager.Instance.CurrentState == GameState.Won)
        {
            character.ForceSetState(CharacterState.Idle);
            anim.SetBool("Invincible", false);
        }
    }
}