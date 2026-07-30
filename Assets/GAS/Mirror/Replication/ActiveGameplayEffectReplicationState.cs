using Mirror;
using System;
using System.Collections.Generic;

namespace GAS.Mirror
{
    /// <summary>
    /// Contains the network state required to reconstruct one authoritative active gameplay effect.
    /// </summary>
    internal readonly struct ActiveGameplayEffectReplicationState :
        INetworkSerializable
    {
        internal const int k_MaxModifierCount = 64;
        private readonly float[] m_EvaluatedModifierMagnitudes;

        public AssetId DefinitionId
        {
            get;
        }

        public uint SourceNetworkId
        {
            get;
        }

        public float Level
        {
            get;
        }

        public float Duration
        {
            get;
        }

        public double StartServerWorldTime
        {
            get;
        }

        public PredictionKey PredictionKey
        {
            get;
        }

        public IReadOnlyList<float> EvaluatedModifierMagnitudes =>
            m_EvaluatedModifierMagnitudes ??
            Array.Empty<float>();

        public bool IsValid =>
            DefinitionId.IsValid;

        /// <summary>
        /// Creates the replicated state of one authoritative active gameplay effect.
        /// </summary>
        public ActiveGameplayEffectReplicationState(
            AssetId definitionId,
            uint sourceNetworkId,
            float level,
            float duration,
            double startServerWorldTime,
            PredictionKey predictionKey,
            float[] evaluatedModifierMagnitudes)
        {
            if (!definitionId.IsValid)
            {
                throw new ArgumentException(
                    "Gameplay effect definition asset ID must be valid.",
                    nameof(definitionId));
            }

            if (sourceNetworkId == 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(sourceNetworkId),
                    sourceNetworkId,
                    "Gameplay effect source network ID must be nonzero.");
            }

            if (
                float.IsNaN(duration) ||
                float.IsInfinity(duration) ||
                duration <
                GameplayEffectConstants.InfiniteDuration)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(duration),
                    duration,
                    "Replicated gameplay effect duration must be valid.");
            }

            if (evaluatedModifierMagnitudes == null)
            {
                throw new ArgumentNullException(
                    nameof(evaluatedModifierMagnitudes));
            }

            DefinitionId = definitionId;
            SourceNetworkId = sourceNetworkId;
            Level = level;
            Duration = duration;
            StartServerWorldTime = startServerWorldTime;
            PredictionKey = predictionKey;

            m_EvaluatedModifierMagnitudes =
                evaluatedModifierMagnitudes.Length == 0
                    ? Array.Empty<float>()
                    : (float[])evaluatedModifierMagnitudes.Clone();
        }

        /// <summary>
        /// Creates active gameplay effect replication state from network data.
        /// </summary>
        internal ActiveGameplayEffectReplicationState(
            NetworkReader reader)
            : this(
                reader.ReadAssetId(),
                reader.ReadUInt(),
                reader.ReadFloat(),
                reader.ReadFloat(),
                reader.ReadDouble(),
                reader.ReadPredictionKey(),
                ReadEvaluatedModifierMagnitudes(
                    reader))
        {
        }

        /// <summary>
        /// Serializes this active gameplay effect replication state into network data.
        /// </summary>
        public void Serialize(
            NetworkWriter writer)
        {
            if (!IsValid)
            {
                throw new InvalidOperationException(
                    "Active gameplay effect replication state must be valid.");
            }

            IReadOnlyList<float> magnitudes = EvaluatedModifierMagnitudes;

            if (magnitudes.Count > k_MaxModifierCount)
            {
                throw new InvalidOperationException(
                    "Active gameplay effect replication state has too many modifiers.");
            }

            writer.WriteAssetId(
                DefinitionId);

            writer.WriteUInt(
                SourceNetworkId);

            writer.WriteFloat(
                Level);

            writer.WriteFloat(
                Duration);

            writer.WriteDouble(
                StartServerWorldTime);

            writer.WritePredictionKey(
                PredictionKey);

            writer.WriteInt(
                magnitudes.Count);

            for (
                int index = 0;
                index < magnitudes.Count;
                index++)
            {
                writer.WriteFloat(
                    magnitudes[index]);
            }
        }

        /// <summary>
        /// Reads evaluated gameplay modifier magnitudes from network data.
        /// </summary>
        private static float[] ReadEvaluatedModifierMagnitudes(
            NetworkReader reader)
        {
            int modifierCount = reader.ReadInt();

            if (
                modifierCount < 0 ||
                modifierCount > k_MaxModifierCount)
            {
                throw new InvalidOperationException(
                    $"Invalid replicated gameplay modifier count: {modifierCount}.");
            }

            float[] evaluatedModifierMagnitudes =
                new float[modifierCount];

            for (
                int index = 0;
                index < modifierCount;
                index++)
            {
                evaluatedModifierMagnitudes[index] =
                    reader.ReadFloat();
            }

            return evaluatedModifierMagnitudes;
        }
    }
}