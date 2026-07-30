using System;

namespace GAS
{
    public readonly struct GameplayEffectModCallbackData
    {
        public GameplayEffectSpec EffectSpec { get; }

        public GameplayModifierEvaluatedData EvaluatedData { get; }

        public AbilitySystemComponent Target { get; }

        /// <summary>
        /// Provides effect execution context to attribute processors.
        /// </summary>
        internal GameplayEffectModCallbackData(
            GameplayEffectSpec effectSpec,
            GameplayModifierEvaluatedData evaluatedData,
            AbilitySystemComponent target)
        {
            EffectSpec = effectSpec ?? throw new ArgumentNullException(
                    nameof(effectSpec));

            if (!evaluatedData.IsValid)
            {
                throw new ArgumentException(
                    "Gameplay effect callback data requires evaluated modifier data.",
                    nameof(evaluatedData));
            }

            EvaluatedData =
                evaluatedData;

            Target = target != null
                ? target
                : throw new ArgumentNullException(
                    nameof(target));
        }
    }
}