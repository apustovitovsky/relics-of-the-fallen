using System;

namespace GAS
{
    [Serializable]
    public struct AttributeState
    {
        public float BaseValue;

        public float CurrentValue;

        public AttributeState(
            float baseValue,
            float currentValue)
        {
            BaseValue =
                baseValue;

            CurrentValue =
                currentValue;
        }
    }
}