using System;
using System.Collections.Generic;
using Mirror;

namespace GAS.Mirror
{
    /// <summary>
    /// Contains the network state required to reconstruct one granted ability specification.
    /// </summary>
    internal readonly struct GameplayAbilitySpecReplicationState :
        INetworkSerializable
    {
        internal const int k_MaxDynamicAbilityTagCount = 64;

        private readonly AssetId[] m_DynamicAbilityTagIds;

        public AssetId AbilityId
        {
            get;
        }

        public int Level
        {
            get;
        }

        public IReadOnlyList<AssetId> DynamicAbilityTagIds =>
            m_DynamicAbilityTagIds ??
            Array.Empty<AssetId>();

        public bool IsValid =>
            AbilityId.IsValid;

        /// <summary>
        /// Creates replicated state for an ability specification without dynamic tags.
        /// </summary>
        public GameplayAbilitySpecReplicationState(
            AssetId abilityId,
            int level)
            : this(
                abilityId,
                level,
                Array.Empty<AssetId>())
        {
        }

        /// <summary>
        /// Creates the replicated state of one authoritative gameplay ability specification.
        /// </summary>
        public GameplayAbilitySpecReplicationState(
            AssetId abilityId,
            int level,
            IReadOnlyList<AssetId> dynamicAbilityTagIds)
        {
            if (!abilityId.IsValid)
            {
                throw new ArgumentException(
                    "Gameplay ability asset ID must be valid.",
                    nameof(abilityId));
            }

            if (dynamicAbilityTagIds == null)
            {
                throw new ArgumentNullException(
                    nameof(dynamicAbilityTagIds));
            }

            if (
                dynamicAbilityTagIds.Count >
                k_MaxDynamicAbilityTagCount)
            {
                throw new ArgumentException(
                    "Gameplay ability specification has too many " +
                    "dynamic ability tags.",
                    nameof(dynamicAbilityTagIds));
            }

            AbilityId = abilityId;
            Level = level;

            m_DynamicAbilityTagIds =
                new AssetId[dynamicAbilityTagIds.Count];

            for (
                int index = 0;
                index < dynamicAbilityTagIds.Count;
                index++)
            {
                AssetId tagId =
                    dynamicAbilityTagIds[index];

                if (!tagId.IsValid)
                {
                    throw new ArgumentException(
                        "Dynamic ability tag IDs must be valid.",
                        nameof(dynamicAbilityTagIds));
                }

                m_DynamicAbilityTagIds[index] =
                    tagId;
            }
        }

        /// <summary>
        /// Creates gameplay ability specification replication state from network data.
        /// </summary>
        internal GameplayAbilitySpecReplicationState(
            NetworkReader reader)
            : this(
                reader.ReadAssetId(),
                reader.ReadInt(),
                ReadDynamicAbilityTagIds(
                    reader))
        {
        }

        /// <summary>
        /// Serializes this gameplay ability specification replication state into network data.
        /// </summary>
        public void Serialize(
            NetworkWriter writer)
        {
            if (!IsValid)
            {
                throw new InvalidOperationException(
                    "Gameplay ability specification replication " +
                    "state must be valid.");
            }

            IReadOnlyList<AssetId> dynamicAbilityTagIds =
                DynamicAbilityTagIds;

            if (
                dynamicAbilityTagIds.Count >
                k_MaxDynamicAbilityTagCount)
            {
                throw new InvalidOperationException(
                    "Gameplay ability specification replication " +
                    "state has too many dynamic ability tags.");
            }

            writer.WriteAssetId(
                AbilityId);

            writer.WriteInt(
                Level);

            writer.WriteInt(
                dynamicAbilityTagIds.Count);

            for (
                int index = 0;
                index < dynamicAbilityTagIds.Count;
                index++)
            {
                writer.WriteAssetId(
                    dynamicAbilityTagIds[index]);
            }
        }

        /// <summary>
        /// Reads and validates dynamic ability tag identities from network data.
        /// </summary>
        private static AssetId[] ReadDynamicAbilityTagIds(
            NetworkReader reader)
        {
            int tagCount =
                reader.ReadInt();

            if (
                tagCount < 0 ||
                tagCount > k_MaxDynamicAbilityTagCount)
            {
                throw new InvalidOperationException(
                    $"Invalid replicated dynamic ability tag " +
                    $"count: {tagCount}.");
            }

            AssetId[] tagIds =
                new AssetId[tagCount];

            for (
                int index = 0;
                index < tagCount;
                index++)
            {
                tagIds[index] =
                    reader.ReadAssetId();
            }

            return tagIds;
        }
    }
}