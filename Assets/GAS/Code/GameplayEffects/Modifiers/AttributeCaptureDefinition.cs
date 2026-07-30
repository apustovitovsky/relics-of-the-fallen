using System;
using UnityEngine;

namespace GAS
{
    public enum AttributeCaptureSource
    {
        Source,
        Target
    }

    public enum AttributeCaptureValueType
    {
        BaseValue,
        CurrentValue
    }

    [Serializable]
    public sealed class AttributeCaptureDefinition
    {
        [SerializeField]
        private AttributeName attribute;

        [SerializeField]
        private AttributeCaptureSource source;

        [SerializeField]
        private AttributeCaptureValueType valueType;

        [SerializeField]
        private bool snapshot;

        public AttributeName Attribute =>
            attribute;

        public AttributeCaptureSource Source =>
            source;

        public AttributeCaptureValueType ValueType =>
            valueType;

        public bool Snapshot =>
            snapshot;
    }
}