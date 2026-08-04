using System;
using UnityObject = UnityEngine.Object;

namespace GAS
{
    public class AbilityTask_WaitTargetData :
        AbilityTask
    {
        private readonly DisposableGroup m_Subscriptions =
            new();

        private readonly DisposableEvent<
            GameplayAbilityTargetDataHandle> m_ValidDataDelegate = new();

        private readonly DisposableEvent<
            GameplayAbilityTargetDataHandle> m_CancelledDelegate = new();

        public string TaskInstanceName
        {
            get;
        }

        public GameplayTargetingConfirmation ConfirmationType
        {
            get;
        }

        public GameplayAbilityTargetActor TargetActor
        {
            get;
        }

        protected AbilityTask_WaitTargetData(
            GameplayAbility owningAbility,
            string taskInstanceName,
            GameplayTargetingConfirmation confirmationType,
            GameplayAbilityTargetActor targetActor)
            : base(
                owningAbility)
        {
            if (targetActor == null)
            {
                throw new ArgumentNullException(
                    nameof(targetActor));
            }

            TaskInstanceName = taskInstanceName;
            ConfirmationType = confirmationType;
            TargetActor = targetActor;
        }

        /// <summary>
        /// Registers a valid-target-data handler for the lifetime of this task.
        /// </summary>
        public IDisposable RegisterValidDataDelegate(
            Action<GameplayAbilityTargetDataHandle> handler)
        {
            IDisposable subscription =
                m_ValidDataDelegate.Subscribe(
                    handler);

            m_Subscriptions.Add(
                subscription);

            return subscription;
        }

        /// <summary>
        /// Registers a target-data cancellation handler for the lifetime of this task.
        /// </summary>
        public IDisposable RegisterCancelledDelegate(
            Action<GameplayAbilityTargetDataHandle> handler)
        {
            IDisposable subscription =
                m_CancelledDelegate.Subscribe(handler);

            m_Subscriptions.Add(subscription);

            return subscription;
        }

        /// <summary>
        /// Creates a task that spawns a target actor prefab and waits for its target data.
        /// </summary>
        public static AbilityTask_WaitTargetData WaitTargetData(
            GameplayAbility owningAbility,
            string taskInstanceName,
            GameplayTargetingConfirmation confirmationType,
            GameplayAbilityTargetActor targetActorPrefab)
        {
            if (targetActorPrefab == null)
            {
                throw new ArgumentNullException(
                    nameof(targetActorPrefab));
            }

            GameplayAbilityTargetActor targetActor =
                UnityObject.Instantiate(
                    targetActorPrefab);

            return new AbilityTask_WaitTargetData(
                owningAbility,
                taskInstanceName,
                confirmationType,
                targetActor);
        }

        /// <summary>
        /// Creates a task that waits for an existing target actor to produce target data.
        /// </summary>
        public static AbilityTask_WaitTargetData
            WaitTargetDataUsingActor(
                GameplayAbility owningAbility,
                string taskInstanceName,
                GameplayTargetingConfirmation confirmationType,
                GameplayAbilityTargetActor targetActor)
        {
            return new AbilityTask_WaitTargetData(
                owningAbility,
                taskInstanceName,
                confirmationType,
                targetActor);
        }

        /// <summary>
        /// Starts local targeting or waits for replicated target data according to the execution role.
        /// </summary>
        protected override void Activate()
        {
            if (ShouldWaitForReplicatedTargetData())
            {
                RegisterReplicatedTargetDataCallbacks();

                AbilitySystemComponent.CallReplicatedTargetDataDelegatesIfSet(
                    Ability.CurrentSpecHandle,
                    Ability
                        .CurrentActivationInfo
                        .GetActivationPredictionKey());

                return;
            }

            if (TargetActor == null)
            {
                m_CancelledDelegate.Invoke(
                    new GameplayAbilityTargetDataHandle());

                EndTask();

                return;
            }

            RegisterTargetDataCallbacks();

            TargetActor.StartTargeting(
                Ability);

            if (
                ConfirmationType ==
                GameplayTargetingConfirmation.Instant)
            {
                TargetActor.ConfirmTargetingAndContinue();
            }
        }

        /// <summary>
        /// Registers target-data callbacks owned by this task.
        /// </summary>
        protected virtual void RegisterTargetDataCallbacks()
        {
            m_Subscriptions.Add(
                TargetActor.RegisterTargetDataReadyDelegate(
                    OnTargetDataReadyCallback));

            m_Subscriptions.Add(
                TargetActor.RegisterCanceledDelegate(
                    OnTargetDataCancelledCallback));
        }

        /// <summary>
        /// Registers callbacks for target data produced by a remotely controlled client.
        /// </summary>
        private void RegisterReplicatedTargetDataCallbacks()
        {
            PredictionKey predictionKey =
                Ability
                    .CurrentActivationInfo
                    .GetActivationPredictionKey();

            m_Subscriptions.Add(
                AbilitySystemComponent.AbilityTargetDataSetDelegate(
                    Ability.CurrentSpecHandle,
                    predictionKey,
                    OnReplicatedTargetDataReadyCallback));

            m_Subscriptions.Add(
                AbilitySystemComponent.AbilityTargetDataCancelledDelegate(
                    Ability.CurrentSpecHandle,
                    predictionKey,
                    OnReplicatedTargetDataCancelledCallback));
        }

        /// <summary>
        /// Returns whether authoritative execution must wait for client-produced target data.
        /// </summary>
        private bool ShouldWaitForReplicatedTargetData()
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
        /// Returns whether locally produced target data must be forwarded to the server.
        /// </summary>
        private bool ShouldReplicateTargetDataToServer()
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

        /// <summary>
        /// Cancels targeting or stops waiting for remotely produced target data.
        /// </summary>
        public override void ExternalCancel()
        {
            if (IsEnded)
            {
                return;
            }

            if (!IsActive)
            {
                EndTask();

                return;
            }

            if (ShouldWaitForReplicatedTargetData())
            {
                EndTask();

                return;
            }

            if (TargetActor == null)
            {
                EndTask();

                return;
            }

            TargetActor.CancelTargeting();
        }

        /// <summary>
        /// Releases target callbacks and destroys the task-owned target actor.
        /// </summary>
        protected override void OnDestroy(
            bool abilityEnded)
        {
            m_Subscriptions.Dispose();

            if (TargetActor != null)
            {
                UnityObject.Destroy(
                    TargetActor.gameObject);
            }

            base.OnDestroy(
                abilityEnded);
        }

        /// <summary>
        /// Replicates and forwards confirmed target data before completing the targeting task.
        /// </summary>
        protected virtual void OnTargetDataReadyCallback(
            GameplayAbilityTargetDataHandle targetData)
        {
            if (IsEnded)
            {
                return;
            }

            if (ShouldReplicateTargetDataToServer())
            {
                PredictionKey predictionKey =
                    Ability
                        .CurrentActivationInfo
                        .GetActivationPredictionKey();

                AbilitySystemComponent.CallServerSetReplicatedTargetData(
                    Ability.CurrentSpecHandle,
                    predictionKey,
                    targetData,
                    null,
                    predictionKey);
            }

            m_ValidDataDelegate.Invoke(
                targetData);

            if (
                ConfirmationType !=
                GameplayTargetingConfirmation.CustomMulti)
            {
                EndTask();
            }
        }

        /// <summary>
        /// Consumes confirmed replicated target data and forwards it to the owning ability.
        /// </summary>
        private void OnReplicatedTargetDataReadyCallback(
            GameplayAbilityTargetDataHandle targetData,
            GameplayTag _)
        {
            if (IsEnded)
            {
                return;
            }

            PredictionKey predictionKey =
                Ability
                    .CurrentActivationInfo
                    .GetActivationPredictionKey();

            AbilitySystemComponent.ConsumeClientReplicatedTargetData(
                Ability.CurrentSpecHandle,
                predictionKey);

            OnTargetDataReadyCallback(
                targetData);
        }

        /// <summary>
        /// Consumes replicated cancellation and forwards it to the owning ability.
        /// </summary>
        private void OnReplicatedTargetDataCancelledCallback()
        {
            if (IsEnded)
            {
                return;
            }

            PredictionKey predictionKey =
                Ability
                    .CurrentActivationInfo
                    .GetActivationPredictionKey();

            AbilitySystemComponent.ConsumeClientReplicatedTargetData(
                Ability.CurrentSpecHandle,
                predictionKey);

            OnTargetDataCancelledCallback(
                new GameplayAbilityTargetDataHandle());
        }

        /// <summary>
        /// Replicates and forwards target cancellation before terminating the targeting task.
        /// </summary>
        protected virtual void OnTargetDataCancelledCallback(
            GameplayAbilityTargetDataHandle targetData)
        {
            if (IsEnded)
            {
                return;
            }

            if (ShouldReplicateTargetDataToServer())
            {
                PredictionKey predictionKey =
                    Ability
                        .CurrentActivationInfo
                        .GetActivationPredictionKey();

                AbilitySystemComponent.ServerSetReplicatedTargetDataCancelled(
                    Ability.CurrentSpecHandle,
                    predictionKey,
                    predictionKey);
            }

            m_CancelledDelegate.Invoke(
                targetData);

            EndTask();
        }
    }
}