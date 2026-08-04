using System;

namespace GAS
{
    /// <summary>
    /// Describes one completed gameplay ability activation.
    /// </summary>
    public readonly struct AbilityEndedData
    {
        public GameplayAbility AbilityThatEnded
        {
            get;
        }

        public GameplayAbilitySpecHandle AbilitySpecHandle
        {
            get;
        }

        public bool ReplicateEndAbility
        {
            get;
        }

        public bool WasCancelled
        {
            get;
        }

        /// <summary>
        /// Creates completion data for one gameplay ability activation.
        /// </summary>
        public AbilityEndedData(
            GameplayAbility abilityThatEnded,
            GameplayAbilitySpecHandle abilitySpecHandle,
            bool replicateEndAbility,
            bool wasCancelled)
        {
            AbilityThatEnded =
                abilityThatEnded ??
                throw new ArgumentNullException(
                    nameof(abilityThatEnded));

            AbilitySpecHandle =
                abilitySpecHandle;

            ReplicateEndAbility =
                replicateEndAbility;

            WasCancelled =
                wasCancelled;
        }
    }
}