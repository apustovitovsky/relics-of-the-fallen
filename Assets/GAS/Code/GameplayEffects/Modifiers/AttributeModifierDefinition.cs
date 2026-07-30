using System;
using UnityEngine;

namespace GAS
{
    public enum AttributeModifierOperation
    {
        Additive,
        Multiplicative,
        Division,
        Override
    }

    [Serializable]
    public sealed class AttributeModifierDefinition
    {
        [SerializeField]
        private AttributeName attribute;

        [SerializeField]
        private AttributeModifierOperation operation;

        [SerializeReference]
        private AttributeModifierMagnitude magnitude =
            new ConstantMagnitude();


        public AttributeName Attribute =>
            attribute;

        public AttributeModifierOperation Operation =>
            operation;

        public AttributeModifierMagnitude Magnitude =>
            magnitude;

        /// <summary>
        /// Creates an empty modifier definition for Unity serialization.
        /// </summary>
        public AttributeModifierDefinition()
        {
        }

        /// <summary>
        /// Creates a complete attribute modifier definition.
        /// </summary>
        public AttributeModifierDefinition(
            AttributeName attribute,
            AttributeModifierOperation operation,
            AttributeModifierMagnitude magnitude)
        {
            this.attribute =
                attribute != null
                    ? attribute
                    : throw new ArgumentNullException(
                        nameof(attribute));

            this.operation =
                operation;

            this.magnitude =
                magnitude ?? throw new ArgumentNullException(
                        nameof(magnitude));
        }
    }
}