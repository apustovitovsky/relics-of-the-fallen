using System;
using System.Collections.Generic;
using GAS;

namespace RelicsOfTheFallen.AbilitySystem
{
    internal sealed class AttributeReconciler :
        IAttributeReconciler
    {
        private readonly AbilityActivationTracker _tracker;
        private readonly GameplayEffectProcessor _effectProcessor;

        private readonly HashSet<AbilitySystemComponent> _targetsInProgress = new();

        public AttributeReconciler(
            AbilityActivationTracker tracker,
            GameplayEffectProcessor effectProcessor)
        {
            _tracker =
                tracker ??
                throw new ArgumentNullException(
                    nameof(tracker));

            _effectProcessor =
                effectProcessor ??
                throw new ArgumentNullException(
                    nameof(effectProcessor));
        }

        /// <summary>
        /// Restores authoritative attributes after a predicted ability activation is rejected.
        /// </summary>
        public bool TryReconcile(
            AbilitySystemComponent target,
            string rejectedActivationId,
            AttributeSnapshot snapshot)
        {
            if (target == null ||
                string.IsNullOrEmpty(
                    rejectedActivationId) ||
                snapshot == null ||
                !snapshot.IsCompleteFor(
                    target))
            {
                return false;
            }

            if (!_tracker.TryGet(
                    rejectedActivationId,
                    out AbilityActivationRecord activation) ||
                activation.State !=
                AbilityActivationState.Rejected)
            {
                return false;
            }

            IReadOnlyList<PredictedEffectRecord>
                pendingEffects =
                    _tracker.GetPendingEffectsFor(
                        target);

            IReadOnlyList<PredictedEffectRecord>
                rejectedEffects =
                    _tracker.GetEffectsForActivation(
                        rejectedActivationId);

            var suspendedEffectIds =
                new List<string>();

            if (!_targetsInProgress.Add(
                target))
            {
                return false;
            }

            try
            {
                SuspendPersistentEffects(
                    target,
                    pendingEffects,
                    suspendedEffectIds);

                RemoveRejectedPersistentEffects(
                    target,
                    rejectedEffects);

                RestoreAttributes(
                    target,
                    snapshot);

                RestoreEffects(
                    target,
                    pendingEffects,
                    suspendedEffectIds);

                _tracker.Remove(
                    rejectedActivationId);

                return true;
            }
            finally
            {
                try
                {
                    ResumeRemainingEffects(
                        target,
                        suspendedEffectIds);
                }
                finally
                {
                    _targetsInProgress.Remove(
                        target);
                }
            }
        }

        /// <summary>
        /// Suspends active persistent predictions and discards records for effects that have already ended.
        /// </summary>
        private void SuspendPersistentEffects(
            AbilitySystemComponent target,
            IReadOnlyList<PredictedEffectRecord> effects,
            List<string> suspendedEffectIds)
        {
            foreach (PredictedEffectRecord effect
                     in effects)
            {
                if (effect.EffectSpec.durationType ==
                    GameplayEffectDurationType.Instant)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(
                        effect.EffectId))
                {
                    throw new InvalidOperationException(
                        $"Persistent predicted effect " +
                        $"'{effect.PredictionId}' has no runtime ID.");
                }

                if (!target.SuspendGameplayEffect(
                        effect.EffectId))
                {
                    _tracker.RemoveEffect(
                        effect.EffectId);

                    continue;
                }

                suspendedEffectIds.Add(
                    effect.EffectId);
            }
        }

        /// <summary>
        /// Permanently removes persistent effects created by the rejected activation.
        /// </summary>
        private static void RemoveRejectedPersistentEffects(
            AbilitySystemComponent target,
            IReadOnlyList<PredictedEffectRecord> rejectedEffects)
        {
            for (int index =
                     rejectedEffects.Count - 1;
                 index >= 0;
                 index--)
            {
                PredictedEffectRecord effect =
                    rejectedEffects[index];

                if (effect.EffectSpec.durationType ==
                    GameplayEffectDurationType.Instant)
                {
                    continue;
                }

                if (!target.RemoveGameplayEffect(
                        effect.EffectId,
                        GameplayEffectNotificationOptions.CuesOnly))
                {
                    throw new InvalidOperationException(
                        $"Rejected persistent effect " +
                        $"'{effect.EffectId}' could not be removed.");
                }
            }
        }

        private static void RestoreAttributes(
            AbilitySystemComponent target,
            AttributeSnapshot snapshot)
        {
            foreach (AttributeValueSnapshot value
                     in snapshot.Values)
            {
                if (!target.attributesDictionary.TryGetValue(
                        value.AttributeName,
                        out GAS.Attribute attribute))
                {
                    throw new InvalidOperationException(
                        $"Attribute '{value.AttributeName}' " +
                        $"does not exist on ASC '{target.name}'.");
                }

                attribute.baseValue =
                    value.BaseValue;
            }

            target.RefreshAttributesModifiers(
                causeEffect: null,
                notifyEvents: false);
        }

        /// <summary>
        /// Restores pending predicted effects after authoritative attributes are applied.
        /// </summary>
        private void RestoreEffects(
            AbilitySystemComponent target,
            IReadOnlyList<PredictedEffectRecord> pendingEffects,
            List<string> suspendedEffectIds)
        {
            foreach (PredictedEffectRecord effect
                     in pendingEffects)
            {
                if (effect.EffectSpec.durationType ==
                    GameplayEffectDurationType.Instant)
                {
                    GameplayEffect replayedEffect =
                        _effectProcessor.ApplyEffect(
                            target,
                            effect.Source,
                            effect.EffectSpec,
                            effect.ActivationId,
                            GameplayEffectApplicationOptions.Silent) ?? throw new InvalidOperationException(
                            $"Instant predicted effect " +
                            $"'{effect.PredictionId}' could not be replayed.");
                    continue;
                }

                if (!suspendedEffectIds.Remove(
                    effect.EffectId))
                {
                    continue;
                }

                if (!target.ResumeGameplayEffect(
                        effect.EffectId))
                {
                    throw new InvalidOperationException(
                        $"Persistent predicted effect " +
                        $"'{effect.EffectId}' could not be resumed.");
                }

                if (!target.ResumeGameplayEffect(
                    effect.EffectId))
                {
                    throw new InvalidOperationException(
                        $"Persistent predicted effect " +
                        $"'{effect.EffectId}' could not be resumed.");
                }
            }
        }
        private static void ResumeRemainingEffects(
            AbilitySystemComponent target,
            List<string> suspendedEffectIds)
        {
            for (int index =
                     suspendedEffectIds.Count - 1;
                 index >= 0;
                 index--)
            {
                target.ResumeGameplayEffect(
                    suspendedEffectIds[index]);
            }
        }
    }
}