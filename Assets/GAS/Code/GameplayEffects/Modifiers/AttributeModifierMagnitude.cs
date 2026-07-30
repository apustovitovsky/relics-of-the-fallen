using System;
using System.Collections.Generic;
using UnityEngine;

namespace GAS
{
    [Serializable]
    public abstract class AttributeModifierMagnitude
    {
        /// <summary>
        /// Calculates a modifier magnitude for one gameplay effect specification.
        /// </summary>
        public abstract float Calculate(
            GameplayEffectSpec spec);

        /// <summary>
        /// Returns the attribute captures required by this magnitude calculation.
        /// </summary>
        internal virtual IEnumerable<AttributeCaptureDefinition>
            GetAttributeCaptures()
        {
            yield break;
        }
    }

    [Serializable]
    public sealed class ConstantMagnitude :
        AttributeModifierMagnitude
    {
        [SerializeField]
        private float value;

        /// <summary>
        /// Creates an empty constant magnitude for Unity serialization.
        /// </summary>
        public ConstantMagnitude()
        {
        }

        /// <summary>
        /// Creates a constant magnitude with the specified value.
        /// </summary>
        public ConstantMagnitude(
            float value)
        {
            this.value =
                value;
        }

        /// <summary>
        /// Returns the configured constant magnitude.
        /// </summary>
        public override float Calculate(
            GameplayEffectSpec spec)
        {
            return value;
        }
    }

    [Serializable]
    public sealed class CurveMagnitude :
        AttributeModifierMagnitude
    {
        [SerializeField]
        private AnimationCurve curve =
            AnimationCurve.Linear(0f, 0f, 1f, 1f);

        /// <summary>
        /// Evaluates the configured curve using the gameplay effect level.
        /// </summary>
        public override float Calculate(
            GameplayEffectSpec spec)
        {
            if (spec == null)
            {
                throw new ArgumentNullException(
                    nameof(spec));
            }

            return curve.Evaluate(
                spec.Level);
        }
    }

    [Serializable]
    public sealed class SetByCallerMagnitude :
        AttributeModifierMagnitude
    {
        [SerializeField]
        private GameplayTag tag;

        /// <summary>
        /// Returns the runtime magnitude stored in the gameplay effect specification.
        /// </summary>
        public override float Calculate(
            GameplayEffectSpec spec)
        {
            if (spec == null)
            {
                throw new ArgumentNullException(
                    nameof(spec));
            }

            return spec.GetSetByCallerMagnitude(
                tag);
        }
    }

    [Serializable]
    public sealed class AttributeBasedMagnitude :
    AttributeModifierMagnitude
    {
        [SerializeField]
        private AttributeCaptureDefinition capture =
            new();

        [SerializeField]
        private float coefficient =
            1f;

        [SerializeField]
        private float preMultiplyAdditiveValue;

        [SerializeField]
        private float postMultiplyAdditiveValue;

        /// <summary>
        /// Returns the attribute capture required by this magnitude calculation.
        /// </summary>
        internal override IEnumerable<AttributeCaptureDefinition>
            GetAttributeCaptures()
        {
            yield return capture;
        }

        /// <summary>
        /// Calculates a magnitude from the captured attribute value.
        /// </summary>
        public override float Calculate(
            GameplayEffectSpec spec)
        {
            if (spec == null)
            {
                throw new ArgumentNullException(
                    nameof(spec));
            }

            float capturedValue =
                spec.GetCapturedAttributeMagnitude(
                    capture);

            return
                (capturedValue +
                preMultiplyAdditiveValue) *
                coefficient +
                postMultiplyAdditiveValue;
        }
    }
}