namespace GAS.Tests
{
    internal static class PeriodicDamageAbilityTestFactory
    {
        /// <summary>
        /// Creates an ability that applies periodic additive damage for a fixed duration.
        /// </summary>
        public static GameplayAbilitySO_InstantAbility Create(
            AbilitySystemTestEnvironment environment,
            AttributeName healthAttribute,
            float damagePerPeriod,
            float duration,
            float period)
        {
            GameplayEffectSO gameplayEffect =
                environment.CreateScriptableObject<GameplayEffectSO>(
                    "GE_PeriodicDamage");

            gameplayEffect.ge =
                new GameplayEffect()
                {
                    name = "PeriodicDamage",
                    durationType =
                        GameplayEffectDurationType.Duration,
                    durationValue = duration,
                    period = period,
                    ExecutePeriodicEffectOnApplication = true
                };

            gameplayEffect.ge.gameplayEffectTags.initialized = true;

            gameplayEffect.ge.modifiers.Add(
                new BasicModifier()
                {
                    attributeName = healthAttribute,
                    value = -damagePerPeriod
                });

            GameplayAbilitySO_InstantAbility gameplayAbility =
                environment.CreateScriptableObject<GameplayAbilitySO_InstantAbility>(
                    "GA_PeriodicDamage");

            gameplayAbility.ga =
                new PeriodicDamageAbility();

            gameplayAbility.ga.abilityTags.initialized = true;

            gameplayAbility.ga.effectsSO.Add(
                gameplayEffect);

            return gameplayAbility;
        }
    }
}