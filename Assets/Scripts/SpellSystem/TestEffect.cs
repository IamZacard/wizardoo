using System;
using UnityEngine;


[Serializable]
public class TestEffect : SpellEffectBase
{
    public override void Execute(CharacterBase caster, Cell targetCell)
    {
        Debug.Log("Test effect executed.");
    }
}