using System;
using Unity.Netcode;
using UnityEngine;

namespace RelicsOfTheFallen.Character
{
    public enum CharacterGait : byte
    {
        Idle,
        Walk,
        Run,
        Sprint
    }

    /// <summary>
    /// Server-authored state used exclusively by client presentation.
    /// </summary>
    public struct CharacterLocomotionState :
        INetworkSerializable,
        IEquatable<CharacterLocomotionState>
    {
        public uint ServerTick;
        public uint LastProcessedInputSequence;

        public uint AirborneSinceTick;
        public uint MovementStartedTick;
        public ushort LocomotionStartSequence;

        public Vector3 Velocity;
        public Vector2 MoveInput;

        public float FacingYaw;
        public float AimYaw;
        public float AimPitch;
        public float InclineAngle;
        public float CameraRotationOffset;

        public CharacterGait Gait;

        public bool IsGrounded;
        public bool IsStrafing;
        public bool IsTurningInPlace;
        public bool IsJumping;
        public bool IsCrouching;

        public void NetworkSerialize<T>(
            BufferSerializer<T> serializer)
            where T : IReaderWriter
        {
            serializer.SerializeValue(ref ServerTick);
            serializer.SerializeValue(
                ref LastProcessedInputSequence);

            serializer.SerializeValue(
                ref AirborneSinceTick);

            serializer.SerializeValue(
                ref MovementStartedTick);

            serializer.SerializeValue(
                ref LocomotionStartSequence);

            serializer.SerializeValue(ref Velocity);
            serializer.SerializeValue(ref MoveInput);

            serializer.SerializeValue(ref FacingYaw);
            serializer.SerializeValue(ref AimYaw);
            serializer.SerializeValue(ref AimPitch);
            serializer.SerializeValue(ref InclineAngle);

            serializer.SerializeValue(
                ref CameraRotationOffset);

            byte gait = (byte)Gait;
            serializer.SerializeValue(ref gait);

            if (serializer.IsReader)
            {
                Gait = (CharacterGait)gait;
            }

            serializer.SerializeValue(ref IsGrounded);
            serializer.SerializeValue(ref IsStrafing);

            serializer.SerializeValue(
                ref IsTurningInPlace);

            serializer.SerializeValue(ref IsJumping);
            serializer.SerializeValue(ref IsCrouching);
        }

        public bool Equals(
            CharacterLocomotionState other)
        {
            return ServerTick == other.ServerTick &&
                   LastProcessedInputSequence ==
                   other.LastProcessedInputSequence &&
                   AirborneSinceTick ==
                   other.AirborneSinceTick &&
                   MovementStartedTick ==
                   other.MovementStartedTick &&
                   LocomotionStartSequence ==
                   other.LocomotionStartSequence &&
                   Velocity.Equals(other.Velocity) &&
                   MoveInput.Equals(other.MoveInput) &&
                   Mathf.Approximately(
                       FacingYaw,
                       other.FacingYaw) &&
                   Mathf.Approximately(
                       AimYaw,
                       other.AimYaw) &&
                   Mathf.Approximately(
                       AimPitch,
                       other.AimPitch) &&
                   Mathf.Approximately(
                       InclineAngle,
                       other.InclineAngle) &&
                   Mathf.Approximately(
                       CameraRotationOffset,
                       other.CameraRotationOffset) &&
                   Gait == other.Gait &&
                   IsGrounded == other.IsGrounded &&
                   IsStrafing == other.IsStrafing &&
                   IsTurningInPlace ==
                   other.IsTurningInPlace &&
                   IsJumping == other.IsJumping &&
                   IsCrouching == other.IsCrouching;
        }
    }
}