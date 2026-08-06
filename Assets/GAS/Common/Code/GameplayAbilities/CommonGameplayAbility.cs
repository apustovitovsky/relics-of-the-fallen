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
            set;
        } = GameplayAbilityActivationPolicy.OnInputTriggered;

        [field: SerializeField]
        protected GameplayAbilityActivationGroup ActivationGroup
        {
            get;
            set;
        } = GameplayAbilityActivationGroup.Independent;

        /// <summary>
        /// Returns the policy controlling when this common gameplay ability attempts activation.
        /// </summary>
        public GameplayAbilityActivationPolicy GetActivationPolicy()
        {
            return ActivationPolicy;
        }

        /// <summary>
        /// Returns the group controlling this ability's relationship with other active abilities.
        /// </summary>
        public GameplayAbilityActivationGroup GetActivationGroup()
        {
            return ActivationGroup;
        }

        /// <summary>
        /// Determines whether this ability can activate under the current common activation-group rules.
        /// </summary>
        public override bool CanActivateAbility(
            GameplayAbilitySpecHandle handle,
            GameplayAbilityActorInfo actorInfo,
            GameplayTagContainer sourceTags = null,
            GameplayTagContainer targetTags = null,
            GameplayTagContainer optionalRelevantTags = null)
        {
            if (
                !base.CanActivateAbility(
                    handle,
                    actorInfo,
                    sourceTags,
                    targetTags,
                    optionalRelevantTags))
            {
                return false;
            }

            if (
                actorInfo.AbilitySystemComponent is not
                    CommonAbilitySystemComponent abilitySystem)
            {
                throw new InvalidOperationException(
                    "Common gameplay abilities require a common ability system component.");
            }

            if (
                !abilitySystem.IsActivationGroupBlocked(
                    ActivationGroup))
            {
                return true;
            }

            optionalRelevantTags?.AddTag(
                CommonGameplayTags.ActivateFailActivationGroupTag);

            return false;
        }

        /// <summary>
        /// Creates a runtime common ability instance preserving its activation configuration.
        /// </summary>
        public override GameplayAbility Instantiate(
            AbilitySystemComponent owner)
        {
            CommonGameplayAbility ability =
                (CommonGameplayAbility)base.Instantiate(
                    owner);

            ability.ActivationPolicy =
                ActivationPolicy;

            ability.ActivationGroup =
                ActivationGroup;

            return ability;
        }
    }
}