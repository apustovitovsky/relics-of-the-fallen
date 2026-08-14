using System;


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
        /// Starts montage playback and binds to its animation lifecycle delegates.
        /// </summary>
        protected override void Activate()
        {
            GameplayAbilityActorInfo actorInfo =
                AbilitySystemComponent?.AbilityActorInfo;

            if (AbilitySystemComponent == null ||
                actorInfo == null ||
                actorInfo.AnimInstance == null)
            {
                FinishCancelled();

                return;
            }

            AnimInstance animInstance =
                actorInfo.AnimInstance;

            m_Subscriptions.Add(
                animInstance.MontageSetBlendedInDelegate(
                    HandleMontageBlendedIn,
                    m_MontageToPlay));

            m_Subscriptions.Add(
                animInstance.MontageSetBlendingOutDelegate(
                    HandleMontageBlendingOut,
                    m_MontageToPlay));

            m_Subscriptions.Add(
                animInstance.MontageSetEndDelegate(
                    HandleMontageEnded,
                    m_MontageToPlay));

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
            }
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

            m_CancelledDelegate.Invoke();

            EndTask();

            StopPlayingMontage();
        }

        /// <summary>
        /// Releases montage delegates and optionally stops owner-bound playback.
        /// </summary>
        protected override void OnDestroy(
            bool abilityEnded)
        {
            m_Subscriptions.Dispose();

            m_BlendedInDelegate.Clear();
            m_BlendOutDelegate.Clear();
            m_CompletedDelegate.Clear();
            m_InterruptedDelegate.Clear();
            m_CancelledDelegate.Clear();

            if (abilityEnded &&
                m_StopWhenAbilityEnds)
            {
                StopPlayingMontage();
            }

            base.OnDestroy(
                abilityEnded);
        }

        /// <summary>
        /// Stops the montage playable while this task still owns its raw animation state.
        /// </summary>
        private bool StopPlayingMontage()
        {
            if (
                AbilitySystemComponent == null ||
                AbilitySystemComponent.GetAnimatingAbility() !=
                    Ability)
            {
                return false;
            }

            GameplayAbilityActorInfo actorInfo =
                AbilitySystemComponent.AbilityActorInfo;

            if (
                actorInfo == null ||
                actorInfo.AnimInstance == null ||
                actorInfo.AnimInstance.CurrentMontage !=
                    m_MontageToPlay)
            {
                return false;
            }

            AbilitySystemComponent.CurrentMontageStop();

            return true;
        }

        /// <summary>
        /// Broadcasts successful natural montage completion.
        /// </summary>
        private void FinishCompleted()
        {
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

        private void HandleMontageBlendedIn(
            GameplayAbilityMontage montage)
        {
            if (IsEnded)
            {
                return;
            }

            m_BlendedInDelegate.Invoke();
        }

        private void HandleMontageBlendingOut(
            GameplayAbilityMontage montage,
            bool wasInterrupted)
        {
            if (IsEnded)
            {
                return;
            }

            AbilitySystemComponent.ClearAnimatingAbility(
                Ability);

            if (wasInterrupted)
            {
                FinishInterrupted();

                return;
            }

            m_BlendOutDelegate.Invoke();
        }

        private void HandleMontageEnded(
            GameplayAbilityMontage montage,
            bool wasInterrupted)
        {
            if (IsEnded)
            {
                return;
            }

            if (wasInterrupted)
            {
                FinishInterrupted();

                return;
            }

            FinishCompleted();
        }
    }
}