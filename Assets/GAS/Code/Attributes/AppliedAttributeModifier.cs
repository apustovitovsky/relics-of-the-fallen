using System;

namespace GAS
{
    public readonly struct AppliedAttributeModifier
    {
        public Attribute TargetAttribute { get; }

        public AttributeModifierHandle Handle { get; }

        /// <summary>
        /// Links an attribute to one removable aggregator modifier.
        /// </summary>
        public AppliedAttributeModifier(
            Attribute targetAttribute,
            AttributeModifierHandle handle)
        {
            TargetAttribute =
                targetAttribute ??
                throw new ArgumentNullException(
                    nameof(targetAttribute));

            if (!handle.IsValid)
            {
                throw new ArgumentException(
                    "An applied modifier requires a valid handle.",
                    nameof(handle));
            }

            Handle = handle;
        }
    }
}