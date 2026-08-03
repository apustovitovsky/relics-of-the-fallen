using System;
using System.Collections.Generic;

namespace GAS
{
    public enum ActiveEffectAuthority
    {
        Predicted,
        Authoritative
    }

    public sealed class ActiveGameplayEffect
    {
        public ActiveGameplayEffectHandle Handle
        {
            get;
            internal set;
        }

        /// <summary>
        /// Identifies this authoritative effect within its replicated container.
        /// </summary>
        public ulong ReplicationId
        {
            get;
            internal set;
        }

        public GameplayEffectSpec Spec
        {
            get;
            private set;
        }

        private readonly DisposableGroup
            m_OngoingTagSubscriptions = new();

        private IDisposable m_PredictionSubscription;

        private IDisposable m_DurationHandle;

        private IDisposable m_PeriodHandle;

        private readonly List<AppliedAttributeModifier>
            m_AppliedModifiers = new();

        internal bool GrantedTagsApplied
        {
            get;
            set;
        }

        public bool IsInhibited
        {
            get;
            internal set;
        }

        internal IReadOnlyList<AppliedAttributeModifier> AppliedModifiers => m_AppliedModifiers;

        public PredictionKey PredictionKey
        {
            get;
            internal set;
        }

        public double StartWorldTime
        {
            get; internal set;
        }

        public double StartServerWorldTime
        {
            get; internal set;
        }

        public double CachedStartServerWorldTime
        {
            get;
            internal set;
        }

        public ActiveEffectAuthority Authority
        {
            get;
            internal set;
        }

        /// <summary>
        /// Creates runtime state for one applied gameplay effect specification.
        /// </summary>
        public ActiveGameplayEffect(
            GameplayEffectSpec spec,
            ActiveEffectAuthority authority)
        {
            Spec = spec ??
                throw new ArgumentNullException(
                    nameof(spec));

            Authority = authority;
        }

        /// <summary>
        /// Creates an authoritative active gameplay effect from replicated runtime state.
        /// </summary>
        public ActiveGameplayEffect(
            ulong replicationId,
            GameplayEffectSpec spec,
            PredictionKey predictionKey,
            double startWorldTime,
            double startServerWorldTime)
            : this(
                spec,
                ActiveEffectAuthority.Authoritative)
        {
            if (replicationId == 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(replicationId),
                    replicationId,
                    "A replicated effect requires a nonzero replication identity.");
            }

            ReplicationId = replicationId;
            PredictionKey = predictionKey;
            StartWorldTime = startWorldTime;
            StartServerWorldTime = startServerWorldTime;
            CachedStartServerWorldTime = startServerWorldTime;
        }

        /// <summary>
        /// Installs this newly replicated effect into its owning active-effect container.
        /// </summary>
        public void PostReplicatedAdd(
            ActiveGameplayEffectsContainer inArray)
        {
            if (inArray == null)
            {
                throw new ArgumentNullException(
                    nameof(inArray));
            }

            inArray.PostReplicatedAdd(
                this);
        }

        /// <summary>
        /// Removes this effect before its replicated item leaves the owning container.
        /// </summary>
        public void PreReplicatedRemove(
            ActiveGameplayEffectsContainer inArray)
        {
            if (inArray == null)
            {
                throw new ArgumentNullException(
                    nameof(inArray));
            }

            inArray.PreReplicatedRemove(
                this);
        }

        /// <summary>
        /// Copies newly replicated fields while preserving this effect's local runtime identity.
        /// </summary>
        public void CopyReplicatedStateFrom(
            ActiveGameplayEffect replicatedState)
        {
            if (replicatedState == null)
            {
                throw new ArgumentNullException(
                    nameof(replicatedState));
            }

            if (!Handle.IsValid)
            {
                throw new InvalidOperationException(
                    "Only a registered active effect can receive replicated changes.");
            }

            if (ReplicationId !=
                replicatedState.ReplicationId)
            {
                throw new ArgumentException(
                    "Replicated effect identities do not match.",
                    nameof(replicatedState));
            }

            if (Spec.DefinitionAsset !=
                replicatedState.Spec.DefinitionAsset)
            {
                throw new ArgumentException(
                    "A replicated update cannot replace the effect definition.",
                    nameof(replicatedState));
            }

            Spec = replicatedState.Spec;
            PredictionKey = replicatedState.PredictionKey;
            StartWorldTime =
                replicatedState.StartWorldTime;
            StartServerWorldTime =
                replicatedState.StartServerWorldTime;
            CachedStartServerWorldTime =
                replicatedState.CachedStartServerWorldTime;
        }

