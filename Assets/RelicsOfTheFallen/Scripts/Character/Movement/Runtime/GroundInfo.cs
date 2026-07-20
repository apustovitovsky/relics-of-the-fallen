using UnityEngine;

namespace RelicsOfTheFallen.Character.Movement
{
    public readonly struct GroundInfo
    {
        public bool HasSurface { get; }

        public bool IsGrounded { get; }

        public Vector3 Point { get; }

        public Vector3 Normal { get; }

        public float SlopeAngle { get; }

        public float InclineAngle { get; }

        public GroundInfo(
            bool hasSurface,
            bool isGrounded,
            Vector3 point,
            Vector3 normal,
            float slopeAngle,
            float inclineAngle)
        {
            HasSurface = hasSurface;
            IsGrounded = isGrounded;
            Point = point;
            Normal = normal;
            SlopeAngle = slopeAngle;
            InclineAngle = inclineAngle;
        }

        public GroundInfo WithIsGrounded(bool isGrounded)
        {
            return new GroundInfo(
                HasSurface,
                isGrounded,
                Point,
                Normal,
                SlopeAngle,
                InclineAngle);
        }
    }
}