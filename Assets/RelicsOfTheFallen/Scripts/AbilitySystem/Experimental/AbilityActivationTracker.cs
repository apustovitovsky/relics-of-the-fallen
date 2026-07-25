using System;
using System.Collections.Generic;
using GAS;

namespace RelicsOfTheFallen.AbilitySystem
{
    public sealed class AbilityActivationTracker
    {
        private readonly Dictionary<string, AbilityActivationRecord> _records = new();
        private readonly Dictionary<string, PredictedEffectRecord> _effects = new();

        public event Action<AbilityActivationRecord> StateChanged;

        public AbilityActivationRecord BeginRequest(
            string activationId,
            GameplayAbility ability,
            AbilitySystemComponent source,
            AbilitySystemComponent target)
        {
            var record = new AbilityActivationRecord(
                activationId,
                ability,
                source,
                target);

            if (!_records.TryAdd(activationId, record))
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
            if (!_records.TryGetValue(activationId, out record))
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
            return _records.TryGetValue(activationId, out record);
        }

        public bool IsPredicted(string activationId)
        {
            return _records.TryGetValue(activationId, out var record) &&
                   record.State is AbilityActivationState.Predicted
                       or AbilityActivationState.Confirmed;
        }

        public bool TrackEffect(GameplayEffect effect)
        {
            if (string.IsNullOrEmpty(effect.applicationGUID))
                return false;

            if (!_records.TryGetValue(
                    effect.applicationGUID,
                    out var activation))
            {
                return false;
            }

            var key = BuildEffectKey(activation, effect);

            if (_effects.ContainsKey(key))
                return false;

            var effectRecord = new PredictedEffectRecord(
                key,
                effect,
                effect.target);

            _effects.Add(key, effectRecord);
            activation.Effects.Add(effectRecord);

            return true;
        }

        public bool TryConfirmEffect(
            GameplayEffect authoritativeEffect,
            out PredictedEffectRecord predictedEffect)
        {
            predictedEffect = null;

            if (string.IsNullOrEmpty(authoritativeEffect.applicationGUID))
                return false;

            if (!_records.TryGetValue(
                    authoritativeEffect.applicationGUID,
                    out var activation))
            {
                return false;
            }

            var key = BuildEffectKey(
                activation,
                authoritativeEffect);

            if (!_effects.Remove(key, out predictedEffect))
                return false;

            // Локальный effect теперь получает серверный GUID.
            predictedEffect.LocalEffect.guid =
                authoritativeEffect.guid;

            activation.Effects.Remove(predictedEffect);
            return true;
        }

        public void Remove(string activationId)
        {
            if (!_records.Remove(
                    activationId,
                    out var record))
            {
                return;
            }

            foreach (var effect in record.Effects)
                _effects.Remove(effect.Key);
        }

        private bool SetState(
            string activationId,
            AbilityActivationState state)
        {
            if (!_records.TryGetValue(
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

        private static string BuildEffectKey(
            AbilityActivationRecord activation,
            GameplayEffect effect)
        {
            var ability = activation.Ability;

            if (ability.cost != null &&
                ability.cost.name == effect.name)
            {
                return $"COST_{activation.Id}";
            }

            if (ability.cooldown != null &&
                ability.cooldown.name == effect.name)
            {
                return $"CD_{activation.Id}";
            }

            var index = ability.effects.FindIndex(
                candidate => candidate.name == effect.name);

            return $"{index}_{activation.Id}";
        }
    }
}