        /// <summary>
        /// Refreshes this effect after its replicated fields have changed.
        /// </summary>
        public void PostReplicatedChange(
            ActiveGameplayEffectsContainer inArray)
        {
            if (inArray == null)
            {
                throw new ArgumentNullException(
                    nameof(inArray));
            }

            inArray.PostReplicatedChange(
                this);
        }

        /// <summary>
        /// Returns the remaining duration at the specified local world time.
        /// </summary>
        public double GetTimeRemaining(
            double currentWorldTime)
        {
            double duration =
                Spec.Duration;

            return
                duration ==
                GameplayEffectGlobals.InfiniteDuration
                    ? GameplayEffectGlobals.InfiniteDuration
                    : duration -
                    (currentWorldTime -
                    StartWorldTime);
        }

        /// <summary>
        /// Recomputes the local start time from synchronized server time.
        /// </summary>
        public void RecomputeStartWorldTime(
            double currentWorldTime,
            double currentServerWorldTime)
        {
            double elapsedServerTime =
                currentServerWorldTime -
                StartServerWorldTime;

            StartWorldTime =
                currentWorldTime -
                elapsedServerTime;

            CachedStartServerWorldTime =
                StartServerWorldTime;
        }

        /// <summary>
        /// Records one attribute modifier owned by this active effect.
        /// </summary>
        internal void AddAppliedModifier(
            Attribute targetAttribute,
            AttributeModifierHandle handle)
        {
            m_AppliedModifiers.Add(
                new AppliedAttributeModifier(
                    targetAttribute,
                    handle));
        }

        /// <summary>
        /// Clears modifier ownership after their aggregator entries are removed.
        /// </summary>
        internal void ClearAppliedModifiers()
        {
            m_AppliedModifiers.Clear();
        }

        /// <summary>
        /// Assigns the prediction resolution subscription owned by this active effect.
        /// </summary>
        internal void SetPredictionSubscription(
            IDisposable subscription)
        {
            if (subscription == null)
            {
                throw new ArgumentNullException(
                    nameof(subscription));
            }

            if (m_PredictionSubscription != null)
            {
                throw new InvalidOperationException(
                    "The active effect already owns a prediction subscription.");
            }

            m_PredictionSubscription =
                subscription;
        }

        /// <summary>
        /// Disposes the prediction resolution subscription owned by this active effect.
        /// </summary>
        internal void DisposePredictionSubscription()
        {
            IDisposable subscription =
                m_PredictionSubscription;

            m_PredictionSubscription =
                null;

            subscription?.Dispose();
        }

        /// <summary>
        /// Replaces the scheduled duration callback owned by this active effect.
        /// </summary>
        internal void SetDurationHandle(
            IDisposable durationHandle)
        {
            IDisposable previousDurationHandle =
                m_DurationHandle;

            m_DurationHandle =
                durationHandle ?? throw new ArgumentNullException(
                    nameof(durationHandle));

            previousDurationHandle?.Dispose();
        }

        /// <summary>
        /// Cancels the scheduled duration callback owned by this active effect.
        /// </summary>
        internal void DisposeDurationHandle()
        {
            IDisposable durationHandle =
                m_DurationHandle;

            m_DurationHandle =
                null;

            durationHandle?.Dispose();
        }

        /// <summary>
        /// Replaces the scheduled periodic callback owned by this active effect.
        /// </summary>
        internal void SetPeriodHandle(
            IDisposable periodHandle)
        {
            IDisposable previousPeriodHandle =
                m_PeriodHandle;

            m_PeriodHandle =
                periodHandle ?? throw new ArgumentNullException(
                    nameof(periodHandle));

            previousPeriodHandle?.Dispose();
        }

        /// <summary>
        /// Cancels the scheduled periodic callback owned by this active effect.
        /// </summary>
        internal void DisposePeriodHandle()
        {
            IDisposable periodHandle =
                m_PeriodHandle;

            m_PeriodHandle = null;

            periodHandle?.Dispose();
        }

        /// <summary>
        /// Records one ongoing gameplay tag subscription owned by this effect.
        /// </summary>
        internal void AddOngoingTagSubscription(
            IDisposable subscription)
        {
            m_OngoingTagSubscriptions.Add(
                subscription);
        }

        /// <summary>
        /// Disposes every ongoing gameplay tag subscription owned by this effect.
        /// </summary>
        internal void DisposeOngoingTagSubscriptions()
        {
            m_OngoingTagSubscriptions.Dispose();
        }
    }
}