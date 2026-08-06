using System;
using GAS;
using GAS.Common;
using RelicsOfTheFallen.Targeting;
using UnityEngine;

namespace RelicsOfTheFallen.Abilities
{
    /// <summary>
    /// Maintains a channeled activation against the target selected when the ability starts.
    /// </summary>
    [Serializable]
    public sealed class ChannelDamageAbility :
        CommonGameplayAbility
    {
        private IDisposable m_TargetDataSetSubscription;
        private IDisposable m_TargetDataCancelledSubscription;

        [field: SerializeField, Min(0.01f)]
        private float TickInterval
        {
            get; set;
        } = 0.25f;

        [field: SerializeField]
        private GameplayAbilityMontage ChannelMontage
        {
            get; set;
        }

        private GameplayAbilityTargetDataHandle m_ChannelTargetData;

        /// <summary>
        /// Creates a runtime channel ability instance preserving its configured gameplay data.
        /// </summary>
        public override GameplayAbility Instantiate(
            AbilitySystemComponent owner)
        {
            ChannelDamageAbility ability =
                (ChannelDamageAbility)base.Instantiate(
                    owner);

            ability.TickInterval =
                TickInterval;

            ability.ChannelMontage =
                ChannelMontage;

            return ability;
        }

        /// <summary>
        /// Acquires local target data or waits for target data produced by the owning client.
        /// </summary>
        protected override void ActivateAbility(
            GameplayAbilitySpecHandle handle,
            GameplayAbilityActorInfo actorInfo,
            GameplayAbilityActivationInfo activationInfo,
            GameplayEventData? triggerEventData)
        {
            base.ActivateAbility(
                handle,
                actorInfo,
                activationInfo,
                triggerEventData);

            if (
                ShouldWaitForReplicatedTargetData(
                    actorInfo,
                    activationInfo))
            {
                RegisterReplicatedTargetDataCallbacks(
                    actorInfo.AbilitySystemComponent,
                    handle,
                    activationInfo);

                return;
            }

            GameplayAbilityTargetDataHandle targetData =
                CreateTargetData(
                    actorInfo);

            if (targetData == null)
            {
                HandleTargetDataCancelled();
                return;
            }

            HandleTargetDataReady(
                targetData);
        }

        /// <summary>
        /// Releases channel state and target-data callbacks before ending the activation.
        /// </summary>
        public override void EndAbility(
            GameplayAbilitySpecHandle handle,
            GameplayAbilityActorInfo actorInfo,
            GameplayAbilityActivationInfo activationInfo,
            bool replicateEndAbility,
            bool wasCancelled)
        {
            DisposeTargetDataSubscriptions();

            m_ChannelTargetData =
                null;

            base.EndAbility(
                handle,
                actorInfo,
                activationInfo,
                replicateEndAbility,
                wasCancelled);
        }

        /// <summary>
        /// Creates actor-array target data from the avatar's currently selected target.
        /// </summary>
        private static GameplayAbilityTargetDataHandle CreateTargetData(
            GameplayAbilityActorInfo actorInfo)
        {
            TargetingController targeting =
                actorInfo.AvatarActor.GetComponentInChildren<
                    TargetingController>();

            if (targeting == null)
            {
                return null;
            }

            ITargetable currentTarget =
                targeting.CurrentTarget;

            if (
                currentTarget == null ||
                currentTarget.TargetActor == null)
            {
                return null;
            }

            GameplayAbilityTargetData_ActorArray actorArray =
                new(
                    currentTarget.TargetActor);

            return new GameplayAbilityTargetDataHandle(
                actorArray);
        }

        /// <summary>
        /// Registers callbacks used by authority while awaiting client-produced target data.
        /// </summary>
        private void RegisterReplicatedTargetDataCallbacks(
            AbilitySystemComponent abilitySystem,
            GameplayAbilitySpecHandle handle,
            GameplayAbilityActivationInfo activationInfo)
        {
            PredictionKey predictionKey =
                activationInfo.GetActivationPredictionKey();

            m_TargetDataSetSubscription =
                abilitySystem.AbilityTargetDataSetDelegate(
                    handle,
                    predictionKey,
                    HandleReplicatedTargetDataReady);

            m_TargetDataCancelledSubscription =
                abilitySystem.AbilityTargetDataCancelledDelegate(
                    handle,
                    predictionKey,
                    HandleTargetDataCancelled);

            abilitySystem.CallReplicatedTargetDataDelegatesIfSet(
                handle,
                predictionKey);
        }

        /// <summary>
        /// Forwards replicated target data into the shared channel-start pipeline.
        /// </summary>
        private void HandleReplicatedTargetDataReady(
            GameplayAbilityTargetDataHandle targetData,
            GameplayTag _)
        {
            HandleTargetDataReady(
                targetData);
        }

        /// <summary>
        /// Commits the ability and starts the authority-driven channel lifecycle.
        /// </summary>
        private void HandleTargetDataReady(
            GameplayAbilityTargetDataHandle targetData)
        {
            DisposeTargetDataSubscriptions();

            AbilitySystemComponent abilitySystem =
                CurrentActorInfo.AbilitySystemComponent;

            PredictionKey predictionKey =
                CurrentActivationInfo.GetActivationPredictionKey();

            if (
                ShouldReplicateTargetDataToServer(
                    CurrentActorInfo,
                    CurrentActivationInfo))
            {
                abilitySystem.CallServerSetReplicatedTargetData(
                    CurrentSpecHandle,
                    predictionKey,
                    targetData,
                    null,
                    predictionKey);
            }

            if (
                !CommitAbility(
                    CurrentSpecHandle,
                    CurrentActorInfo,
                    CurrentActivationInfo))
            {
                EndAbility(
                    CurrentSpecHandle,
                    CurrentActorInfo,
                    CurrentActivationInfo,
                    true,
                    true);

                return;
            }

            if (
                ShouldWaitForReplicatedTargetData(
                    CurrentActorInfo,
                    CurrentActivationInfo))
            {
                abilitySystem.ConsumeClientReplicatedTargetData(
                    CurrentSpecHandle,
                    predictionKey);
            }

            m_ChannelTargetData =
                targetData;

            AbilityTask_WaitInputRelease inputReleaseTask =
                AbilityTask_WaitInputRelease.WaitInputRelease(
                    this,
                    true);

            inputReleaseTask.RegisterReleasedDelegate(
                HandleInputReleased);

            inputReleaseTask.ReadyForActivation();

            if (!IsActive)
            {
                return;
            }

            StartChannelMontage();

            if (!IsActive)
            {
                return;
            }

            ExecuteDamageTick();
            ScheduleNextDamageTick();
        }

        /// <summary>
        /// Starts the looped montage that visually represents the active channel.
        /// </summary>
        private void StartChannelMontage()
        {
            AbilityTask_PlayMontageAndWait montageTask =
                AbilityTask_PlayMontageAndWait
                    .CreatePlayMontageAndWaitProxy(
                        this,
                        nameof(ChannelDamageAbility),
                        ChannelMontage,
                        stopWhenAbilityEnds: true);

            montageTask.RegisterCompletedDelegate(
                HandleChannelMontageCompleted);

            montageTask.RegisterInterruptedDelegate(
                HandleChannelMontageCancelled);

            montageTask.RegisterCancelledDelegate(
                HandleChannelMontageCancelled);

            montageTask.ReadyForActivation();
        }

        /// <summary>
        /// Ends the channel normally if its visual montage completes unexpectedly.
        /// </summary>
        private void HandleChannelMontageCompleted()
        {
            if (!IsActive)
            {
                return;
            }

            EndAbility(
                CurrentSpecHandle,
                CurrentActorInfo,
                CurrentActivationInfo,
                true,
                false);
        }

        /// <summary>
        /// Cancels the channel when its visual montage is interrupted or cannot start.
        /// </summary>
        private void HandleChannelMontageCancelled()
        {
            if (!IsActive)
            {
                return;
            }

            EndAbility(
                CurrentSpecHandle,
                CurrentActorInfo,
                CurrentActivationInfo,
                true,
                true);
        }

        /// <summary>
        /// Cancels activation and forwards locally produced cancellation when required.
        /// </summary>
        private void HandleTargetDataCancelled()
        {
            DisposeTargetDataSubscriptions();

            PredictionKey predictionKey =
                CurrentActivationInfo.GetActivationPredictionKey();

            if (
                ShouldReplicateTargetDataToServer(
                    CurrentActorInfo,
                    CurrentActivationInfo))
            {
                CurrentActorInfo
                    .AbilitySystemComponent
                    .ServerSetReplicatedTargetDataCancelled(
                        CurrentSpecHandle,
                        predictionKey,
                        predictionKey);
            }

            EndAbility(
                CurrentSpecHandle,
                CurrentActorInfo,
                CurrentActivationInfo,
                true,
                true);
        }

        /// <summary>
        /// Ends the channel after the owning ability input is released.
        /// </summary>
        private void HandleInputReleased(
            float _)
        {
            EndAbility(
                CurrentSpecHandle,
                CurrentActorInfo,
                CurrentActivationInfo,
                true,
                false);
        }

        /// <summary>
        /// Applies one configured Instant gameplay effect tick on authority.
        /// </summary>
        private void ExecuteDamageTick()
        {
            if (
                !IsActive ||
                !CurrentActorInfo.IsNetAuthority() ||
                m_ChannelTargetData == null)
            {
                return;
            }

            ApplyGameplayEffects(
                CurrentActorInfo.AbilitySystemComponent,
                CurrentActivationInfo,
                m_ChannelTargetData);
        }

        /// <summary>
        /// Schedules the next authority-side channel damage tick.
        /// </summary>
        private void ScheduleNextDamageTick()
        {
            if (
                !IsActive ||
                !CurrentActorInfo.IsNetAuthority())
            {
                return;
            }

            AbilityTask_WaitDelay delayTask =
                AbilityTask_WaitDelay.WaitDelay(
                    this,
                    Mathf.Max(
                        TickInterval,
                        0.01f));

            delayTask.RegisterFinishDelegate(
                HandleDamageDelayFinished);

            delayTask.ReadyForActivation();
        }

        /// <summary>
        /// Executes one channel tick and schedules the following delay.
        /// </summary>
        private void HandleDamageDelayFinished()
        {
            if (!IsActive)
            {
                return;
            }

            ExecuteDamageTick();
            ScheduleNextDamageTick();
        }

        /// <summary>
        /// Returns whether authority must wait for target data produced by a remote client.
        /// </summary>
        private static bool ShouldWaitForReplicatedTargetData(
            GameplayAbilityActorInfo actorInfo,
            GameplayAbilityActivationInfo activationInfo)
        {
            PredictionKey predictionKey =
                activationInfo.GetActivationPredictionKey();

            return
                actorInfo.IsNetAuthority() &&
                !actorInfo.IsLocallyControlled() &&
                predictionKey.IsValid;
        }

        /// <summary>
        /// Returns whether locally produced target data must be forwarded to authority.
        /// </summary>
        private static bool ShouldReplicateTargetDataToServer(
            GameplayAbilityActorInfo actorInfo,
            GameplayAbilityActivationInfo activationInfo)
        {
            PredictionKey predictionKey =
                activationInfo.GetActivationPredictionKey();

            return
                !actorInfo.IsNetAuthority() &&
                actorInfo.IsLocallyControlled() &&
                predictionKey.IsValid;
        }

        /// <summary>
        /// Releases callbacks registered while authority awaited replicated target data.
        /// </summary>
        private void DisposeTargetDataSubscriptions()
        {
            if (m_TargetDataSetSubscription != null)
            {
                m_TargetDataSetSubscription.Dispose();
                m_TargetDataSetSubscription = null;
            }

            if (m_TargetDataCancelledSubscription != null)
            {
                m_TargetDataCancelledSubscription.Dispose();
                m_TargetDataCancelledSubscription = null;
            }
        }
    }
}