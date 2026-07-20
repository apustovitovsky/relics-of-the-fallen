using System;
using Unity.Netcode;
using UnityEngine;

namespace RelicsOfTheFallen.Character
{
    public enum CharacterAirState : byte
    {
        Grounded,
        Jumping,
        Falling
    }

    /// <summary>
    /// A server-authoritative locomotion snapshot sent only to the
    /// owning client. It contains the complete state needed to restore
    /// and replay the local locomotion simulation.
    /// </summary>
    public struct CharacterOwnerSnapshot :
        INetworkSerializable,
        IEquatable<CharacterOwnerSnapshot>
    {
        public uint ServerTick;
        public uint LastProcessedInputSequence;

        public Vector3 Position;
        public Quaternion Rotation;

        public Vector3 HorizontalVelocity;
        public float VerticalVelocity;
        public float CameraRotationOffset;

        public bool IsWalking;
        public bool IsCrouchRequested;
        public bool IsCrouching;
        public bool IsGrounded;

        public CharacterAirState AirState;

        public void NetworkSerialize<T>(
            BufferSerializer<T> serializer)
            where T : IReaderWriter
        {
            serializer.SerializeValue(ref ServerTick);
            serializer.SerializeValue(
                ref LastProcessedInputSequence);

            serializer.SerializeValue(ref Position);
            serializer.SerializeValue(ref Rotation);

            serializer.SerializeValue(
                ref HorizontalVelocity);

            serializer.SerializeValue(
                ref VerticalVelocity);

            serializer.SerializeValue(
                ref CameraRotationOffset);

            serializer.SerializeValue(ref IsWalking);
            serializer.SerializeValue(
                ref IsCrouchRequested);

            serializer.SerializeValue(ref IsCrouching);
            serializer.SerializeValue(ref IsGrounded);

            byte airState = (byte)AirState;
            serializer.SerializeValue(ref airState);

            if (serializer.IsReader)
            {
                AirState = (CharacterAirState)airState;
            }
        }

        public bool Equals(CharacterOwnerSnapshot other)
        {
            return ServerTick == other.ServerTick &&
                   LastProcessedInputSequence ==
                   other.LastProcessedInputSequence &&
                   Position.Equals(other.Position) &&
                   Rotation.Equals(other.Rotation) &&
                   HorizontalVelocity.Equals(
                       other.HorizontalVelocity) &&
                   Mathf.Approximately(
                       VerticalVelocity,
                       other.VerticalVelocity) &&
                   Mathf.Approximately(
                       CameraRotationOffset,
                       other.CameraRotationOffset) &&
                   IsWalking == other.IsWalking &&
                   IsCrouchRequested ==
                   other.IsCrouchRequested &&
                   IsCrouching == other.IsCrouching &&
                   IsGrounded == other.IsGrounded &&
                   AirState == other.AirState;
        }
    }
}