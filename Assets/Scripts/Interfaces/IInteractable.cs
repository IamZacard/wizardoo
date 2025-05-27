using UnityEngine;

// Interface for interactable objects (shrines, NPCs, etc.)
public interface IInteractable
{
    void StartInteraction(CharacterBase character);
    void EndInteraction(CharacterBase character);
    bool CanInteract(CharacterBase character);
    string GetInteractionPrompt();
}