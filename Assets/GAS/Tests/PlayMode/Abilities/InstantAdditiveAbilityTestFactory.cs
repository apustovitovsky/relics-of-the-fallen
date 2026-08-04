namespace GAS.Tests
{
    internal static class InstantAdditiveAbilityTestFactory
    {
        /// <summary>
        /// Creates an instant additive ability with a resource cost and duration-based cooldown.
        /// </summary>
        public static GameplayAbilitySO Create(
            AbilitySystemTestEnvironment environment,
            GameplayAbilityTargetActor targetActorPrefab,
            AttributeName affectedAttribute,
            float magnitude,
            AttributeName costAttribute,
            float costMagnitude,
            float cooldownDuration)
        {
            GameplayEffectSO gameplayEffect =
                environment.CreateScriptableObject<GameplayEffectSO>(
                    "GE_InstantAdditive");

            gameplayEffect.ge =
                new GameplayEffect()
                {
                    name = "InstantAdditive",
                    durationType =
                        GameplayEffectDurationType.Instant
                };

            gameplayEffect.ge.gameplayEffectTags.initialized = true;

            gameplayEffect.ge.modifiers.Add(
                new BasicModifier()
                {
                    attributeName = affectedAttribute,
                    value = magnitude
                });

            GameplayEffectSO costGameplayEffect =
                environment.CreateScriptableObject<GameplayEffectSO>(
                    "GE_InstantAdditiveCost");

            costGameplayEffect.ge =
                new GameplayEffect()
                {
                    name = "InstantAdditiveCost",
                    durationType =
                        GameplayEffectDurationType.Instant
                };

            costGameplayEffect.ge.gameplayEffectTags.initialized = true;

            costGameplayEffect.ge.modifiers.Add(
                new BasicModifier()
                {
                    attributeName = costAttribute,
                    value = costMagnitude
                });

            GameplayTag cooldownTag =
                GameplayTagLibrary.Instance.GetByName(
                    "Ability.Cooldown.Global");

            GameplayEffectSO cooldownGameplayEffect =
                environment.CreateScriptableObject<GameplayEffectSO>(
                    "GE_InstantAdditiveCooldown");

            cooldownGameplayEffect.ge =
                new GameplayEffect()
                {
                    name = "InstantAdditiveCooldown",
                    durationType =
                        GameplayEffectDurationType.Duration,
                    durationValue = cooldownDuration
                };

            cooldownGameplayEffect.ge.gameplayEffectTags.initialized = true;

            cooldownGameplayEffect.ge.gameplayEffectTags.GrantedTags.Add(
                cooldownTag);

            GameplayAbilitySO gameplayAbility =
                environment.CreateScriptableObject<GameplayAbilityTestAsset>(
                    "GA_InstantAdditive");

            InstantAdditiveAbility instantAdditiveAbility =
                new();

            instantAdditiveAbility.SetTargetActorPrefab(
                targetActorPrefab);

            instantAdditiveAbility.SetCostGameplayEffect(
                costGameplayEffect);

            instantAdditiveAbility.SetCooldownGameplayEffect(
                cooldownGameplayEffect);

            instantAdditiveAbility.abilityTags.initialized = true;

            instantAdditiveAbility.effectsSO.Add(
                gameplayEffect);

            gameplayAbility.ga =
                instantAdditiveAbility;

            return gameplayAbility;
        }
    }
}