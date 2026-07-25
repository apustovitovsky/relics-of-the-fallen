using System;
using System.Collections.Generic;
using GAS;

namespace RelicsOfTheFallen.AbilitySystem
{
    public sealed class AbilityActivationTracker
    {
        private readonly Dictionary<string, AbilityActivationRecord> _recordsByActivationId = new();
        private readonly Dictionary<string, PredictedEffectRecord> _effectsByPredictionId = new();


        public event Action<AbilityActivationRecord> StateChanged;
        private long _nextSequence;

        public AbilityActivationRecord BeginRequest(
            string activationId,
            GameplayAbility ability,
            AbilitySystemComponent source,
            AbilitySystemComponent target)
        {
            var record =
                new AbilityActivationRecord(
                    activationId,
                    ability,
                    source,
                    target);

            if (!_recordsByActivationId.TryAdd(activationId, record))
            {
                throw new InvalidOperationException(
                    $"Activation '{activationId}' is already registered.");
            }

            Notify(record);
            return record;
        }

        public bool MarkPredicted(string activationId)
        {
            return SetState(
                activationId,
                AbilityActivationState.Predicted);
        }

        public bool Confirm(string activationId)
        {
            return SetState(
                activationId,
                AbilityActivationState.Confirmed);
        }

        public bool Reject(
            string activationId,
            out AbilityActivationRecord record)
        {
            if (!_recordsByActivationId.TryGetValue(activationId, out record))
                return false;

            record.State = AbilityActivationState.Rejected;
            Notify(record);
            return true;
        }

        public bool Complete(string activationId)
        {
            return SetState(
                activationId,
                AbilityActivationState.Completed);
        }

        public bool TryGet(
            string activationId,
            out AbilityActivationRecord record)
        {
            return _recordsByActivationId.TryGetValue(activationId, out record);
        }

        public bool IsPredicted(string activationId)
        {
            return
                _recordsByActivationId.TryGetValue(activationId, out var record)
                && record.State is AbilityActivationState.Predicted
                or AbilityActivationState.Confirmed;
        }

        /// <summary>
        /// Returns predicted effects registered for the specified ability activation.
        /// </summary>
        public IReadOnlyList<PredictedEffectRecord>
            GetEffectsForActivation(
                string activationId)
        {
            if (string.IsNullOrEmpty(
                    activationId))
            {
                throw new ArgumentException(
                    "Activation ID is required.",
                    nameof(activationId));
            }

            if (!_recordsByActivationId.TryGetValue(
                    activationId,
                    out AbilityActivationRecord activation))
            {
                throw new InvalidOperationException(
                    $"Activation '{activationId}' is not registered.");
            }

            var result =
                new List<PredictedEffectRecord>(
                    activation.Effects);

            result.Sort(
                (left, right) =>
                    left.Sequence.CompareTo(
                        right.Sequence));

            return result;
        }

        /// <summary>
        /// Returns ordered predicted effects that still participate in reconciliation for the target.
        /// </summary>
        public IReadOnlyList<PredictedEffectRecord>
            GetPendingEffectsFor(
                AbilitySystemComponent target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(
                    nameof(target));
            }

            var result =
                new List<PredictedEffectRecord>();

            foreach (PredictedEffectRecord effect
                     in _effectsByPredictionId.Values)
            {
                if (!ReferenceEquals(
                        effect.Target,
                        target))
                {
                    continue;
                }

                if (!_recordsByActivationId.TryGetValue(
                        effect.ActivationId,
                        out AbilityActivationRecord activation))
                {
                    throw new InvalidOperationException(
                        $"Predicted effect '{effect.PredictionId}' " +
                        "has no registered activation.");
                }

                if (activation.State is not
                    (AbilityActivationState.Predicted or
                     AbilityActivationState.Confirmed))
                {
                    continue;
                }

                result.Add(
                    effect);
            }

            result.Sort(
                (left, right) =>
                    left.Sequence.CompareTo(
                        right.Sequence));

            return result;
        }

