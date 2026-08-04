using NUnit.Framework;
using System.Threading.Tasks;

namespace GAS.Tests
{
    public sealed class InstantDamageAbilityScenarioTests
    {
        [Test]
        public async Task ActivateAbility_WithCostAndCooldown_AppliesDamageOnce()
        {
            using AbilitySystemTestEnvironment environment = new();

            AbilitySystemComponent source =
                environment.CreateAbilitySystem(
                    "Source");

            AbilitySystemComponent target =
                environment.CreateAbilitySystem(
                    "Target");

            AttributeName health =
                environment.CreateAttributeName(
                    "Health");

            AttributeName mana =
                environment.CreateAttributeName(
                    "Mana");

            Attribute targetHealth =
                environment.AddAttribute(
                    target,
                    health,
                    100f);

            Attribute sourceMana =
                environment.AddAttribute(
                    source,
                    mana,
                    25f);

            DirectActorTargetActor targetActorPrefab =
                environment.CreateDirectActorTargetActorPrefab(
                    "TA_DirectActor",
                    target.AbilityActorInfo.OwnerActor);

            GameplayAbilitySO ability =
                InstantAdditiveAbilityTestFactory.Create(
                    environment,
                    targetActorPrefab,
                    health,
                    -10f,
                    mana,
                    -25f,
                    0.5f);

            GameplayAbilitySpecHandle abilityHandle =
                source.GiveAbility(
                    new GameplayAbilitySpec(
                        ability,
                        1));

            bool firstActivationSucceeded =
                await source.TryActivateAbility(
                    abilityHandle);

            bool cooldownActivationSucceeded =
                await source.TryActivateAbility(
                    abilityHandle);

            await Task.Delay(
                600);

            bool insufficientCostActivationSucceeded =
                await source.TryActivateAbility(
                    abilityHandle);

            Assert.That(
                firstActivationSucceeded,
                Is.True);

            Assert.That(
                cooldownActivationSucceeded,
                Is.False);

            Assert.That(
                insufficientCostActivationSucceeded,
                Is.False);

            Assert.That(
                targetHealth.BaseValue,
                Is.EqualTo(
                    90f));

            Assert.That(
                sourceMana.BaseValue,
                Is.EqualTo(
                    0f));
        }
    }
}