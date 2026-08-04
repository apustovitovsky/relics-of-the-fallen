using System;

namespace GAS
{
    public readonly struct GameplayAbilitySpecHandleAndPredictionKey :
        IEquatable<GameplayAbilitySpecHandleAndPredictionKey>
    {
        public GameplayAbilitySpecHandle AbilityHandle
        {
            get;
        }

        public uint PredictionKeyAtCreation
        {
            get;
        }

        /// <summary>
        /// Creates a stable lookup key for replicated data belonging to one ability activation.
        /// </summary>
        public GameplayAbilitySpecHandleAndPredictionKey(
            GameplayAbilitySpecHandle abilityHandle,
            PredictionKey predictionKey)
        {
            AbilityHandle = abilityHandle;
            PredictionKeyAtCreation = predictionKey.Sequence;
        }

        public bool Equals(
            GameplayAbilitySpecHandleAndPredictionKey other)
        {
            return
                AbilityHandle == other.AbilityHandle &&
                PredictionKeyAtCreation ==
                other.PredictionKeyAtCreation;
        }

        public override bool Equals(
            object obj)
        {
            return
                obj is GameplayAbilitySpecHandleAndPredictionKey other &&
                Equals(
                    other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return
                    (AbilityHandle.GetHashCode() * 397) ^
                    PredictionKeyAtCreation.GetHashCode();
            }
        }

        public static bool operator ==(
            GameplayAbilitySpecHandleAndPredictionKey left,
            GameplayAbilitySpecHandleAndPredictionKey right)
        {
            return left.Equals(
                right);
        }

        public static bool operator !=(
            GameplayAbilitySpecHandleAndPredictionKey left,
            GameplayAbilitySpecHandleAndPredictionKey right)
        {
            return !left.Equals(
                right);
        }
    }
}