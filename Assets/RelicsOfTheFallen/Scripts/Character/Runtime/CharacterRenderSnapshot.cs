using System;
using Unity.Netcode;
using UnityEngine;

namespace RelicsOfTheFallen.Character
{
    /// <summary>
    /// Server-authored observer snapshot. Pose and locomotion state
    /// share the same server tick and are sampled together by graphics.
    /// </summary>
    public struct CharacterRenderSnapshot :
        INetworkSerializable,
        IEquatable<CharacterRenderSnapshot>
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public CharacterLocomotionState LocomotionState;

        public void NetworkSerialize<T>(
            BufferSerializer<T> serializer)
            where T : IReaderWriter
        {
            serializer.SerializeValue(ref Position);
            serializer.SerializeValue(ref Rotation);
            serializer.SerializeValue(ref LocomotionState);
        }

        public bool Equals(
            CharacterRenderSnapshot other)
        {
            return Position.Equals(other.Position) &&
                   Rotation.Equals(other.Rotation) &&
                   LocomotionState.Equals(
                       other.LocomotionState);
        }
    }
}