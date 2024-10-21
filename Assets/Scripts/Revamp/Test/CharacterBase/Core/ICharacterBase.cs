using System;
using UnityEngine;

namespace Core {
    public interface ICharacterBase
    {
        void Move(Vector2 direction);

        void Reveal();

        void Flag();

        void Explode(Cell cell);

        void Interact();
    }
}


