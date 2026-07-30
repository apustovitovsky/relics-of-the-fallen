using System;

namespace GAS
{
    public sealed class AttributeModifierSpec
    {
        public AttributeModifierDefinition Definition { get; }

        public bool HasEvaluatedMagnitude
        {
            get;
            private set;
        }

        public float EvaluatedMagnitude
        {
            get;
            private set;
        }

        /// <summary>
        /// Creates runtime modifier data without evaluating its magnitude.
        /// </summary>
        public AttributeModifierSpec(
            AttributeModifierDefinition definition)
        {
            Definition = definition ??
                throw new ArgumentNullException(
                    nameof(definition));
        }

        /// <summary>
        /// Copies runtime modifier data for a separate effect application.
        /// </summary>
        internal AttributeModifierSpec(
            AttributeModifierSpec source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(
                    nameof(source));
            }

            Definition =
                source.Definition;

            if (source.HasEvaluatedMagnitude)
            {
                SetEvaluatedMagnitude(
                    source.EvaluatedMagnitude);
            }
        }

        /// <summary>
        /// Stores the latest evaluated magnitude for this runtime modifier.
        /// </summary>
        internal void SetEvaluatedMagnitude(
            float magnitude)
        {
            EvaluatedMagnitude =
                magnitude;

            HasEvaluatedMagnitude =
                true;
        }
    }
}