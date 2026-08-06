using System;

namespace GAS
{
    public sealed class AbilityReplicatedDataCache
    {
        private const int k_GenericEventCount =
            (int)GameplayAbilityGenericReplicatedEvent.Max;

        private readonly DisposableEvent<
            GameplayAbilityTargetDataHandle,
            GameplayTag> m_TargetSetDelegate = new();

        private readonly DisposableEvent
            m_TargetCancelledDelegate = new();

        private readonly DisposableEvent[]
            m_GenericEventDelegates =
                new DisposableEvent[k_GenericEventCount];

        private readonly bool[]
            m_GenericEventsTriggered =
                new bool[k_GenericEventCount];

        /// <summary>
        /// Creates an empty activation cache with one delegate slot per generic replicated event.
        /// </summary>
        public AbilityReplicatedDataCache()
        {
            for (
                int index = 0;
                index < m_GenericEventDelegates.Length;
                index++)
            {
                m_GenericEventDelegates[index] =
                    new DisposableEvent();
            }
        }

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
        /// Registers a callback for one generic replicated ability event.
        /// </summary>
        internal IDisposable RegisterGenericEventDelegate(
            GameplayAbilityGenericReplicatedEvent eventType,
            Action handler)
        {
            int eventIndex =
                GetGenericEventIndex(
                    eventType);

            return m_GenericEventDelegates[eventIndex].Subscribe(
                handler);
        }

        /// <summary>
        /// Stores one generic replicated ability event and notifies current listeners.
        /// </summary>
        internal void SetGenericEvent(
            GameplayAbilityGenericReplicatedEvent eventType,
            PredictionKey predictionKey)
        {
            int eventIndex =
                GetGenericEventIndex(
                    eventType);

            m_GenericEventsTriggered[eventIndex] = true;
            PredictionKey = predictionKey;

            m_GenericEventDelegates[eventIndex].Invoke();
        }

        /// <summary>
        /// Invokes a generic event delegate when the event arrived before registration.
        /// </summary>
        internal bool CallGenericEventDelegateIfSet(
            GameplayAbilityGenericReplicatedEvent eventType)
        {
            int eventIndex =
                GetGenericEventIndex(
                    eventType);

            if (!m_GenericEventsTriggered[eventIndex])
            {
                return false;
            }

            m_GenericEventDelegates[eventIndex].Invoke();

            return true;
        }

        /// <summary>
        /// Consumes one cached generic replicated ability event while preserving its delegate.
        /// </summary>
        internal void ConsumeGenericEvent(
            GameplayAbilityGenericReplicatedEvent eventType)
        {
            int eventIndex =
                GetGenericEventIndex(
                    eventType);

            m_GenericEventsTriggered[eventIndex] = false;
        }

        /// <summary>
        /// Returns the validated cache index for one generic replicated ability event.
        /// </summary>
        private static int GetGenericEventIndex(
            GameplayAbilityGenericReplicatedEvent eventType)
        {
            int eventIndex =
                (int)eventType;

            if (
                eventIndex < 0 ||
                eventIndex >= k_GenericEventCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(eventType),
                    eventType,
                    "Generic replicated event type is outside the supported range.");
            }

            return eventIndex;
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
            Reset();

            TargetData = targetData ?? throw new ArgumentNullException(
                    nameof(targetData));

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

            Array.Clear(
                m_GenericEventsTriggered,
                0,
                m_GenericEventsTriggered.Length);
        }

        /// <summary>
        /// Resets cached data and removes every registered delegate.
        /// </summary>
        public void ResetAll()
        {
            Reset();

            m_TargetSetDelegate.Clear();
            m_TargetCancelledDelegate.Clear();

            for (
                int index = 0;
                index < m_GenericEventDelegates.Length;
                index++)
            {
                m_GenericEventDelegates[index].Clear();
            }
        }
    }
}