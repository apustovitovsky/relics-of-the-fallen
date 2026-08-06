using System;
using UnityEngine;

namespace GAS
{
    public sealed class AbilityTask_WaitInputRelease :
        AbilityTask
    {
        private readonly DisposableGroup m_Subscriptions =
            new();

        private readonly DisposableEvent<float>
            m_ReleasedDelegate = new();

        private float m_StartTime;

        public bool TestAlreadyReleased
        {
            get;
        }

        private AbilityTask_WaitInputRelease(
            GameplayAbility owningAbility,
            bool testAlreadyReleased)
            : base(
                owningAbility)
        {
            TestAlreadyReleased =
                testAlreadyReleased;
        }

        /// <summary>
        /// Creates a task that waits for the owning ability input to be released.
        /// </summary>
        public static AbilityTask_WaitInputRelease WaitInputRelease(
            GameplayAbility owningAbility,
            bool testAlreadyReleased = false)
        {
            return new AbilityTask_WaitInputRelease(
                owningAbility,
                testAlreadyReleased);
        }

        /// <summary>
        /// Registers a release callback for the lifetime of this task.
        /// </summary>
        public IDisposable RegisterReleasedDelegate(
            Action<float> handler)
        {
            IDisposable subscription =
                m_ReleasedDelegate.Subscribe(
                    handler);

            m_Subscriptions.Add(
                subscription);

            return subscription;
        }

        /// <summary>
        /// Registers the replicated event callback and optionally tests the current input state.
        /// </summary>
        protected override void Activate()
        {
            m_StartTime =
                Time.realtimeSinceStartup;

            PredictionKey predictionKey =
                Ability
                    .CurrentActivationInfo
                    .GetActivationPredictionKey();

            m_Subscriptions.Add(
                AbilitySystemComponent.AbilityReplicatedEventDelegate(
                    GameplayAbilityGenericReplicatedEvent.InputReleased,
                    Ability.CurrentSpecHandle,
                    predictionKey,
                    OnReleaseCallback));

            if (ShouldWaitForReplicatedEvent())
            {
                AbilitySystemComponent.CallReplicatedEventDelegateIfSet(
                    GameplayAbilityGenericReplicatedEvent.InputReleased,
                    Ability.CurrentSpecHandle,
                    predictionKey);

                return;
            }

            if (!TestAlreadyReleased)
            {
                return;
            }

            GameplayAbilitySpec abilitySpec =
                AbilitySystemComponent.FindAbilitySpecFromHandle(
                    Ability.CurrentSpecHandle);

            if (
                abilitySpec == null ||
                abilitySpec.InputPressed)
            {
                return;
            }

            OnReleaseCallback();
        }

        /// <summary>
        /// Releases event subscriptions when this task or its owning ability ends.
        /// </summary>
        protected override void OnDestroy(
            bool abilityEnded)
        {
            m_Subscriptions.Dispose();
            m_ReleasedDelegate.Clear();

            base.OnDestroy(
                abilityEnded);
        }

        /// <summary>
        /// Replicates and consumes input release before completing this task.
        /// </summary>
        private void OnReleaseCallback()
        {
            if (IsEnded)
            {
                return;
            }

            PredictionKey predictionKey =
                Ability
                    .CurrentActivationInfo
                    .GetActivationPredictionKey();

            if (ShouldReplicateEventToServer())
            {
                AbilitySystemComponent.ServerSetReplicatedEvent(
                    GameplayAbilityGenericReplicatedEvent.InputReleased,
                    Ability.CurrentSpecHandle,
                    predictionKey,
                    predictionKey);
            }
            else if (ShouldWaitForReplicatedEvent())
            {
                AbilitySystemComponent.ConsumeGenericReplicatedEvent(
                    GameplayAbilityGenericReplicatedEvent.InputReleased,
                    Ability.CurrentSpecHandle,
                    predictionKey);
            }

            float timeHeld =
                Time.realtimeSinceStartup -
                m_StartTime;

            m_ReleasedDelegate.Invoke(
                timeHeld);

            EndTask();
        }

        /// <summary>
        /// Returns whether authoritative execution must wait for a remote input event.
        /// </summary>
        private bool ShouldWaitForReplicatedEvent()
        {
            GameplayAbilityActorInfo actorInfo =
                AbilitySystemComponent.AbilityActorInfo;

            PredictionKey predictionKey =
                Ability
                    .CurrentActivationInfo
                    .GetActivationPredictionKey();

            return
                actorInfo.IsNetAuthority() &&
                !actorInfo.IsLocallyControlled() &&
                predictionKey.IsValid;
        }

        /// <summary>
        /// Returns whether the locally observed event must be forwarded to the server.
        /// </summary>
        private bool ShouldReplicateEventToServer()
        {
            GameplayAbilityActorInfo actorInfo =
                AbilitySystemComponent.AbilityActorInfo;

            PredictionKey predictionKey =
                Ability
                    .CurrentActivationInfo
                    .GetActivationPredictionKey();

            return
                !actorInfo.IsNetAuthority() &&
                actorInfo.IsLocallyControlled() &&
                predictionKey.IsValid;
        }
    }
}