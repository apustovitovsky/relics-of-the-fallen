using System;
using Unity.Netcode;
using UnityEngine;

namespace RelicsOfTheFallen.Character
{
    public enum CharacterGait : byte
    {
        Idle,
        Run,
        Sprint
    }

    public struct CharacterLocomotionState :
        INetworkSerializable,
        IEquatable<CharacterLocomotionState>
    {
        public uint ServerTick;
        public uint LastProcessedInputTick;
        public Vector3 Velocity;
        public Vector2 MoveInput;
        public float FacingYaw;
        public float AimYaw;
        public float AimPitch;
        public CharacterGait Gait;
        public bool IsGrounded;

        public void NetworkSerialize<T>(
            BufferSerializer<T> serializer)
            where T : IReaderWriter
        {
            serializer.SerializeValue(ref ServerTick);
            serializer.SerializeValue(ref LastProcessedInputTick);
            serializer.SerializeValue(ref Velocity);
            serializer.SerializeValue(ref MoveInput);
            serializer.SerializeValue(ref FacingYaw);
            serializer.SerializeValue(ref AimYaw);
            serializer.SerializeValue(ref AimPitch);

            byte gait = (byte)Gait;
            serializer.SerializeValue(ref gait);

            if (serializer.IsReader)
            {
                Gait = (CharacterGait)gait;
            }

            serializer.SerializeValue(ref IsGrounded);
        }

        public bool Equals(CharacterLocomotionState other)
        {
            return ServerTick == other.ServerTick &&
                   LastProcessedInputTick ==
                   other.LastProcessedInputTick &&
                   Velocity.Equals(other.Velocity) &&
                   MoveInput.Equals(other.MoveInput) &&
                   FacingYaw.Equals(other.FacingYaw) &&
                   AimYaw.Equals(other.AimYaw) &&
                   AimPitch.Equals(other.AimPitch) &&
                   Gait == other.Gait &&
                   IsGrounded == other.IsGrounded;
        }
    }
}