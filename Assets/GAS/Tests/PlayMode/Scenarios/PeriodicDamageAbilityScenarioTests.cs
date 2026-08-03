using NUnit.Framework;
using System.Threading.Tasks;

namespace GAS.Tests
{
    public sealed class PeriodicDamageAbilityScenarioTests
    {
        [Test]
        public async Task ActivateAbility_AppliesImmediateAndPeriodicDamage()
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

            Attribute targetHealth =
                environment.AddAttribute(
                    target,
                    health,
                    100f);

            GameplayAbilitySO_InstantAbility ability =
                PeriodicDamageAbilityTestFactory.Create(
                    environment,
                    health,
                    10f,
                    0.18f,
                    0.05f);

            GameplayAbilitySpecHandle abilityHandle =
                source.GiveAbility(
                    new GameplayAbilitySpec(
                        ability,
                        1));

            await source.TryActivateAbility(
                abilityHandle,
                target);

            await Task.Delay(
                300);

            Assert.That(
                targetHealth.BaseValue,
                Is.EqualTo(
                    60f));
        }
    }
}