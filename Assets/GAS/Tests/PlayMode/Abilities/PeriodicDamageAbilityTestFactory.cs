namespace GAS.Tests
{
    internal static class PeriodicDamageAbilityTestFactory
    {
        /// <summary>
        /// Creates an ability that applies periodic additive damage for a fixed duration.
        /// </summary>
        public static GameplayAbilitySO Create(
            AbilitySystemTestEnvironment environment,
            GameplayAbilityTargetActor targetActorPrefab,
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

            GameplayAbilitySO gameplayAbility =
                environment.CreateScriptableObject<GameplayAbilityTestAsset>(
                    "GA_PeriodicDamage");

            PeriodicDamageAbility periodicDamageAbility =
                new();

            periodicDamageAbility.SetTargetActorPrefab(
                targetActorPrefab);

            periodicDamageAbility.abilityTags.initialized = true;

            periodicDamageAbility.effectsSO.Add(
                gameplayEffect);

            gameplayAbility.ga =
                periodicDamageAbility;

            return gameplayAbility;
        }
    }
}