using UnityEngine;

public abstract class Spell : ScriptableObject
{
    public string spellName; // Name of the spell
    
    // Cast method to be overridden by specific spell logic
    public abstract void Cast(CharacterBase caster, object context = null); 
}