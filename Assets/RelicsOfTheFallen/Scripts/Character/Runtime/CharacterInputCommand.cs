using UnityEngine;

namespace RelicsOfTheFallen.Character
{
    [System.Flags]
    public enum CharacterInputHeldButtons : byte
    {
        None = 0,
        Sprint = 1 << 0,
        Aim = 1 << 1,

        All = Sprint | Aim
    }

    [System.Flags]
    public enum CharacterInputPressedButtons : byte
    {
        None = 0,
        WalkToggle = 1 << 0,
        Jump = 1 << 1,
        CrouchToggle = 1 << 2,

        All = WalkToggle | Jump | CrouchToggle
    }

    public struct CharacterInputCommand
    {
        public uint Sequence;
        public Vector2 Move;
        public float LookYaw;
        public float LookPitch;
        public CharacterInputHeldButtons HeldButtons;
        public CharacterInputPressedButtons PressedButtons;

        public bool IsHeld(CharacterInputHeldButtons button)
        {
            return (HeldButtons & button) != 0;
        }

        public bool IsPressed(
            CharacterInputPressedButtons button)
        {
            return (PressedButtons & button) != 0;
        }
    }
}