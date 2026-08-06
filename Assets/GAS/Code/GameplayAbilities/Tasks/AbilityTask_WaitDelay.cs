using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace GAS
{
    public sealed class AbilityTask_WaitDelay :
        AbilityTask
    {
        private readonly DisposableEvent m_FinishDelegate =
            new();

        private readonly DisposableGroup m_Subscriptions =
            new();

        private CancellationTokenSource m_CancellationSource;

        public float Time
        {
            get;
        }

        private AbilityTask_WaitDelay(
            GameplayAbility owningAbility,
            float time)
            : base(
                owningAbility)
        {
            if (time < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(time));
            }

            Time = time;
        }

        /// <summary>
        /// Creates an ability task that completes after the specified gameplay time.
        /// </summary>
        public static AbilityTask_WaitDelay WaitDelay(
            GameplayAbility owningAbility,
            float time)
        {
            return new AbilityTask_WaitDelay(
                owningAbility,
                time);
        }

        /// <summary>
        /// Registers a completion callback for the lifetime of this delay task.
        /// </summary>
        public IDisposable RegisterFinishDelegate(
            Action handler)
        {
            IDisposable subscription =
                m_FinishDelegate.Subscribe(
                    handler);

            m_Subscriptions.Add(
                subscription);

            return subscription;
        }

        /// <summary>
        /// Starts the owner-bound gameplay delay.
        /// </summary>
        protected override void Activate()
        {
            m_CancellationSource =
                CancellationTokenSource.CreateLinkedTokenSource(
                    AbilitySystemComponent.GetCancellationTokenOnDestroy());

            WaitForDelay(
                    m_CancellationSource.Token)
                .Forget();
        }

        /// <summary>
        /// Cancels the delay and releases its callbacks when the task ends.
        /// </summary>
        protected override void OnDestroy(
            bool abilityEnded)
        {
            CancelDelay();

            m_Subscriptions.Dispose();
            m_FinishDelegate.Clear();

            base.OnDestroy(
                abilityEnded);
        }

        /// <summary>
        /// Waits for the configured delay before completing this task.
        /// </summary>
        private async UniTask WaitForDelay(
            CancellationToken cancellationToken)
        {
            bool isCancelled =
                await UniTask
                    .Delay(
                        TimeSpan.FromSeconds(
                            Time),
                        cancellationToken:
                            cancellationToken)
                    .SuppressCancellationThrow();

            if (
                isCancelled ||
                IsEnded)
            {
                return;
            }

            m_FinishDelegate.Invoke();

            EndTask();
        }

        /// <summary>
        /// Stops the active delay operation and releases its cancellation source.
        /// </summary>
        private void CancelDelay()
        {
            if (m_CancellationSource == null)
            {
                return;
            }

            m_CancellationSource.Cancel();
            m_CancellationSource.Dispose();
            m_CancellationSource = null;
        }
    }
}