using Unity.Netcode;
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

    public struct CharacterInputCommand : INetworkSerializable
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

        public bool IsPressed(CharacterInputPressedButtons button)
        {
            return (PressedButtons & button) != 0;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer)
            where T : IReaderWriter
        {
            serializer.SerializeValue(ref Sequence);
            serializer.SerializeValue(ref Move);
            serializer.SerializeValue(ref LookYaw);
            serializer.SerializeValue(ref LookPitch);

            byte heldButtons = (byte)HeldButtons;
            serializer.SerializeValue(ref heldButtons);

            byte pressedButtons = (byte)PressedButtons;
            serializer.SerializeValue(ref pressedButtons);

            if (serializer.IsReader)
            {
                HeldButtons = (CharacterInputHeldButtons)heldButtons;
                PressedButtons = (CharacterInputPressedButtons)pressedButtons;
            }
        }
    }
}