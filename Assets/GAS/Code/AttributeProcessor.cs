using System;
using UnityEngine;

namespace GAS
{
    [Serializable]
    public class AttributeProcessor
    {
        [ReadOnly]
        public string name;

        /// <summary>
        /// Constrains a proposed attribute base value before it is committed.
        /// </summary>
        public virtual void PreAttributeBaseChange(
            Attribute attribute,
            ref float newValue,
            AbilitySystemComponent abilitySystem)
        {
        }

        /// <summary>
        /// Handles a gameplay effect after it modifies an attribute base value.
        /// </summary>
        public virtual void PostGameplayEffectExecute(
            GameplayEffectModCallbackData data)
        {
        }
    }

    [Serializable]
    public sealed class Clamper :
        AttributeProcessor
    {
        public float min;

        public float max;

        public AttributeName clampedAttributeName;

        /// <summary>
        /// Clamps a configured attribute between constant bounds.
        /// </summary>
        public override void PreAttributeBaseChange(
            Attribute attribute,
            ref float newValue,
            AbilitySystemComponent abilitySystem)
        {
            if (
                attribute.attributeName !=
                clampedAttributeName)
            {
                return;
            }

            newValue =
                Mathf.Clamp(
                    newValue,
                    min,
                    max);
        }
    }

    [Serializable]
    public sealed class ClamperMaxAttributeValue :
        AttributeProcessor
    {
        public AttributeName max;

        public AttributeName clampedAttributeName;

        /// <summary>
        /// Clamps a configured attribute to another attribute maximum.
        /// </summary>
        public override void PreAttributeBaseChange(
            Attribute attribute,
            ref float newValue,
            AbilitySystemComponent abilitySystem)
        {
            if (
                attribute.attributeName !=
                clampedAttributeName)
            {
                return;
            }

            Attribute maximumAttribute =
                abilitySystem.GetAttribute(
                    max);

            newValue =
                Mathf.Min(
                    newValue,
                    maximumAttribute.CurrentValue);
        }
    }

    [Serializable]
    public sealed class ClamperMinAttributeValue :
        AttributeProcessor
    {
        public AttributeName min;

        public AttributeName clampedAttributeName;

        /// <summary>
        /// Clamps a configured attribute to another attribute minimum.
        /// </summary>
        public override void PreAttributeBaseChange(
            Attribute attribute,
            ref float newValue,
            AbilitySystemComponent abilitySystem)
        {
            if (
                attribute.attributeName !=
                clampedAttributeName)
            {
                return;
            }

            Attribute minimumAttribute =
                abilitySystem.GetAttribute(
                    min);

            newValue =
                Mathf.Max(
                    newValue,
                    minimumAttribute.CurrentValue);
        }
    }
}