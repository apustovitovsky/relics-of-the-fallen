using System;
using System.Threading;

namespace GAS
{
    public readonly struct ActiveGameplayEffectHandle :
        IEquatable<ActiveGameplayEffectHandle>
    {
        private static long s_NextHandle;

        private readonly ulong m_Value;

        private readonly bool m_WasSuccessfullyApplied;

        public bool IsValid =>
            m_Value != 0;

        public bool WasSuccessfullyApplied =>
            m_WasSuccessfullyApplied;

        private ActiveGameplayEffectHandle(
            ulong value,
            bool wasSuccessfullyApplied)
        {
            m_Value =
                value;

            m_WasSuccessfullyApplied =
                wasSuccessfullyApplied;
        }

        /// <summary>
        /// Generates a successfully applied handle for one active effect.
        /// </summary>
        internal static ActiveGameplayEffectHandle GenerateNewHandle()
        {
            long generatedValue =
                Interlocked.Increment(
                    ref s_NextHandle);

            if (generatedValue <= 0)
            {
                throw new InvalidOperationException(
                    "Active gameplay effect handle space was exhausted.");
            }

            return new ActiveGameplayEffectHandle(
                (ulong)generatedValue,
                true);
        }

        /// <summary>
        /// Returns a successful result that does not identify an active effect.
        /// </summary>
        internal static ActiveGameplayEffectHandle GetInstantExecutedHandle()
        {
            return new ActiveGameplayEffectHandle(
                0,
                true);
        }

        public bool Equals(
            ActiveGameplayEffectHandle other)
        {
            return
                m_Value ==
                other.m_Value &&
                m_WasSuccessfullyApplied ==
                other.m_WasSuccessfullyApplied;
        }

        public override bool Equals(
            object obj)
        {
            return
                obj is ActiveGameplayEffectHandle other &&
                Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                m_Value,
                m_WasSuccessfullyApplied);
        }

        public override string ToString()
        {
            return IsValid
                ? m_Value.ToString()
                : WasSuccessfullyApplied
                    ? "InstantExecuted"
                    : "Invalid";
        }

        public static bool operator ==(
            ActiveGameplayEffectHandle lhs,
            ActiveGameplayEffectHandle rhs)
        {
            return lhs.Equals(
                rhs);
        }

        public static bool operator !=(
            ActiveGameplayEffectHandle lhs,
            ActiveGameplayEffectHandle rhs)
        {
            return !lhs.Equals(
                rhs);
        }
    }
}