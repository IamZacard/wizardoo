using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewStats", menuName = "CharacterStats")]
public class Stats : ScriptableObject
{
    public float _lightRadius;
    public float _flagCount;
    public float _trapCountIncrease;
}
