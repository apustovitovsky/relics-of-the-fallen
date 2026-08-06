using System;
using UnityEngine;

namespace GAS.Common
{
    [Serializable]
    public class CommonGameplayAbility :
        GameplayAbility
    {
        [field: SerializeField]
        protected GameplayAbilityActivationPolicy ActivationPolicy
        {
            get;
            private set;
        } = GameplayAbilityActivationPolicy.OnInputTriggered;

        /// <summary>
        /// Returns the policy controlling when this common gameplay ability attempts activation.
        /// </summary>
        public GameplayAbilityActivationPolicy GetActivationPolicy()
        {
            return ActivationPolicy;
        }

        /// <summary>
        /// Creates a runtime common ability instance preserving its activation policy.
        /// </summary>
        public override GameplayAbility Instantiate(
            AbilitySystemComponent owner)
        {
            CommonGameplayAbility ability =
                (CommonGameplayAbility)base.Instantiate(
                    owner);

            ability.ActivationPolicy =
                ActivationPolicy;

            return ability;
        }
    }
}