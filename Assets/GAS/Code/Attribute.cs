using System;
using UnityEngine;

namespace GAS
{
    /// <summary>
    /// Stores an attribute base value and evaluates its current value through active modifiers.
    /// </summary>
    [Serializable]
    public class Attribute
    {
        [ReadOnly]
        public string name;

        [SerializeReference]
        public AttributeName attributeName;

        [SerializeField]
        private float baseValue;

        public float BaseValue =>
            baseValue;

        public float CurrentValue =>
            Aggregator.Evaluate(
                BaseValue);

        [NonSerialized]
        private AttributeModifierAggregator aggregator;

        private AttributeModifierAggregator Aggregator =>
            aggregator ??=
                new AttributeModifierAggregator();

        public Action<
            AttributeName,
            float,
            float,
            GameplayEffect> OnPostAttributeChange;

        /// <summary>
        /// Creates an empty attribute for Unity serialization.
        /// </summary>
        public Attribute()
        {
        }

        /// <summary>
        /// Creates an attribute with its initial base value.
        /// </summary>
        public Attribute(
            AttributeName attributeName,
            float baseValue)
        {
            this.attributeName =
                attributeName != null
                    ? attributeName
                    : throw new ArgumentNullException(
                        nameof(attributeName));

            name =
                attributeName.name;

            this.baseValue =
                baseValue;
        }

        /// <summary>
        /// Commits a new authoritative base value without applying lifecycle callbacks.
        /// </summary>
        internal void SetBaseValue(
            float newValue)
        {
            baseValue =
                newValue;
        }

        /// <summary>
        /// Adds a removable modifier to this attribute.
        /// </summary>
        internal AttributeModifierHandle AddModifier(
            float magnitude,
            AttributeModifierOperation operation)
        {
            return Aggregator.AddModifier(
                magnitude,
                operation);
        }

        /// <summary>
        /// Updates the magnitude of an existing attribute modifier.
        /// </summary>
        internal bool UpdateModifier(
            AttributeModifierHandle handle,
            float magnitude)
        {
            return Aggregator.UpdateModifier(
                handle,
                magnitude);
        }

        /// <summary>
        /// Removes an existing modifier from this attribute.
        /// </summary>
        internal bool RemoveModifier(
            AttributeModifierHandle handle)
        {
            return Aggregator.RemoveModifier(
                handle);
        }

        /// <summary>
        /// Returns the evaluated current value of this attribute.
        /// </summary>
        public float GetValue()
        {
            return CurrentValue;
        }
    }
}