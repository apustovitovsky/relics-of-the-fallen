using System;

namespace GAS
{
    public readonly struct PredictionKey :
        IEquatable<PredictionKey>
    {
        public uint Sequence { get; }

        public bool IsValid =>
            Sequence != 0;

        /// <summary>
        /// Creates an identity for one owner-scoped predicted activation.
        /// </summary>
        public PredictionKey(
            uint sequence)
        {
            Sequence = sequence;
        }

        public bool Equals(
            PredictionKey other)
        {
            return Sequence ==
                other.Sequence;
        }

        public override bool Equals(
            object obj)
        {
            return
                obj is PredictionKey other &&
                Equals(other);
        }

        public override int GetHashCode()
        {
            return (int)Sequence;
        }

        public static bool operator ==(
            PredictionKey lhs,
            PredictionKey rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(
            PredictionKey lhs,
            PredictionKey rhs)
        {
            return !lhs.Equals(rhs);
        }
    }
}