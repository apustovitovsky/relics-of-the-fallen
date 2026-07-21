using System;
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
    /// Replicated locomotion state consumed exclusively by presentation.
    /// </summary>
    public struct CharacterLocomotionState :
        IEquatable<CharacterLocomotionState>
    {
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

        public bool Equals(
            CharacterLocomotionState other)
        {
            return LocomotionStartSequence ==
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