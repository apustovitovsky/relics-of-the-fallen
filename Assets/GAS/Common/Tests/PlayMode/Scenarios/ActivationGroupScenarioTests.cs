using System.Threading.Tasks;
using NUnit.Framework;

namespace GAS.Common.Tests
{
    public sealed class ActivationGroupScenarioTests
    {
        /// <summary>
        /// Verifies that a blocking ability cancels and blocks other exclusive abilities.
        /// </summary>
        [Test]
        public async Task BlockingAbility_CancelsAndBlocksExclusiveAbilities()
        {
            using CommonAbilitySystemTestEnvironment environment =
                new();

            CommonAbilitySystemComponent abilitySystem =
                environment.CreateAbilitySystem(
                    "AbilitySystem");

            GameplayAbilitySpec replaceableSpec =
                GrantAbility(
                    environment,
                    abilitySystem,
                    "GA_Replaceable",
                    GameplayAbilityActivationGroup.ExclusiveReplaceable);

            GameplayAbilitySpec blockingSpec =
                GrantAbility(
                    environment,
                    abilitySystem,
                    "GA_Blocking",
                    GameplayAbilityActivationGroup.ExclusiveBlocking);

            GameplayAbilitySpec secondBlockingSpec =
                GrantAbility(
                    environment,
                    abilitySystem,
                    "GA_SecondBlocking",
                    GameplayAbilityActivationGroup.ExclusiveBlocking);

            bool replaceableActivated =
                await abilitySystem.TryActivateAbility(
                    replaceableSpec.Handle);

            bool blockingActivated =
                await abilitySystem.TryActivateAbility(
                    blockingSpec.Handle);

            GameplayTagContainer replaceableFailureReason =
                new();

            bool replaceableCanActivate =
                GetAbility(
                    replaceableSpec)
                    .CanActivateAbility(
                        replaceableSpec.Handle,
                        abilitySystem.AbilityActorInfo,
                        null,
                        null,
                        replaceableFailureReason);

            bool blockedReplaceableActivation =
                await abilitySystem.TryActivateAbility(
                    replaceableSpec.Handle);

            GameplayTagContainer blockingFailureReason =
                new();

            bool secondBlockingCanActivate =
                GetAbility(
                    secondBlockingSpec)
                    .CanActivateAbility(
                        secondBlockingSpec.Handle,
                        abilitySystem.AbilityActorInfo,
                        null,
                        null,
                        blockingFailureReason);

            bool secondBlockingActivated =
                await abilitySystem.TryActivateAbility(
                    secondBlockingSpec.Handle);

            Assert.That(
                replaceableActivated,
                Is.True);

            Assert.That(
                blockingActivated,
                Is.True);

            Assert.That(
                replaceableSpec.IsActive(),
                Is.False);

            Assert.That(
                replaceableCanActivate,
                Is.False);

            Assert.That(
                blockedReplaceableActivation,
                Is.False);

            Assert.That(
                secondBlockingCanActivate,
                Is.False);

            Assert.That(
                secondBlockingActivated,
                Is.False);

            Assert.That(
                blockingSpec.IsActive(),
                Is.True);

            Assert.That(
                replaceableFailureReason.HasTagExact(
                    CommonGameplayTags.ActivateFailActivationGroupTag),
                Is.True);

            Assert.That(
                blockingFailureReason.HasTagExact(
                    CommonGameplayTags.ActivateFailActivationGroupTag),
                Is.True);

            EndAbility(
                abilitySystem,
                blockingSpec);

            bool replaceableReactivated =
                await abilitySystem.TryActivateAbility(
                    replaceableSpec.Handle);

            Assert.That(
                replaceableReactivated,
                Is.True);

            Assert.That(
                replaceableSpec.IsActive(),
                Is.True);

            EndAbility(
                abilitySystem,
                replaceableSpec);
        }

