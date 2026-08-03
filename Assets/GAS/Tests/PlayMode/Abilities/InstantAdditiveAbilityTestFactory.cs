namespace GAS.Tests
{
    internal static class InstantAdditiveAbilityTestFactory
    {
        /// <summary>
        /// Creates an instant ability that applies one additive attribute modifier.
        /// </summary>
        public static GameplayAbilitySO_InstantAbility Create(
            AbilitySystemTestEnvironment environment,
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

            GameplayAbilitySO_InstantAbility gameplayAbility =
                environment.CreateScriptableObject<GameplayAbilitySO_InstantAbility>(
                    "GA_InstantAdditive");

            gameplayAbility.ga =
                new InstantAbility();

            gameplayAbility.ga.abilityTags.initialized = true;

            gameplayAbility.ga.effectsSO.Add(
                gameplayEffect);

            return gameplayAbility;
        }
    }
}