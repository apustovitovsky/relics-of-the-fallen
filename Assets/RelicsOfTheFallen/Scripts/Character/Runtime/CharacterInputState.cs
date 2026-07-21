using UnityEngine;

namespace RelicsOfTheFallen.Character
{
    public readonly struct CharacterInputState
    {
        public CharacterInputState(
            Vector2 move,
            Vector2 look,
            bool sprintHeld,
            bool aimHeld,
            bool jumpPressed)
        {
            Move = move;
            Look = look;
            SprintHeld = sprintHeld;
            AimHeld = aimHeld;
            JumpPressed = jumpPressed;
        }

        public Vector2 Move { get; }

        public Vector2 Look { get; }

        public bool SprintHeld { get; }

        public bool AimHeld { get; }

        public bool JumpPressed { get; }
    }
}