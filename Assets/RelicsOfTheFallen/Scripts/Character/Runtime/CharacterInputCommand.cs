using Unity.Netcode;
using UnityEngine;

namespace RelicsOfTheFallen.Character
{
    [System.Flags]
    public enum CharacterInputButtons : ushort
    {
        None = 0,
        SprintHeld = 1 << 0
    }

    public struct CharacterInputCommand : INetworkSerializable
    {
        public uint Tick;
        public Vector2 Move;
        public float LookYaw;
        public float LookPitch;
        public CharacterInputButtons Buttons;

        public bool IsPressed(CharacterInputButtons button)
        {
            return (Buttons & button) != 0;
        }

        public void NetworkSerialize<T>(
            BufferSerializer<T> serializer)
            where T : IReaderWriter
        {
            serializer.SerializeValue(ref Tick);
            serializer.SerializeValue(ref Move);
            serializer.SerializeValue(ref LookYaw);
            serializer.SerializeValue(ref LookPitch);

            ushort buttons = (ushort)Buttons;
            serializer.SerializeValue(ref buttons);

            if (serializer.IsReader)
            {
                Buttons = (CharacterInputButtons)buttons;
            }
        }
    }
}