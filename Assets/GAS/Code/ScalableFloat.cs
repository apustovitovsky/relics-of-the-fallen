using System;
using UnityEngine;

namespace GAS
{
    /// <summary>
    /// Represents a numerical value optionally scaled by an evaluated level curve.
    /// </summary>
    [Serializable]
    public struct ScalableFloat
    {
        [field: SerializeField]
        public float Value
        {
            get;
            private set;
        }

        [field: SerializeField]
        public AnimationCurve Curve
        {
            get;
            private set;
        }

        public ScalableFloat(
            float value)
        {
            Value = value;
            Curve = null;
        }

        /// <summary>
        /// Returns the raw value multiplied by the optional curve value at the given level.
        /// </summary>
        public readonly float GetValueAtLevel(
            float level)
        {
            if (
                Curve == null ||
                Curve.length == 0)
            {
                return Value;
            }

            return
                Value *
                Curve.Evaluate(
                    level);
        }
    }
}