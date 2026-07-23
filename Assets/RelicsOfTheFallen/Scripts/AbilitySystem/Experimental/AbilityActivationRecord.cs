using System;
using System.Collections.Generic;
using GAS;

namespace RelicsOfTheFallen.AbilitySystem
{
    public enum AbilityActivationState
    {
        Requested,
        Predicted,
        Confirmed,
        Rejected,
        Cancelled,
        Completed
    }

    public sealed class PredictedEffectRecord
    {
        public string Key { get; }
        public GameplayEffect LocalEffect { get; }
        public AbilitySystemComponent Target { get; }

        public PredictedEffectRecord(
            string key,
            GameplayEffect localEffect,
            AbilitySystemComponent target)
        {
            Key = key;
            LocalEffect = localEffect;
            Target = target;
        }
    }

    public sealed class AbilityActivationRecord
    {
        public string Id { get; }
        public GameplayAbility Ability { get; }
        public AbilitySystemComponent Source { get; }
        public AbilitySystemComponent Target { get; }

        public AbilityActivationState State { get; internal set; }
        public DateTime CreatedAtUtc { get; } = DateTime.UtcNow;

        public List<PredictedEffectRecord> Effects { get; } = new();

        public AbilityActivationRecord(
            string id,
            GameplayAbility ability,
            AbilitySystemComponent source,
            AbilitySystemComponent target)
        {
            Id = id;
            Ability = ability;
            Source = source;
            Target = target;
            State = AbilityActivationState.Requested;
        }
    }
}