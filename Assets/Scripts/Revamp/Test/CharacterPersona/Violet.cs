using Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Violet : CharacterBase
{
    public bool hasTeleported;
    public override void Move(Vector2 direction)
    {
        base.Move(direction);
        hasTeleported = false;
    }
}
