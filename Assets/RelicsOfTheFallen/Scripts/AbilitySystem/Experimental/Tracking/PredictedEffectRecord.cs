using System;
using GAS;

namespace RelicsOfTheFallen.AbilitySystem
{
    public sealed class PredictedEffectRecord
    {
        public string PredictionId { get; }

        public string ActivationId { get; }

        public GameplayEffectSlot Slot { get; }

        public string EffectId { get; internal set; }

        public GameplayEffect EffectSpec { get; }

        public double AppliedAtNetworkTime { get; }

        public long Sequence { get; }

        public AbilitySystemComponent Source { get; }

        public AbilitySystemComponent Target { get; }

        /// <summary>
        /// Creates a prediction record for an applied gameplay effect.
        /// </summary>
        public PredictedEffectRecord(
            string predictionId,
            string activationId,
            GameplayEffectSlot slot,
            long sequence,
            GameplayEffect predictedEffect,
            double appliedAtNetworkTime)
        {
            if (string.IsNullOrEmpty(
                    predictionId))
            {
                throw new ArgumentException(
                    "Prediction ID is required.",
                    nameof(predictionId));
            }

            if (string.IsNullOrEmpty(
                    activationId))
            {
                throw new ArgumentException(
                    "Activation ID is required.",
                    nameof(activationId));
            }

            if (!slot.IsValid)
            {
                throw new ArgumentException(
                    "Gameplay effect slot is required.",
                    nameof(slot));
            }

            if (predictedEffect == null)
            {
                throw new ArgumentNullException(
                    nameof(predictedEffect));
            }

            PredictionId =
                predictionId;

            ActivationId =
                activationId;

            Slot =
                slot;

            Sequence =
                sequence;

            Source =
                predictedEffect.source;

            Target =
                predictedEffect.target;

            EffectSpec =
                predictedEffect.Instantiate();

            EffectSpec.guid =
                null;

            AppliedAtNetworkTime =
                appliedAtNetworkTime;

            EffectId =
                predictedEffect.durationType ==
                GameplayEffectDurationType.Instant
                    ? null
                    : predictedEffect.guid;
        }
    }
}