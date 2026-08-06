using System;
using UnityEngine;

namespace GAS
{
    public sealed class AbilityTask_WaitInputPress :
        AbilityTask
    {
        private readonly DisposableGroup m_Subscriptions =
            new();

        private readonly DisposableEvent<float>
            m_PressedDelegate = new();

        private readonly bool m_TestInitialState;

        private float m_StartTime;

        private AbilityTask_WaitInputPress(
            GameplayAbility owningAbility,
            bool testAlreadyPressed)
            : base(
                owningAbility)
        {
            m_TestInitialState =
                testAlreadyPressed;
        }

        /// <summary>
        /// Creates a task that waits for the owning ability input to be pressed.
        /// </summary>
        public static AbilityTask_WaitInputPress WaitInputPress(
            GameplayAbility owningAbility,
            bool testAlreadyPressed = false)
        {
            return new AbilityTask_WaitInputPress(
                owningAbility,
                testAlreadyPressed);
        }

        /// <summary>
        /// Registers a press callback for the lifetime of this task.
        /// </summary>
        public IDisposable RegisterPressedDelegate(
            Action<float> handler)
        {
            IDisposable subscription =
                m_PressedDelegate.Subscribe(
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
                    GameplayAbilityGenericReplicatedEvent.InputPressed,
                    Ability.CurrentSpecHandle,
                    predictionKey,
                    OnPressCallback));

            if (ShouldWaitForReplicatedEvent())
            {
                AbilitySystemComponent.CallReplicatedEventDelegateIfSet(
                    GameplayAbilityGenericReplicatedEvent.InputPressed,
                    Ability.CurrentSpecHandle,
                    predictionKey);

                return;
            }

            if (!m_TestInitialState)
            {
                return;
            }

            GameplayAbilitySpec abilitySpec =
                AbilitySystemComponent.FindAbilitySpecFromHandle(
                    Ability.CurrentSpecHandle);

            if (
                abilitySpec == null ||
                !abilitySpec.InputPressed)
            {
                return;
            }

            OnPressCallback();
        }

        /// <summary>
        /// Releases event subscriptions when this task or its owning ability ends.
        /// </summary>
        protected override void OnDestroy(
            bool abilityEnded)
        {
            m_Subscriptions.Dispose();
            m_PressedDelegate.Clear();

            base.OnDestroy(
                abilityEnded);
        }

        /// <summary>
        /// Replicates and consumes input press before completing this task.
        /// </summary>
        private void OnPressCallback()
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
                    GameplayAbilityGenericReplicatedEvent.InputPressed,
                    Ability.CurrentSpecHandle,
                    predictionKey,
                    predictionKey);
            }
            else if (ShouldWaitForReplicatedEvent())
            {
                AbilitySystemComponent.ConsumeGenericReplicatedEvent(
                    GameplayAbilityGenericReplicatedEvent.InputPressed,
                    Ability.CurrentSpecHandle,
                    predictionKey);
            }

            float timeWaited =
                Time.realtimeSinceStartup -
                m_StartTime;

            m_PressedDelegate.Invoke(
                timeWaited);

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