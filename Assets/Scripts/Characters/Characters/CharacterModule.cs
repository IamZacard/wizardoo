// CharacterModule.cs
using UnityEngine;

public abstract class CharacterModule : MonoBehaviour
{
    protected CharacterBase character;

    // Called once the module is added and the character is initialized
    public virtual void Initialize(CharacterBase owner)
    {
        character = owner;
    }

    public virtual void OnCharacterStep() { }
        
    // public virtual void OnSpellCast(SpellData spellData) { }
}