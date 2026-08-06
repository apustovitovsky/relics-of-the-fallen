using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace GAS.Tests
{
    public sealed class ActiveGameplayEffectDurationScenarioTests
    {
        /// <summary>
        /// Verifies that applying a duration effect publishes its spec, handle, and timing data.
        /// </summary>
        [Test]
        public void ApplyDurationEffect_PublishesEffectAndTimingData()
        {
            using AbilitySystemTestEnvironment environment =
                new();

            AbilitySystemComponent abilitySystem =
                environment.CreateAbilitySystem(
                    "AbilitySystem");

            GameplayEffectSO definition =
                environment.CreateScriptableObject<GameplayEffectSO>(
                    "GE_Duration");

            definition.ge =
                new GameplayEffect()
                {
                    name = "Duration",
                    durationType =
                        GameplayEffectDurationType.Duration,
                    durationValue = 1f
                };

            definition.ge.gameplayEffectTags.initialized =
                true;

            AbilitySystemComponent callbackTarget =
                null;

            GameplayEffectSpec callbackSpec =
                null;

            ActiveGameplayEffectHandle callbackHandle =
                default;

            void HandleGameplayEffectAdded(
                AbilitySystemComponent target,
                GameplayEffectSpec appliedSpec,
                ActiveGameplayEffectHandle activeHandle)
            {
                callbackTarget =
                    target;

                callbackSpec =
                    appliedSpec;

                callbackHandle =
                    activeHandle;
            }

            using IDisposable subscription =
                abilitySystem
                    .RegisterActiveGameplayEffectAddedDelegateToSelf(
                        HandleGameplayEffectAdded);

            GameplayEffectContextHandle context =
                abilitySystem.MakeEffectContext();

            GameplayEffectSpec outgoingSpec =
                abilitySystem.MakeOutgoingSpec(
                    definition,
                    1f,
                    context);

            ActiveGameplayEffectHandle appliedHandle =
                abilitySystem.ApplyGameplayEffectSpecToSelf(
                    outgoingSpec);

            GameplayTagContainer emptyTags =
                new();

            GameplayEffectQuery query =
                GameplayEffectQuery.MakeQuery_MatchAnyOwningTags(
                    emptyTags);

            List<(
                float TimeRemaining,
                float Duration)> effectTimes =
                    abilitySystem
                        .GetActiveEffectsTimeRemainingAndDuration(
                            query);

            Assert.That(
                callbackTarget,
                Is.SameAs(
                    abilitySystem));

            Assert.That(
                callbackSpec,
                Is.Not.Null);

            Assert.That(
                callbackSpec.Duration,
                Is.EqualTo(
                    1f));

            Assert.That(
                callbackHandle,
                Is.EqualTo(
                    appliedHandle));

            Assert.That(
                effectTimes,
                Has.Count.EqualTo(
                    1));

            Assert.That(
                effectTimes[0].Duration,
                Is.EqualTo(
                    1f));

            Assert.That(
                effectTimes[0].TimeRemaining,
                Is.GreaterThan(
                    0f).And.LessThanOrEqualTo(
                    1f));

            Assert.That(
                abilitySystem.RemoveActiveGameplayEffect(
                    appliedHandle),
                Is.True);
        }
    }
}