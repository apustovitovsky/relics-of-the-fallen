using System;
using System.Collections.Generic;
using GAS;

namespace RelicsOfTheFallen.AbilitySystem
{
    public sealed class AttributeSnapshot
    {
        public AbilitySystemComponent Target { get; }

        public IReadOnlyList<AttributeValueSnapshot> Values { get; }

        /// <summary>
        /// Creates a snapshot from authoritative base values received for the local target.
        /// </summary>
        private static AttributeSnapshot
            FromAuthoritativeBaseValues(
                AbilitySystemComponent target,
                IReadOnlyList<AttributeValueSnapshot> values)
        {
            if (target == null)
            {
                throw new ArgumentNullException(
                    nameof(target));
            }

            if (values == null)
            {
                throw new ArgumentNullException(
                    nameof(values));
            }

            return new AttributeSnapshot(
                target,
                values);
        }

        /// <summary>
        /// Captures the authoritative base values of every attribute on the target.
        /// </summary>
        public static AttributeSnapshot Capture(
            AbilitySystemComponent target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(
                    nameof(target));
            }

            var values =
                new List<AttributeValueSnapshot>(
                    target.attributes.Count);

            foreach (GAS.Attribute attribute
                     in target.attributes)
            {
                if (attribute == null ||
                    attribute.attributeName == null)
                {
                    throw new InvalidOperationException(
                        $"ASC '{target.name}' contains an invalid attribute.");
                }

                values.Add(
                    new AttributeValueSnapshot(
                        attribute.attributeName.name,
                        attribute.baseValue));
            }

            return new AttributeSnapshot(
                target,
                values);
        }

        public AttributeSnapshot(
            AbilitySystemComponent target,
            IReadOnlyList<AttributeValueSnapshot> values)
        {
            Target =
                target ??
                throw new ArgumentNullException(
                    nameof(target));

            Values =
                new List<AttributeValueSnapshot>(
                    values ??
                    throw new ArgumentNullException(
                        nameof(values)))
                .AsReadOnly();
        }

        public bool BelongsTo(
            AbilitySystemComponent target)
        {
            return ReferenceEquals(
                Target,
                target);
        }

        /// <summary>
        /// Determines whether the snapshot contains every attribute of the specified target exactly once.
        /// </summary>
        public bool IsCompleteFor(
            AbilitySystemComponent target)
        {
            if (!BelongsTo(target) ||
                Values.Count !=
                target.attributesDictionary.Count)
            {
                return false;
            }

            var attributeNames =
                new HashSet<string>();

            foreach (AttributeValueSnapshot value
                in Values)
            {
                if (!attributeNames.Add(
                    value.AttributeName))
                {
                    return false;
                }

                if (!target.attributesDictionary.ContainsKey(
                    value.AttributeName))
                {
                    return false;
                }
            }

            return true;
        }
    }

    public readonly struct AttributeValueSnapshot
    {
        public string AttributeName { get; }

        public float BaseValue { get; }

        public AttributeValueSnapshot(
            string attributeName,
            float baseValue)
        {
            if (string.IsNullOrEmpty(
                    attributeName))
            {
                throw new ArgumentException(
                    "Attribute name is required.",
                    nameof(attributeName));
            }

            if (float.IsNaN(baseValue) ||
                float.IsInfinity(baseValue))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(baseValue));
            }

            AttributeName =
                attributeName;

            BaseValue =
                baseValue;
        }
    }
}