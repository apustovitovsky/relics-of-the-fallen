using System.Collections.Generic;

namespace GAS
{
    public static class TagProcessor
    {
        /// <summary>
        /// Returns whether the owned tags satisfy required and forbidden tag collections.
        /// </summary>
        private static bool CheckTagRequirements(
            AbilitySystemComponent abilitySystem,
            IReadOnlyList<GameplayTag> requiredTags,
            IReadOnlyList<GameplayTag> forbiddenTags)
        {
            return
                abilitySystem.HasAllMatchingGameplayTags(
                    requiredTags) &&
                !abilitySystem.HasAnyMatchingGameplayTags(
                    forbiddenTags);
        }

        /// <summary>
        /// Returns whether a gameplay effect satisfies its target application requirements.
        /// </summary>
        public static bool CheckApplicationTagRequirementsGE(
            AbilitySystemComponent abilitySystem,
            GameplayEffect gameplayEffect,
            List<GameplayTag> legacyTags)
        {
            return
                CheckTagRequirements(
                    abilitySystem,
                    gameplayEffect
                        .gameplayEffectTags
                        .ApplicationTagRequirementsRequired,
                    gameplayEffect
                        .gameplayEffectTags
                        .ApplicationTagRequirementsForbidden);
        }
    }
}