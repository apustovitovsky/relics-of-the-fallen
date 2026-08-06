using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace GAS
{
    public sealed class AbilityTask_PlayMontageAndWait : AbilityTask
    {
        private readonly GameplayAbilityMontage m_MontageToPlay;
        private readonly float m_Rate;
        private readonly string m_StartSectionName;
        private readonly bool m_StopWhenAbilityEnds;
        private readonly float m_StartTimeSeconds;

        private readonly DisposableEvent m_BlendedInDelegate =
            new();

        private readonly DisposableEvent m_BlendOutDelegate =
            new();

        private readonly DisposableEvent m_CompletedDelegate =
            new();

        private readonly DisposableEvent m_InterruptedDelegate =
            new();

        private readonly DisposableEvent m_CancelledDelegate =
            new();

        private readonly DisposableGroup m_Subscriptions =
            new();

        private CancellationTokenSource m_MonitorCancellationSource;

        public string TaskInstanceName
        {
            get;
        }

        private AbilityTask_PlayMontageAndWait(
            GameplayAbility owningAbility,
            string taskInstanceName,
            GameplayAbilityMontage montageToPlay,
            float rate,
            string startSectionName,
            bool stopWhenAbilityEnds,
            float startTimeSeconds)
            : base(
                owningAbility)
        {
            if (montageToPlay == null)
            {
                throw new ArgumentNullException(
                    nameof(montageToPlay));
            }

            if (rate <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(rate),
                    "Montage play rate must be greater than zero.");
            }

            if (startTimeSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(startTimeSeconds),
                    "Montage start time cannot be negative.");
            }

            TaskInstanceName = taskInstanceName;
            m_MontageToPlay = montageToPlay;
            m_Rate = rate;
            m_StartSectionName = startSectionName;
            m_StopWhenAbilityEnds = stopWhenAbilityEnds;
            m_StartTimeSeconds = startTimeSeconds;
        }

        /// <summary>
        /// Creates a task that plays an ability montage and waits for its termination.
        /// </summary>
        public static AbilityTask_PlayMontageAndWait
            CreatePlayMontageAndWaitProxy(
                GameplayAbility owningAbility,
                string taskInstanceName,
                GameplayAbilityMontage montageToPlay,
                float rate = 1f,
                string startSectionName = null,
                bool stopWhenAbilityEnds = true,
                float startTimeSeconds = 0f)
        {
            return new AbilityTask_PlayMontageAndWait(
                owningAbility,
                taskInstanceName,
                montageToPlay,
                rate,
                startSectionName,
                stopWhenAbilityEnds,
                startTimeSeconds);
        }

        /// <summary>
        /// Registers a callback invoked after montage playback starts successfully.
        /// </summary>
        public IDisposable RegisterBlendedInDelegate(
            Action handler)
        {
            return RegisterDelegate(
                m_BlendedInDelegate,
                handler);
        }

        /// <summary>
        /// Registers a callback invoked when the montage begins blending out normally.
        /// </summary>
        public IDisposable RegisterBlendOutDelegate(
            Action handler)
        {
            return RegisterDelegate(
                m_BlendOutDelegate,
                handler);
        }

        /// <summary>
        /// Registers a callback invoked when the montage reaches its natural end.
        /// </summary>
        public IDisposable RegisterCompletedDelegate(
            Action handler)
        {
            return RegisterDelegate(
                m_CompletedDelegate,
                handler);
        }

        /// <summary>
        /// Registers a callback invoked when another montage replaces this playback.
        /// </summary>
        public IDisposable RegisterInterruptedDelegate(
            Action handler)
        {
            return RegisterDelegate(
                m_InterruptedDelegate,
                handler);
        }

        /// <summary>
        /// Registers a callback invoked when montage playback is cancelled.
        /// </summary>
        public IDisposable RegisterCancelledDelegate(
            Action handler)
        {
            return RegisterDelegate(
                m_CancelledDelegate,
                handler);
        }

        /// <summary>
        /// Starts montage playback and begins monitoring its lifecycle.
        /// </summary>
        protected override void Activate()
        {
            if (AbilitySystemComponent == null)
            {
                FinishCancelled();

                return;
            }

            float duration = AbilitySystemComponent.PlayMontage(
                Ability,
                Ability.CurrentActivationInfo,
                m_MontageToPlay,
                m_Rate,
                m_StartSectionName,
                m_StartTimeSeconds);

            if (duration <= 0f)
            {
                FinishCancelled();

                return;
            }

            m_MonitorCancellationSource =
                CancellationTokenSource.CreateLinkedTokenSource(
                    AbilitySystemComponent.GetCancellationTokenOnDestroy());

            m_BlendedInDelegate.Invoke();

            MonitorMontageAsync(
                m_MonitorCancellationSource.Token).Forget();
        }

        /// <summary>
        /// Cancels this task and stops its montage when it still owns playback.
        /// </summary>
        public override void ExternalCancel()
        {
            if (IsEnded)
            {
                return;
            }

            StopPlayingMontage();

            FinishCancelled();
        }

        /// <summary>
        /// Stops owner-bound playback and releases asynchronous monitoring resources.
        /// </summary>
        protected override void OnDestroy(
            bool abilityEnded)
        {
            if (
                abilityEnded &&
                m_StopWhenAbilityEnds)
            {
                StopPlayingMontage();
            }

            CancelMonitoring();

            m_Subscriptions.Dispose();

            m_BlendedInDelegate.Clear();
            m_BlendOutDelegate.Clear();
            m_CompletedDelegate.Clear();
            m_InterruptedDelegate.Clear();
            m_CancelledDelegate.Clear();

            base.OnDestroy(
                abilityEnded);
        }

        private async UniTask MonitorMontageAsync(
            CancellationToken cancellationToken)
        {
            try
            {
                while (!IsEnded)
                {
                    await UniTask.Yield(
                        PlayerLoopTiming.Update,
                        cancellationToken);

                    if (IsEnded)
                    {
                        return;
                    }

                    GameplayAbilityActorInfo actorInfo =
                        AbilitySystemComponent.AbilityActorInfo;

                    if (actorInfo == null ||
                        actorInfo.AnimInstance == null)
                    {
                        FinishInterrupted();

                        return;
                    }

                    AnimInstance animInstance =
                        actorInfo.AnimInstance;

                    if (animInstance.MontageGetIsStopped())
                    {
                        if (animInstance.CurrentMontage ==
                            m_MontageToPlay)
                        {
                            FinishCompleted();
                        }
                        else
                        {
                            FinishInterrupted();
                        }

                        return;
                    }

                    if (animInstance.CurrentMontage !=
                            m_MontageToPlay ||
                        AbilitySystemComponent.GetAnimatingAbility() !=
                            Ability)
                    {
                        FinishInterrupted();

                        return;
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private bool StopPlayingMontage()
        {
            if (AbilitySystemComponent == null ||
                AbilitySystemComponent.GetAnimatingAbility() !=
                    Ability ||
                AbilitySystemComponent.GetCurrentMontage() !=
                    m_MontageToPlay)
            {
                return false;
            }

            AbilitySystemComponent.CurrentMontageStop();

            return true;
        }

        private void FinishCompleted()
        {
            AbilitySystemComponent.ClearAnimatingAbility(
                Ability);

            m_BlendOutDelegate.Invoke();
            m_CompletedDelegate.Invoke();

            EndTask();
        }

        private void FinishInterrupted()
        {
            m_InterruptedDelegate.Invoke();

            EndTask();
        }

        private void FinishCancelled()
        {
            m_CancelledDelegate.Invoke();

            EndTask();
        }

        private IDisposable RegisterDelegate(
            DisposableEvent callbackEvent,
            Action handler)
        {
            IDisposable subscription =
                callbackEvent.Subscribe(
                    handler);

            m_Subscriptions.Add(
                subscription);

            return subscription;
        }

        private void CancelMonitoring()
        {
            CancellationTokenSource cancellationSource =
                m_MonitorCancellationSource;

            m_MonitorCancellationSource = null;

            if (cancellationSource == null)
            {
                return;
            }

            cancellationSource.Cancel();
            cancellationSource.Dispose();
        }
    }
}