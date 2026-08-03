using NUnit.Framework;
using System.Threading.Tasks;

namespace GAS.Tests
{
    public sealed class InstantDamageAbilityScenarioTests
    {
        [Test]
        public async Task ActivateAbility_ReducesTargetHealth()
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
                InstantAdditiveAbilityTestFactory.Create(
                    environment,
                    health,
                    -10f);

            GameplayAbilitySpecHandle abilityHandle =
                source.GiveAbility(
                    new GameplayAbilitySpec(
                        ability,
                        1));

            await source.TryActivateAbility(
                abilityHandle,
                target);

            Assert.That(
                targetHealth.BaseValue,
                Is.EqualTo(
                    90f));
        }
    }
}