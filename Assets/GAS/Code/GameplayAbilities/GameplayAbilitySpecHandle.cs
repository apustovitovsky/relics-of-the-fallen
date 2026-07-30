using System;
using System.Threading;

namespace GAS
{
    public readonly struct GameplayAbilitySpecHandle :
        IEquatable<GameplayAbilitySpecHandle>
    {
        private static int s_NextHandle;

        public int Value { get; }

        public bool IsValid =>
            Value != 0;

        /// <summary>
        /// Creates an ability specification handle from its serialized value.
        /// </summary>
        public GameplayAbilitySpecHandle(
            int value)
        {
            Value =
                value;
        }

        /// <summary>
        /// Generates a unique handle for one granted gameplay ability specification.
        /// </summary>
        internal static GameplayAbilitySpecHandle GenerateNewHandle()
        {
            int value =
                Interlocked.Increment(
                    ref s_NextHandle);

            if (value <= 0)
            {
                throw new InvalidOperationException(
                    "Gameplay ability specification handle space was exhausted.");
            }

            return new GameplayAbilitySpecHandle(
                value);
        }

        public bool Equals(
            GameplayAbilitySpecHandle other)
        {
            return Value ==
                other.Value;
        }

        public override bool Equals(
            object obj)
        {
            return
                obj is GameplayAbilitySpecHandle other &&
                Equals(other);
        }

        public override int GetHashCode()
        {
            return Value;
        }

        public override string ToString()
        {
            return IsValid
                ? Value.ToString()
                : "Invalid";
        }

        public static bool operator ==(
            GameplayAbilitySpecHandle lhs,
            GameplayAbilitySpecHandle rhs)
        {
            return lhs.Equals(
                rhs);
        }

        public static bool operator !=(
            GameplayAbilitySpecHandle lhs,
            GameplayAbilitySpecHandle rhs)
        {
            return !lhs.Equals(
                rhs);
        }
    }
}