        /// <summary>
        /// Verifies that a replaceable ability replaces another active replaceable ability.
        /// </summary>
        [Test]
        public async Task ReplaceableAbility_ReplacesActiveReplaceable()
        {
            using CommonAbilitySystemTestEnvironment environment =
                new();

            CommonAbilitySystemComponent abilitySystem =
                environment.CreateAbilitySystem(
                    "AbilitySystem");

            GameplayAbilitySpec firstSpec =
                GrantAbility(
                    environment,
                    abilitySystem,
                    "GA_FirstReplaceable",
                    GameplayAbilityActivationGroup.ExclusiveReplaceable);

            GameplayAbilitySpec secondSpec =
                GrantAbility(
                    environment,
                    abilitySystem,
                    "GA_SecondReplaceable",
                    GameplayAbilityActivationGroup.ExclusiveReplaceable);

            bool firstActivated =
                await abilitySystem.TryActivateAbility(
                    firstSpec.Handle);

            bool secondActivated =
                await abilitySystem.TryActivateAbility(
                    secondSpec.Handle);

            Assert.That(
                firstActivated,
                Is.True);

            Assert.That(
                secondActivated,
                Is.True);

            Assert.That(
                firstSpec.IsActive(),
                Is.False);

            Assert.That(
                secondSpec.IsActive(),
                Is.True);

            EndAbility(
                abilitySystem,
                secondSpec);
        }

        /// <summary>
        /// Verifies that independent abilities coexist with an active blocking ability.
        /// </summary>
        [Test]
        public async Task IndependentAbility_CoexistsWithBlockingAbility()
        {
            using CommonAbilitySystemTestEnvironment environment =
                new();

            CommonAbilitySystemComponent abilitySystem =
                environment.CreateAbilitySystem(
                    "AbilitySystem");

            GameplayAbilitySpec firstIndependentSpec =
                GrantAbility(
                    environment,
                    abilitySystem,
                    "GA_FirstIndependent",
                    GameplayAbilityActivationGroup.Independent);

            GameplayAbilitySpec blockingSpec =
                GrantAbility(
                    environment,
                    abilitySystem,
                    "GA_Blocking",
                    GameplayAbilityActivationGroup.ExclusiveBlocking);

            GameplayAbilitySpec secondIndependentSpec =
                GrantAbility(
                    environment,
                    abilitySystem,
                    "GA_SecondIndependent",
                    GameplayAbilityActivationGroup.Independent);

            bool firstIndependentActivated =
                await abilitySystem.TryActivateAbility(
                    firstIndependentSpec.Handle);

            bool blockingActivated =
                await abilitySystem.TryActivateAbility(
                    blockingSpec.Handle);

            bool secondIndependentActivated =
                await abilitySystem.TryActivateAbility(
                    secondIndependentSpec.Handle);

            Assert.That(
                firstIndependentActivated,
                Is.True);

            Assert.That(
                blockingActivated,
                Is.True);

            Assert.That(
                secondIndependentActivated,
                Is.True);

            Assert.That(
                firstIndependentSpec.IsActive(),
                Is.True);

            Assert.That(
                blockingSpec.IsActive(),
                Is.True);

            Assert.That(
                secondIndependentSpec.IsActive(),
                Is.True);

            EndAbility(
                abilitySystem,
                secondIndependentSpec);

            EndAbility(
                abilitySystem,
                blockingSpec);

            EndAbility(
                abilitySystem,
                firstIndependentSpec);
        }

        private static GameplayAbilitySpec GrantAbility(
            CommonAbilitySystemTestEnvironment environment,
            CommonAbilitySystemComponent abilitySystem,
            string name,
            GameplayAbilityActivationGroup activationGroup)
        {
            GameplayAbilitySO definition =
                environment.CreateAbility(
                    name,
                    activationGroup);

            GameplayAbilitySpec spec =
                new(
                    definition,
                    1);

            abilitySystem.GiveAbility(
                spec);

            return spec;
        }

        private static ActivationGroupTestAbility GetAbility(
            GameplayAbilitySpec spec)
        {
            return
                (ActivationGroupTestAbility)
                spec.PrimaryInstance;
        }

        private static void EndAbility(
            CommonAbilitySystemComponent abilitySystem,
            GameplayAbilitySpec spec)
        {
            if (!spec.IsActive())
            {
                return;
            }

            ActivationGroupTestAbility ability =
                GetAbility(
                    spec);

            ability.EndAbility(
                spec.Handle,
                abilitySystem.AbilityActorInfo,
                ability.CurrentActivationInfo,
                false,
                false);
        }
    }
}