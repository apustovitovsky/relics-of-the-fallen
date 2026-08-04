using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GAS
{
    public sealed class ActiveGameplayEffectsContainer
    {
        private readonly AbilitySystemComponent m_Owner;

        private readonly Dictionary<
            ActiveGameplayEffectHandle,
            ActiveGameplayEffect> m_ActiveByHandle = new();

        private readonly Dictionary<
            ulong,
            ActiveGameplayEffect>
            m_AuthoritativeByReplicationId = new();

        private readonly Dictionary<
            PredictionKey,
            HashSet<ActiveGameplayEffectHandle>>
            m_PredictedByKey = new();

        private readonly Dictionary<
            ActiveGameplayEffectHandle,
            ActiveGameplayEffect>
            m_PendingOngoingEvaluations = new();

        private readonly List<ActiveGameplayEffect>
            m_OngoingEvaluationBuffer = new();

        private bool m_IsEvaluatingOngoingRequirements;

        private ulong m_NextReplicationId;

        public AbilitySystemComponent Owner =>
            m_Owner;

        /// <summary>
        /// Exposes the authoritative active effects currently owned by this container.
        /// </summary>
        public IReadOnlyCollection<ActiveGameplayEffect> AuthoritativeGameplayEffects =>
            m_AuthoritativeByReplicationId.Values;

        /// <summary>
        /// Notifies optional observers after an authoritative active effect is registered.
        /// </summary>
        public event Action<ActiveGameplayEffect>
            AuthoritativeGameplayEffectAdded;

        /// <summary>
        /// Notifies replication adapters after an authoritative active effect changes.
        /// </summary>
        public event Action<ActiveGameplayEffect>
            AuthoritativeGameplayEffectChanged;

        /// <summary>
        /// Notifies optional observers after an authoritative active effect is removed.
        /// </summary>
        public event Action<ActiveGameplayEffect>
            AuthoritativeGameplayEffectRemoved;

        /// <summary>
        /// Creates an active effect container for one target component.
        /// </summary>
        internal ActiveGameplayEffectsContainer(
            AbilitySystemComponent owner)
        {
            if (owner == null)
            {
                throw new ArgumentNullException(
                    nameof(owner));
            }

            m_Owner =
                owner;
        }

        /// <summary>
        /// Marks one authoritative active effect for replicated state synchronization.
        /// </summary>
        public void MarkItemDirty(
            ActiveGameplayEffect activeEffect)
        {
            if (activeEffect == null)
            {
                throw new ArgumentNullException(
                    nameof(activeEffect));
            }

            if (
                !m_AuthoritativeByReplicationId.TryGetValue(
                    activeEffect.ReplicationId,
                    out ActiveGameplayEffect registeredEffect) ||
                !ReferenceEquals(
                    registeredEffect,
                    activeEffect))
            {
                throw new InvalidOperationException(
                    "Only an active effect owned by this container can be marked dirty.");
            }

            AuthoritativeGameplayEffectChanged?.Invoke(
                activeEffect);
        }

        /// <summary>
        /// Registers a predicted active effect and installs its attribute modifiers.
        /// </summary>
        internal ActiveGameplayEffect RegisterPredicted(
            GameplayEffectSpec spec,
            PredictionKey predictionKey,
            double startWorldTime,
            double startServerWorldTime)
        {
            if (!predictionKey.IsValid)
            {
                throw new ArgumentException(
                    "A predicted effect requires a valid prediction key.",
                    nameof(predictionKey));
            }

            if (
                !m_PredictedByKey.TryGetValue(
                    predictionKey,
                    out HashSet<
                        ActiveGameplayEffectHandle> predictedHandles))
            {
                predictedHandles =
                    new HashSet<
                        ActiveGameplayEffectHandle>();

                m_PredictedByKey.Add(
                    predictionKey,
                    predictedHandles);
            }

            ActiveGameplayEffect activeEffect =
                new(
                    spec,
                    ActiveEffectAuthority.Predicted)
                {
                    Handle =
                        ActiveGameplayEffectHandle.GenerateNewHandle(),

                    PredictionKey =
                        predictionKey,

                    StartWorldTime =
                        startWorldTime,

                    StartServerWorldTime =
                        startServerWorldTime,

                    CachedStartServerWorldTime =
                        startServerWorldTime
                };

            void HandlePredictionResolved()
            {
                _ =
                    RemoveActiveGameplayEffect(
                        activeEffect.Handle);
            }

            try
            {
                predictedHandles.Add(
                    activeEffect.Handle);

                m_ActiveByHandle.Add(
                    activeEffect.Handle,
                    activeEffect);

                IDisposable predictionSubscription =
                    m_Owner
                        .PredictionKeyDelegates
                        .RegisterRejectOrCaughtUpDelegate(
                            predictionKey,
                            HandlePredictionResolved);

                activeEffect.SetPredictionSubscription(
                    predictionSubscription);

                RegisterOngoingTagEvents(
                    activeEffect);

                ApplyModifiers(
                    activeEffect);

                return activeEffect;
            }
            catch
            {
                m_ActiveByHandle.Remove(
                    activeEffect.Handle);

                predictedHandles.Remove(
                    activeEffect.Handle);

                if (predictedHandles.Count == 0)
                {
                    m_PredictedByKey.Remove(
                        predictionKey);
                }

                activeEffect.DisposePredictionSubscription();
                activeEffect.DisposeOngoingTagSubscriptions();

                RemoveAppliedModifiers(
                    activeEffect);

                throw;
            }
        }

        /// <summary>
        /// Registers one constructed authoritative effect and installs its runtime state.
        /// </summary>
        private ActiveGameplayEffect RegisterAuthoritative(
            ActiveGameplayEffect activeEffect)
        {
            if (activeEffect == null)
            {
                throw new ArgumentNullException(
                    nameof(activeEffect));
            }

            if (activeEffect.Authority !=
                ActiveEffectAuthority.Authoritative)
            {
                throw new ArgumentException(
                    "Only authoritative effects can be registered by this path.",
                    nameof(activeEffect));
            }

            if (activeEffect.ReplicationId == 0)
            {
                throw new ArgumentException(
                    "An authoritative effect requires a replication identity.",
                    nameof(activeEffect));
            }

            if (activeEffect.Handle.IsValid)
            {
                throw new InvalidOperationException(
                    "The authoritative effect is already assigned a local handle.");
            }

            activeEffect.Handle =
                ActiveGameplayEffectHandle.GenerateNewHandle();

            try
            {
                m_ActiveByHandle.Add(
                    activeEffect.Handle,
                    activeEffect);

                m_AuthoritativeByReplicationId.Add(
                    activeEffect.ReplicationId,
                    activeEffect);

                RegisterOngoingTagEvents(
                    activeEffect);

                ApplyModifiers(
                    activeEffect);
            }
            catch
            {
                m_AuthoritativeByReplicationId.Remove(
                    activeEffect.ReplicationId);

                m_ActiveByHandle.Remove(
                    activeEffect.Handle);

                activeEffect.DisposeOngoingTagSubscriptions();

                RemoveAppliedModifiers(
                    activeEffect);

                throw;
            }

            AuthoritativeGameplayEffectAdded?.Invoke(
                activeEffect);

            return activeEffect;
        }

        /// <summary>
        /// Creates and registers authoritative active effect state under its replication identity.
        /// </summary>
        private ActiveGameplayEffect RegisterAuthoritative(
            ulong replicationId,
            GameplayEffectSpec spec,
            PredictionKey predictionKey,
            double startWorldTime,
            double startServerWorldTime)
        {
            ActiveGameplayEffect activeEffect =
                new(
                    replicationId,
                    spec,
                    predictionKey,
                    startWorldTime,
                    startServerWorldTime);

            return RegisterAuthoritative(
                activeEffect);
        }

        /// <summary>
        /// Registers a locally created authoritative effect with a new replication identity.
        /// </summary>
        internal ActiveGameplayEffect RegisterAuthoritative(
            GameplayEffectSpec spec,
            PredictionKey predictionKey,
            double startWorldTime,
            double startServerWorldTime)
        {
            ulong replicationId =
                ++m_NextReplicationId;

            return RegisterAuthoritative(
                replicationId,
                spec,
                predictionKey,
                startWorldTime,
                startServerWorldTime);
        }

        /// <summary>
        /// Registers an active effect after its replicated state has been received.
        /// </summary>
        internal void PostReplicatedAdd(
            ActiveGameplayEffect activeEffect)
        {
            RegisterAuthoritative(
                activeEffect);

            CheckDuration(
                activeEffect.Handle);
        }

        /// <summary>
        /// Refreshes runtime state after an existing active effect receives replicated changes.
        /// </summary>
        internal void PostReplicatedChange(
            ActiveGameplayEffect activeEffect)
        {
            if (activeEffect == null)
            {
                throw new ArgumentNullException(
                    nameof(activeEffect));
            }

            if (
                !m_AuthoritativeByReplicationId.TryGetValue(
                    activeEffect.ReplicationId,
                    out ActiveGameplayEffect registeredEffect))
            {
                throw new InvalidOperationException(
                    "The replicated active effect is not registered.");
            }

            if (!ReferenceEquals(
                    registeredEffect,
                    activeEffect))
            {
                throw new InvalidOperationException(
                    "The replicated effect does not match its registered runtime item.");
            }

            if (
                !activeEffect.IsInhibited &&
                !activeEffect.Spec.IsPeriodic)
            {
                IReadOnlyList<AttributeModifierSpec> modifierSpecs =
                    activeEffect.Spec.ModifierSpecs;

                IReadOnlyList<AppliedAttributeModifier> appliedModifiers =
                    activeEffect.AppliedModifiers;

                if (modifierSpecs.Count !=
                    appliedModifiers.Count)
                {
                    throw new InvalidOperationException(
                        "Replicated modifiers do not match their aggregator entries.");
                }

                for (
                    int index = 0;
                    index < modifierSpecs.Count;
                    index++)
                {
                    AttributeModifierSpec modifierSpec =
                        modifierSpecs[index];

                    if (!modifierSpec.HasEvaluatedMagnitude)
                    {
                        throw new InvalidOperationException(
                            "A replicated modifier has no evaluated magnitude.");
                    }

                    AppliedAttributeModifier appliedModifier =
                        appliedModifiers[index];

                    if (
                        !appliedModifier.TargetAttribute.UpdateModifier(
                            appliedModifier.Handle,
                            modifierSpec.EvaluatedMagnitude))
                    {
                        throw new InvalidOperationException(
                            "A replicated modifier has no aggregator entry.");
                    }
                }
            }

            CheckDuration(
                activeEffect.Handle);
        }

        /// <summary>
        /// Removes an active effect before its replicated item leaves the container.
        /// </summary>
        internal void PreReplicatedRemove(
            ActiveGameplayEffect activeEffect)
        {
            if (activeEffect == null)
            {
                throw new ArgumentNullException(
                    nameof(activeEffect));
            }

            if (
                !m_AuthoritativeByReplicationId.TryGetValue(
                    activeEffect.ReplicationId,
                    out ActiveGameplayEffect registeredEffect))
            {
                return;
            }

            if (!ReferenceEquals(
                    registeredEffect,
                    activeEffect))
            {
                throw new InvalidOperationException(
                    "The replicated effect does not match its registered runtime item.");
            }

            RemoveActiveGameplayEffect(
                activeEffect.Handle);
        }

        /// <summary>
        /// Removes an active effect and every runtime entry owned by it.
        /// </summary>
        internal bool RemoveActiveGameplayEffect(
            ActiveGameplayEffectHandle handle)
        {
            if (
                !m_ActiveByHandle.TryGetValue(
                    handle,
                    out ActiveGameplayEffect activeEffect))
            {
                return false;
            }

            bool wasAuthoritative =
                activeEffect.Authority ==
                ActiveEffectAuthority.Authoritative;

            m_ActiveByHandle.Remove(
                handle);

            activeEffect.DisposeDurationHandle();
            activeEffect.DisposePeriodHandle();
            activeEffect.DisposeOngoingTagSubscriptions();
            activeEffect.DisposePredictionSubscription();

            switch (activeEffect.Authority)
            {
                case ActiveEffectAuthority.Predicted:
                    if (
                        m_PredictedByKey.TryGetValue(
                            activeEffect.PredictionKey,
                            out HashSet<
                                ActiveGameplayEffectHandle> predictedHandles))
                    {
                        predictedHandles.Remove(
                            activeEffect.Handle);

                        if (predictedHandles.Count == 0)
                        {
                            m_PredictedByKey.Remove(
                                activeEffect.PredictionKey);
                        }
                    }

                    break;

                case ActiveEffectAuthority.Authoritative:
                    m_AuthoritativeByReplicationId.Remove(
                        activeEffect.ReplicationId);

                    break;
            }

            RemoveAppliedModifiers(
                activeEffect);

            if (wasAuthoritative)
            {
                AuthoritativeGameplayEffectRemoved?.Invoke(
                    activeEffect);
            }

            return true;
        }

        /// <summary>
        /// Returns an active effect identified by its local handle.
        /// </summary>
        internal bool TryGetActiveGameplayEffect(
            ActiveGameplayEffectHandle handle,
            out ActiveGameplayEffect activeEffect)
        {
            return m_ActiveByHandle.TryGetValue(
                handle,
                out activeEffect);
        }

        /// <summary>
        /// Modifies an active gameplay effect start time and refreshes its duration state.
        /// </summary>
        public void ModifyActiveEffectStartTime(
            ActiveGameplayEffectHandle handle,
            float startTimeDiff)
        {
            if (
                !TryGetActiveGameplayEffect(
                    handle,
                    out ActiveGameplayEffect activeEffect))
            {
                return;
            }

            activeEffect.StartWorldTime +=
                startTimeDiff;

            activeEffect.StartServerWorldTime +=
                startTimeDiff;

            activeEffect.CachedStartServerWorldTime +=
                startTimeDiff;

            if (
                activeEffect.Authority ==
                ActiveEffectAuthority.Authoritative)
            {
                MarkItemDirty(
                    activeEffect);
            }

            CheckDuration(
                handle);
        }

        /// <summary>
        /// Recomputes local start times for every active effect after clock synchronization changes.
        /// </summary>
        public void RecomputeStartWorldTimes(
            double currentWorldTime,
            double currentServerWorldTime)
        {
            foreach (
                ActiveGameplayEffect activeEffect
                in m_ActiveByHandle.Values)
            {
                activeEffect.RecomputeStartWorldTime(
                    currentWorldTime,
                    currentServerWorldTime);
            }
        }

        /// <summary>
        /// Executes the evaluated modifiers of one active periodic gameplay effect.
        /// </summary>
        internal void ExecutePeriodicGameplayEffect(
            ActiveGameplayEffectHandle handle)
        {
            if (
                !TryGetActiveGameplayEffect(
                    handle,
                    out ActiveGameplayEffect activeEffect))
            {
                return;
            }

            if (
                activeEffect.Authority !=
                ActiveEffectAuthority.Authoritative)
            {
                return;
            }

            if (
                !activeEffect.Spec.IsPeriodic ||
                activeEffect.IsInhibited)
            {
                return;
            }

            m_Owner.ExecuteGameplayEffect(
                activeEffect.Spec);
        }


        /// <summary>
        /// Starts the repeating execution timer owned by one authoritative periodic effect.
        /// </summary>
        internal void StartPeriodicGameplayEffect(
            ActiveGameplayEffectHandle handle)
        {
            if (
                !TryGetActiveGameplayEffect(
                    handle,
                    out ActiveGameplayEffect activeEffect))
            {
                return;
            }

            if (
                activeEffect.Authority !=
                ActiveEffectAuthority.Authoritative)
            {
                return;
            }

            if (!activeEffect.Spec.IsPeriodic)
            {
                return;
            }

            CancellationTokenSource periodCancellationSource =
                CancellationTokenSource.CreateLinkedTokenSource(
                    m_Owner.GetCancellationTokenOnDestroy());

            void CancelPeriod()
            {
                periodCancellationSource.Cancel();
                periodCancellationSource.Dispose();
            }

            activeEffect.SetPeriodHandle(
                new DisposableSubscription(
                    CancelPeriod));

            RunPeriodicGameplayEffect(
                    handle,
                    activeEffect.Spec.Period,
                    periodCancellationSource.Token)
                .Forget();
        }

        /// <summary>
        /// Waits for each period and executes the corresponding active gameplay effect.
        /// </summary>
        private async UniTaskVoid RunPeriodicGameplayEffect(
            ActiveGameplayEffectHandle handle,
            float period,
            CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                bool isCanceled =
                    await UniTask
                        .Delay(
                            TimeSpan.FromSeconds(
                                period),
                            cancellationToken:
                                cancellationToken)
                        .SuppressCancellationThrow();

                if (isCanceled)
                {
                    return;
                }

                m_Owner.ExecutePeriodicEffect(
                    handle);
            }
        }

        /// <summary>
        /// Removes an expired effect or schedules another duration check.
        /// </summary>
        internal void CheckDuration(
            ActiveGameplayEffectHandle handle)
        {
            if (
                !TryGetActiveGameplayEffect(
                    handle,
                    out ActiveGameplayEffect activeEffect))
            {
                return;
            }

            if (
                activeEffect.Spec.Duration ==
                GameplayEffectGlobals.InfiniteDuration)
            {
                activeEffect.DisposeDurationHandle();

                return;
            }

            double remainingDuration =
                activeEffect.GetTimeRemaining(
                    Time.timeAsDouble);

            if (remainingDuration <= 0d)
            {
                m_Owner.RemoveActiveGameplayEffect(
                    handle);

                return;
            }

            CancellationTokenSource durationCancellationSource =
                CancellationTokenSource.CreateLinkedTokenSource(
                    m_Owner.GetCancellationTokenOnDestroy());

            void CancelDurationCheck()
            {
                durationCancellationSource.Cancel();
                durationCancellationSource.Dispose();
            }

            activeEffect.SetDurationHandle(
                new DisposableSubscription(
                    CancelDurationCheck));

            WaitForDurationCheck(
                    handle,
                    remainingDuration,
                    durationCancellationSource.Token)
                .Forget();
        }

        /// <summary>
        /// Waits until an active effect requires another duration check.
        /// </summary>
        private async UniTaskVoid WaitForDurationCheck(
            ActiveGameplayEffectHandle handle,
            double delaySeconds,
            CancellationToken cancellationToken)
        {
            bool isCanceled =
                await UniTask
                    .Delay(
                        TimeSpan.FromSeconds(
                            delaySeconds),
                        cancellationToken:
                            cancellationToken)
                    .SuppressCancellationThrow();

            if (isCanceled)
            {
                return;
            }

            CheckDuration(
                handle);
        }

        /// <summary>
        /// Installs persistent modifiers and granted tags when ongoing requirements are satisfied.
        /// </summary>
        private void ApplyModifiers(
            ActiveGameplayEffect activeEffect)
        {
            if (
                !AreOngoingTagRequirementsSatisfied(
                    activeEffect))
            {
                activeEffect.IsInhibited = true;

                return;
            }

            activeEffect.IsInhibited = false;

            if (!activeEffect.Spec.IsPeriodic)
            {
                foreach (
                    AttributeModifierSpec modifierSpec
                    in activeEffect.Spec.ModifierSpecs)
                {
                    if (!modifierSpec.HasEvaluatedMagnitude)
                    {
                        throw new InvalidOperationException(
                            "An active effect requires evaluated modifier magnitudes.");
                    }

                    Attribute targetAttribute =
                        m_Owner.GetAttribute(
                            modifierSpec.Definition.Attribute);

                    AttributeModifierHandle handle =
                        targetAttribute.AddModifier(
                            modifierSpec.EvaluatedMagnitude,
                            modifierSpec.Definition.Operation);

                    activeEffect.AddAppliedModifier(
                        targetAttribute,
                        handle);
                }
            }

            activeEffect.GrantedTagsApplied = true;

            m_Owner.UpdateTagMap(
                activeEffect.Spec
                    .Definition
                    .gameplayEffectTags
                    .GrantedTags,
                1);
        }

        /// <summary>
        /// Removes every modifier and granted tag owned by an active effect.
        /// </summary>
        private void RemoveAppliedModifiers(
            ActiveGameplayEffect activeEffect)
        {
            IReadOnlyList<AppliedAttributeModifier> appliedModifiers =
                activeEffect.AppliedModifiers;

            for (
                int index = appliedModifiers.Count - 1;
                index >= 0;
                index--)
            {
                AppliedAttributeModifier appliedModifier =
                    appliedModifiers[index];

                appliedModifier.TargetAttribute.RemoveModifier(
                    appliedModifier.Handle);
            }

            activeEffect.ClearAppliedModifiers();

            if (!activeEffect.GrantedTagsApplied)
            {
                return;
            }

            m_Owner.UpdateTagMap(
                activeEffect.Spec
                    .Definition
                    .gameplayEffectTags
                    .GrantedTags,
                -1);

            activeEffect.GrantedTagsApplied =
                false;
        }

        /// <summary>
        /// Registers unique tag dependencies used by one active effect.
        /// </summary>
        private void RegisterOngoingTagEvents(
            ActiveGameplayEffect activeEffect)
        {
            void HandleOngoingTagChanged(
                GameplayTag changedTag,
                int newCount)
            {
                _ = changedTag;
                _ = newCount;

                QueueOngoingTagRequirementEvaluation(
                    activeEffect);
            }

            GameplayEffectTags effectTags =
                activeEffect.Spec
                    .Definition
                    .gameplayEffectTags;

            if (
                effectTags.OngoingTagRequirementsRequired.Count == 0 &&
                effectTags.OngoingTagRequirementsForbidden.Count == 0)
            {
                return;
            }

            HashSet<GameplayTag> registeredTags =
                new();

            RegisterOngoingTagEventList(
                activeEffect,
                effectTags.OngoingTagRequirementsRequired,
                registeredTags,
                HandleOngoingTagChanged);

            RegisterOngoingTagEventList(
                activeEffect,
                effectTags.OngoingTagRequirementsForbidden,
                registeredTags,
                HandleOngoingTagChanged);
        }

        /// <summary>
        /// Registers subscriptions for one gameplay tag requirement list.
        /// </summary>
        private void RegisterOngoingTagEventList(
            ActiveGameplayEffect activeEffect,
            IReadOnlyList<GameplayTag> tags,
            HashSet<GameplayTag> registeredTags,
            Action<GameplayTag, int> handler)
        {
            for (
                int index = 0;
                index < tags.Count;
                index++)
            {
                GameplayTag tag =
                    tags[index];

                if (
                    tag == null ||
                    !registeredTags.Add(
                        tag))
                {
                    continue;
                }

                IDisposable subscription =
                    m_Owner.RegisterGameplayTagEvent(
                        tag,
                        GameplayTagEventType.NewOrRemoved,
                        handler);

                activeEffect.AddOngoingTagSubscription(
                    subscription);
            }
        }

        /// <summary>
        /// Queues one active effect for ongoing requirement reevaluation.
        /// </summary>
        private void QueueOngoingTagRequirementEvaluation(
            ActiveGameplayEffect activeEffect)
        {
            if (
                !m_ActiveByHandle.TryGetValue(
                    activeEffect.Handle,
                    out ActiveGameplayEffect registeredEffect) ||
                !ReferenceEquals(
                    activeEffect,
                    registeredEffect))
            {
                return;
            }

            m_PendingOngoingEvaluations[activeEffect.Handle] =
                activeEffect;

            if (m_IsEvaluatingOngoingRequirements)
            {
                return;
            }

            EvaluatePendingOngoingRequirements();
        }

        /// <summary>
        /// Processes targeted ongoing requirement updates until tag state stabilizes.
        /// </summary>
        private void EvaluatePendingOngoingRequirements()
        {
            m_IsEvaluatingOngoingRequirements =
                true;

            try
            {
                int evaluationPass =
                    0;

                while (m_PendingOngoingEvaluations.Count > 0)
                {
                    evaluationPass++;

                    if (evaluationPass > 32)
                    {
                        throw new InvalidOperationException(
                            "Ongoing tag requirements did not reach a stable state.");
                    }

                    m_OngoingEvaluationBuffer.Clear();

                    foreach (
                        ActiveGameplayEffect activeEffect
                        in m_PendingOngoingEvaluations.Values)
                    {
                        m_OngoingEvaluationBuffer.Add(
                            activeEffect);
                    }

                    m_PendingOngoingEvaluations.Clear();

                    for (
                        int index = 0;
                        index < m_OngoingEvaluationBuffer.Count;
                        index++)
                    {
                        ActiveGameplayEffect activeEffect =
                            m_OngoingEvaluationBuffer[index];

                        if (
                            !m_ActiveByHandle.TryGetValue(
                                activeEffect.Handle,
                                out ActiveGameplayEffect registeredEffect) ||
                            !ReferenceEquals(
                                activeEffect,
                                registeredEffect))
                        {
                            continue;
                        }

                        UpdateOngoingTagRequirements(
                            activeEffect);
                    }
                }
            }
            finally
            {
                m_OngoingEvaluationBuffer.Clear();
                m_PendingOngoingEvaluations.Clear();

                m_IsEvaluatingOngoingRequirements =
                    false;
            }
        }

        /// <summary>
        /// Returns whether an active effect currently satisfies its ongoing tag requirements.
        /// </summary>
        private bool AreOngoingTagRequirementsSatisfied(
            ActiveGameplayEffect activeEffect)
        {
            GameplayEffectTags effectTags =
                activeEffect.Spec
                    .Definition
                    .gameplayEffectTags;

            return
                m_Owner.HasAllMatchingGameplayTags(
                    effectTags.OngoingTagRequirementsRequired) &&
                !m_Owner.HasAnyMatchingGameplayTags(
                    effectTags.OngoingTagRequirementsForbidden);
        }

        /// <summary>
        /// Applies or removes active effect state when its inhibition changes.
        /// </summary>
        private void UpdateOngoingTagRequirements(
            ActiveGameplayEffect activeEffect)
        {
            bool shouldBeInhibited =
                !AreOngoingTagRequirementsSatisfied(
                    activeEffect);

            if (
                activeEffect.IsInhibited ==
                shouldBeInhibited)
            {
                return;
            }

            if (shouldBeInhibited)
            {
                activeEffect.IsInhibited =
                    true;

                RemoveAppliedModifiers(
                    activeEffect);

                return;
            }

            ApplyModifiers(
                activeEffect);
        }
    }
}