using System;
using System.Collections.Generic;

namespace GAS
{

    public readonly struct AttributeModifierHandle :
        IEquatable<AttributeModifierHandle>
    {
        public ulong Value { get; }

        public bool IsValid =>
            Value != 0;

        /// <summary>
        /// Creates a typed identity for one aggregator modifier.
        /// </summary>
        internal AttributeModifierHandle(
            ulong value)
        {
            Value = value;
        }

        public bool Equals(
            AttributeModifierHandle other)
        {
            return Value ==
                other.Value;
        }

        public override bool Equals(
            object obj)
        {
            return
                obj is AttributeModifierHandle other &&
                Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public static bool operator ==(
            AttributeModifierHandle left,
            AttributeModifierHandle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            AttributeModifierHandle left,
            AttributeModifierHandle right)
        {
            return !left.Equals(right);
        }
    }

    public sealed class AttributeModifierAggregator
    {
        private readonly Dictionary<
            AttributeModifierHandle,
            ModifierEntry> modifiers =
                new();

        private ulong nextModifierId;

        private bool isDirty = true;

        private float cachedBaseValue;

        private float cachedCurrentValue;

        /// <summary>
        /// Adds a removable modifier and returns its typed handle.
        /// </summary>
        public AttributeModifierHandle AddModifier(
            float magnitude,
            AttributeModifierOperation operation)
        {
            AttributeModifierHandle handle =
                new(++nextModifierId);

            modifiers.Add(
                handle,
                new ModifierEntry(
                    magnitude,
                    operation));

            isDirty = true;

            return handle;
        }

        /// <summary>
        /// Updates an existing modifier without changing its identity.
        /// </summary>
        public bool UpdateModifier(
            AttributeModifierHandle handle,
            float magnitude)
        {
            if (
                !modifiers.TryGetValue(
                    handle,
                    out ModifierEntry modifier))
            {
                return false;
            }

            modifiers[handle] =
                new ModifierEntry(
                    magnitude,
                    modifier.Operation);

            isDirty = true;

            return true;
        }

        /// <summary>
        /// Removes one modifier and invalidates the evaluated value.
        /// </summary>
        public bool RemoveModifier(
            AttributeModifierHandle handle)
        {
            if (!modifiers.Remove(handle))
            {
                return false;
            }

            isDirty = true;

            return true;
        }

        /// <summary>
        /// Executes one modifier operation against an attribute base value.
        /// </summary>
        internal static float ExecuteModifierOnBaseValue(
            float baseValue,
            AttributeModifierOperation operation,
            float magnitude)
        {
            switch (operation)
            {
                case AttributeModifierOperation.Additive:
                    return baseValue +
                        magnitude;

                case AttributeModifierOperation.Multiplicative:
                    return baseValue *
                        magnitude;

                case AttributeModifierOperation.Division:
                    if (magnitude == 0f)
                    {
                        throw new InvalidOperationException(
                            "A division modifier magnitude cannot be zero.");
                    }

                    return baseValue /
                        magnitude;

                case AttributeModifierOperation.Override:
                    return magnitude;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(operation),
                        operation,
                        "Unsupported attribute modifier operation.");
            }
        }

        /// <summary>
        /// Evaluates the current value from the base and active modifiers.
        /// </summary>
        public float Evaluate(
            float baseValue)
        {
            if (
                !isDirty &&
                cachedBaseValue == baseValue)
            {
                return cachedCurrentValue;
            }

            float additive = 0f;
            float multiplicativeDelta = 0f;
            float divisionDelta = 0f;

            bool hasOverride = false;
            ulong latestOverrideId = 0;
            float overrideMagnitude = 0f;

            foreach (
                KeyValuePair<
                    AttributeModifierHandle,
                    ModifierEntry> pair
                in modifiers)
            {
                ModifierEntry modifier =
                    pair.Value;

                switch (modifier.Operation)
                {
                    case AttributeModifierOperation.Additive:
                        additive +=
                            modifier.Magnitude;

                        break;

                    case AttributeModifierOperation.Multiplicative:
                        multiplicativeDelta +=
                            modifier.Magnitude - 1f;

                        break;

                    case AttributeModifierOperation.Division:
                        divisionDelta +=
                            modifier.Magnitude - 1f;

                        break;

                    case AttributeModifierOperation.Override:
                        if (
                            !hasOverride ||
                            pair.Key.Value >
                            latestOverrideId)
                        {
                            hasOverride = true;

                            latestOverrideId =
                                pair.Key.Value;

                            overrideMagnitude =
                                modifier.Magnitude;
                        }

                        break;
                }
            }

            float multiplicative =
                1f + multiplicativeDelta;

            float division =
                1f + divisionDelta;

            float evaluatedValue =
                (baseValue + additive) * multiplicative / division;

            cachedBaseValue =
                baseValue;

            cachedCurrentValue =
                hasOverride
                    ? overrideMagnitude
                    : evaluatedValue;

            isDirty = false;

            return cachedCurrentValue;
        }

        private readonly struct ModifierEntry
        {
            public float Magnitude { get; }

            public AttributeModifierOperation Operation
            {
                get;
            }

            public ModifierEntry(
                float magnitude,
                AttributeModifierOperation operation)
            {
                Magnitude = magnitude;
                Operation = operation;
            }
        }
    }
}