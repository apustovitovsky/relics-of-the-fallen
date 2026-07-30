using System;
using Mirror;

namespace GAS.Mirror
{
    /// <summary>
    /// Contains the minimal network state required to reconstruct one granted ability specification.
    /// </summary>
    internal readonly struct GameplayAbilitySpecReplicationState :
        INetworkSerializable
    {
        public AssetId AbilityId
        {
            get;
        }

        public int Level
        {
            get;
        }

        public bool IsValid =>
            AbilityId.IsValid;

        /// <summary>
        /// Creates the replicated state of one authoritative gameplay ability specification.
        /// </summary>
        public GameplayAbilitySpecReplicationState(
            AssetId abilityId,
            int level)
        {
            if (!abilityId.IsValid)
            {
                throw new ArgumentException(
                    "Gameplay ability asset ID must be valid.",
                    nameof(abilityId));
            }

            AbilityId = abilityId;
            Level = level;
        }

        /// <summary>
        /// Creates gameplay ability specification replication state from network data.
        /// </summary>
        internal GameplayAbilitySpecReplicationState(
            NetworkReader reader)
            : this(
                reader.ReadAssetId(),
                reader.ReadInt())
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
                    "Gameplay ability specification replication state must be valid.");
            }

            writer.WriteAssetId(
                AbilityId);

            writer.WriteInt(
                Level);
        }
    }
}