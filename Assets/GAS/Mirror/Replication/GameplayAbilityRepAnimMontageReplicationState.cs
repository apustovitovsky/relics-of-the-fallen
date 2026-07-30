using System;
using Mirror;

namespace GAS.Mirror
{
    /// <summary>
    /// Contains the network state required to reconstruct one gameplay ability montage.
    /// </summary>
    internal readonly struct GameplayAbilityRepAnimMontageReplicationState :
        INetworkSerializable
    {
        public AssetId AnimationId
        {
            get;
        }

        public bool IsValid =>
            AnimationId.IsValid;

        public byte PlayInstanceId
        {
            get;
        }

        public float PlayRate
        {
            get;
        }

        public float Position
        {
            get;
        }

        public float BlendTime
        {
            get;
        }

        public bool IsStopped
        {
            get;
        }

        public PredictionKey PredictionKey
        {
            get;
        }

        /// <summary>
        /// Creates the network state of one authoritative gameplay ability montage.
        /// </summary>
        public GameplayAbilityRepAnimMontageReplicationState(
            AssetId animationId,
            byte playInstanceId,
            float playRate,
            float position,
            float blendTime,
            bool isStopped,
            PredictionKey predictionKey)
        {
            if (!animationId.IsValid)
            {
                throw new ArgumentException(
                    "Gameplay ability montage asset ID must be valid.",
                    nameof(animationId));
            }

            if (!isStopped &&
                playRate <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(playRate),
                    "Active montage play rate must be greater than zero.");
            }

            if (position < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(position),
                    "Montage position cannot be negative.");
            }

            if (blendTime < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(blendTime),
                    "Montage blend time cannot be negative.");
            }

            AnimationId = animationId;
            PlayInstanceId = playInstanceId;
            PlayRate = playRate;
            Position = position;
            BlendTime = blendTime;
            IsStopped = isStopped;
            PredictionKey = predictionKey;
        }

        /// <summary>
        /// Creates gameplay ability animation montage replication state from network data.
        /// </summary>
        internal GameplayAbilityRepAnimMontageReplicationState(
            NetworkReader reader)
        {
            if (!reader.ReadBool())
            {
                this = default;

                return;
            }

            this = new GameplayAbilityRepAnimMontageReplicationState(
                reader.ReadAssetId(),
                reader.ReadByte(),
                reader.ReadFloat(),
                reader.ReadFloat(),
                reader.ReadFloat(),
                reader.ReadBool(),
                reader.ReadPredictionKey());
        }

        /// <summary>
        /// Serializes this gameplay ability animation montage replication state into network data.
        /// </summary>
        public void Serialize(
            NetworkWriter writer)
        {
            bool isValid = IsValid;

            writer.WriteBool(
                isValid);

            if (!isValid)
            {
                return;
            }

            writer.WriteAssetId(
                AnimationId);

            writer.WriteByte(
                PlayInstanceId);

            writer.WriteFloat(
                PlayRate);

            writer.WriteFloat(
                Position);

            writer.WriteFloat(
                BlendTime);

            writer.WriteBool(
                IsStopped);

            writer.WritePredictionKey(
                PredictionKey);
        }
    }
}