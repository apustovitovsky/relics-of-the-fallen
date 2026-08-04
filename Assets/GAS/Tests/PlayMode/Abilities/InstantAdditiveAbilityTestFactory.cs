namespace GAS.Tests
{
    internal static class InstantAdditiveAbilityTestFactory
    {
        /// <summary>
        /// Creates an instant ability that applies one additive attribute modifier.
        /// </summary>
        public static GameplayAbilitySO Create(
            AbilitySystemTestEnvironment environment,
            GameplayAbilityTargetActor targetActorPrefab,
            AttributeName attributeName,
            float magnitude)
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
                    attributeName = attributeName,
                    value = magnitude
                });

            GameplayAbilitySO gameplayAbility =
                environment.CreateScriptableObject<GameplayAbilityTestAsset>(
                    "GA_InstantAdditive");

            InstantAdditiveAbility instantAdditiveAbility =
                new();

            instantAdditiveAbility.SetTargetActorPrefab(
                targetActorPrefab);

            instantAdditiveAbility.abilityTags.initialized = true;

            instantAdditiveAbility.effectsSO.Add(
                gameplayEffect);

            gameplayAbility.ga =
                instantAdditiveAbility;

            return gameplayAbility;
        }
    }
}