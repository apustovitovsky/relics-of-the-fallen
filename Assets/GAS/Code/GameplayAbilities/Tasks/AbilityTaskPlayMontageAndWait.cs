using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace GAS
{
    public sealed class AbilityTaskPlayMontageAndWait : AbilityTask
    {
        private readonly GameplayAbilityMontage m_MontageToPlay;
        private readonly float m_Rate;
        private readonly string m_StartSectionName;
        private readonly bool m_StopWhenAbilityEnds;
        private readonly float m_StartTimeSeconds;

        private CancellationTokenSource m_MonitorCancellationSource;

        public event Action OnBlendedIn;

        public event Action OnBlendOut;

        public event Action OnCompleted;

        public event Action OnInterrupted;

        public event Action OnCancelled;

        public string TaskInstanceName
        {
            get;
        }

        private AbilityTaskPlayMontageAndWait(
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
        public static AbilityTaskPlayMontageAndWait
            CreatePlayMontageAndWaitProxy(
                GameplayAbility owningAbility,
                string taskInstanceName,
                GameplayAbilityMontage montageToPlay,
                float rate = 1f,
                string startSectionName = null,
                bool stopWhenAbilityEnds = true,
                float startTimeSeconds = 0f)
        {
            return new AbilityTaskPlayMontageAndWait(
                owningAbility,
                taskInstanceName,
                montageToPlay,
                rate,
                startSectionName,
                stopWhenAbilityEnds,
                startTimeSeconds);
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

            OnBlendedIn?.Invoke();

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
            if (abilityEnded &&
                m_StopWhenAbilityEnds)
            {
                StopPlayingMontage();
            }

            CancelMonitoring();

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

            EndTask();

            OnBlendOut?.Invoke();
            OnCompleted?.Invoke();
        }

        private void FinishInterrupted()
        {
            EndTask();

            OnInterrupted?.Invoke();
        }

        private void FinishCancelled()
        {
            EndTask();

            OnCancelled?.Invoke();
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