        /// <summary>
        /// Tracks a predicted gameplay effect using its stable ability slot.
        /// </summary>
        public bool TrackEffect(
            GameplayEffect effect,
            GameplayEffectSlot slot,
            double appliedAtNetworkTime)
        {
            if (effect == null ||
                !slot.IsValid ||
                string.IsNullOrEmpty(
                    effect.applicationGUID))
            {
                return false;
            }

            if (!_recordsByActivationId.TryGetValue(
                    effect.applicationGUID,
                    out var activation))
            {
                return false;
            }

            string predictionId =
                BuildPredictionId(
                    effect.applicationGUID,
                    slot);

            if (_effectsByPredictionId.ContainsKey(predictionId))
                return false;

            var effectRecord =
                new PredictedEffectRecord(
                    predictionId,
                    activation.Id,
                    slot,
                    ++_nextSequence,
                    effect,
                    appliedAtNetworkTime);

            _effectsByPredictionId.Add(predictionId, effectRecord);
            activation.Effects.Add(effectRecord);

            return true;
        }

        /// <summary>
        /// Matches an authoritative gameplay effect with its locally predicted effect slot.
        /// </summary>
        public bool TryConfirmEffect(
            GameplayEffect authoritativeEffect,
            GameplayEffectSlot slot,
            out PredictedEffectRecord predictedEffect)
        {

            predictedEffect = null;

            if (authoritativeEffect == null ||
                !slot.IsValid ||
                string.IsNullOrEmpty(
                    authoritativeEffect.applicationGUID))
            {
                return false;
            }

            if (string.IsNullOrEmpty(
                    authoritativeEffect.applicationGUID))
            {
                return false;
            }

            if (!_recordsByActivationId.TryGetValue(
                    authoritativeEffect.applicationGUID,
                    out var activation))
            {
                return false;
            }

            string predictionId =
                BuildPredictionId(
                    authoritativeEffect.applicationGUID,
                    slot);

            if (!_effectsByPredictionId.TryGetValue(
                    predictionId,
                    out predictedEffect))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(
                    predictedEffect.EffectId))
            {
                string localEffectGuid =
                    predictedEffect.EffectId;

                var localEffect =
                    predictedEffect.Target
                        .appliedGameplayEffects
                        .Find(effect =>
                            effect.guid == localEffectGuid);

                if (localEffect == null)
                {
                    _effectsByPredictionId.Remove(predictionId);
                    activation.Effects.Remove(predictedEffect);
                    predictedEffect = null;

                    return false;
                }

                localEffect.guid =
                    authoritativeEffect.guid;

                predictedEffect.EffectId =
                    authoritativeEffect.guid;
            }

            _effectsByPredictionId.Remove(predictionId);
            activation.Effects.Remove(predictedEffect);

            return true;
        }

        /// <summary>
        /// Removes the prediction record associated with a finished persistent effect.
        /// </summary>
        public bool RemoveEffect(
            string effectId)
        {
            if (string.IsNullOrEmpty(
                    effectId))
            {
                return false;
            }

            PredictedEffectRecord matchedEffect =
                null;

            foreach (PredictedEffectRecord effect
                     in _effectsByPredictionId.Values)
            {
                if (effect.EffectId != effectId)
                {
                    continue;
                }

                matchedEffect =
                    effect;

                break;
            }

            if (matchedEffect == null)
            {
                return false;
            }

            if (!_effectsByPredictionId.Remove(
                    matchedEffect.PredictionId))
            {
                return false;
            }

            if (_recordsByActivationId.TryGetValue(
                    matchedEffect.ActivationId,
                    out AbilityActivationRecord activation))
            {
                activation.Effects.Remove(
                    matchedEffect);
            }

            return true;
        }

        public void Remove(string activationId)
        {
            if (!_recordsByActivationId.Remove(
                    activationId,
                    out var record))
            {
                return;
            }

            foreach (var effect in record.Effects)
                _effectsByPredictionId.Remove(effect.PredictionId);
        }

        private bool SetState(
            string activationId,
            AbilityActivationState state)
        {
            if (!_recordsByActivationId.TryGetValue(
                    activationId,
                    out var record))
            {
                return false;
            }

            record.State = state;
            Notify(record);
            return true;
        }

        private void Notify(AbilityActivationRecord record)
        {
            StateChanged?.Invoke(record);
        }

        /// <summary>
        /// Builds a prediction identifier from an activation and its stable effect slot.
        /// </summary>
        private static string BuildPredictionId(
            string activationId,
            GameplayEffectSlot slot)
        {
            if (string.IsNullOrEmpty(
                    activationId) ||
                !slot.IsValid)
            {
                throw new ArgumentException(
                    "Activation ID and gameplay effect slot are required.");
            }

            return
                $"{activationId}:{slot}";
        }
    }
}