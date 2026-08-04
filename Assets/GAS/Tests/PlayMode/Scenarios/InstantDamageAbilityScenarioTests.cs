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

            DirectActorTargetActor targetActorPrefab =
                environment.CreateDirectActorTargetActorPrefab(
                    "TA_DirectActor",
                    target.AbilityActorInfo.OwnerActor);

            GameplayAbilitySO ability =
                InstantAdditiveAbilityTestFactory.Create(
                    environment,
                    targetActorPrefab,
                    health,
                    -10f);

            GameplayAbilitySpecHandle abilityHandle =
                source.GiveAbility(
                    new GameplayAbilitySpec(
                        ability,
                        1));

            await source.TryActivateAbility(
                abilityHandle);

            Assert.That(
                targetHealth.BaseValue,
                Is.EqualTo(
                    90f));
        }
    }
}