using System;

namespace GAS
{
    public sealed class AbilityReplicatedDataCache
    {
        private readonly DisposableEvent<
            GameplayAbilityTargetDataHandle,
            GameplayTag> m_TargetSetDelegate = new();

        private readonly DisposableEvent
            m_TargetCancelledDelegate = new();

        public GameplayAbilityTargetDataHandle TargetData
        {
            get;
            private set;
        } = new();

        public GameplayTag ApplicationTag
        {
            get;
            private set;
        }

        public bool TargetConfirmed
        {
            get;
            private set;
        }

        public bool TargetCancelled
        {
            get;
            private set;
        }

        public PredictionKey PredictionKey
        {
            get;
            private set;
        }

        /// <summary>
        /// Registers a callback invoked when confirmed replicated target data becomes available.
        /// </summary>
        internal IDisposable RegisterTargetSetDelegate(
            Action<GameplayAbilityTargetDataHandle, GameplayTag> handler)
        {
            return m_TargetSetDelegate.Subscribe(
                handler);
        }

        /// <summary>
        /// Registers a callback invoked when replicated targeting is cancelled.
        /// </summary>
        internal IDisposable RegisterTargetCancelledDelegate(
            Action handler)
        {
            return m_TargetCancelledDelegate.Subscribe(
                handler);
        }

        /// <summary>
        /// Stores confirmed target data and notifies current listeners.
        /// </summary>
        internal void SetTargetData(
            GameplayAbilityTargetDataHandle targetData,
            GameplayTag applicationTag,
            PredictionKey predictionKey)
        {
            if (targetData == null)
            {
                throw new ArgumentNullException(
                    nameof(targetData));
            }

            Reset();

            TargetData = targetData;
            ApplicationTag = applicationTag;
            PredictionKey = predictionKey;
            TargetConfirmed = true;

            m_TargetSetDelegate.Invoke(
                TargetData,
                ApplicationTag);
        }

        /// <summary>
        /// Stores target cancellation and notifies current listeners.
        /// </summary>
        internal void SetTargetCancelled(
            PredictionKey predictionKey)
        {
            Reset();

            PredictionKey = predictionKey;
            TargetCancelled = true;

            m_TargetCancelledDelegate.Invoke();
        }

        /// <summary>
        /// Invokes the cached target-data delegate when data arrived before registration.
        /// </summary>
        internal bool CallDelegatesIfSet()
        {
            if (TargetConfirmed)
            {
                m_TargetSetDelegate.Invoke(
                    TargetData,
                    ApplicationTag);

                return true;
            }

            if (TargetCancelled)
            {
                m_TargetCancelledDelegate.Invoke();

                return true;
            }

            return false;
        }

        /// <summary>
        /// Resets cached data while preserving registered delegates.
        /// </summary>
        public void Reset()
        {
            TargetData =
                new GameplayAbilityTargetDataHandle();

            ApplicationTag = null;
            TargetConfirmed = false;
            TargetCancelled = false;
            PredictionKey = default;
        }

        /// <summary>
        /// Resets cached data and removes every registered delegate.
        /// </summary>
        public void ResetAll()
        {
            Reset();

            m_TargetSetDelegate.Clear();
            m_TargetCancelledDelegate.Clear();
        }
    }
}