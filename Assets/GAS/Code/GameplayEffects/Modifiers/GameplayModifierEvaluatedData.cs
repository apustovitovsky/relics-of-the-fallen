using System;

namespace GAS
{
    public readonly struct GameplayModifierEvaluatedData
    {
        public AttributeName Attribute { get; }

        public AttributeModifierOperation Operation { get; }

        public float Magnitude { get; }

        public ActiveGameplayEffectHandle Handle { get; }

        public bool IsValid =>
            Attribute != null;

        /// <summary>
        /// Describes one evaluated attribute modification.
        /// </summary>
        internal GameplayModifierEvaluatedData(
            AttributeName attribute,
            AttributeModifierOperation operation,
            float magnitude,
            ActiveGameplayEffectHandle handle)
        {
            Attribute = attribute != null
                ? attribute
                : throw new ArgumentNullException(
                    nameof(attribute));

            Operation =
                operation;

            Magnitude =
                magnitude;

            Handle =
                handle;
        }
    }
}