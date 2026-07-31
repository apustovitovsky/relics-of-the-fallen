using Mirror;
using UnityEngine;

namespace GAS.Mirror
{
    internal readonly struct GameplayEffectContextReplicationState :
        INetworkSerializable,
        IGameplayEffectContextObjectProvider
    {
        public uint InstigatorNetworkId
        {
            get;
        }

        public uint EffectCauserNetworkId
        {
            get;
        }

        public Object SourceObject =>
            null;

        public GameObject Instigator =>
            GetSpawnedObject(
                InstigatorNetworkId);

        public GameObject EffectCauser =>
            GetSpawnedObject(
                EffectCauserNetworkId);

        /// <summary>
        /// Creates the replicated object-reference state of one gameplay effect context.
        /// </summary>
        public GameplayEffectContextReplicationState(
            uint instigatorNetworkId,
            uint effectCauserNetworkId)
        {
            InstigatorNetworkId = instigatorNetworkId;
            EffectCauserNetworkId = effectCauserNetworkId;
        }

        /// <summary>
        /// Creates gameplay effect context replication state from network data.
        /// </summary>
        internal GameplayEffectContextReplicationState(
            NetworkReader reader)
            : this(
                reader.ReadUInt(),
                reader.ReadUInt())
        {
        }

        /// <summary>
        /// Serializes this gameplay effect context state into network data.
        /// </summary>
        public void Serialize(
            NetworkWriter writer)
        {
            writer.WriteUInt(
                InstigatorNetworkId);

            writer.WriteUInt(
                EffectCauserNetworkId);
        }

        /// <summary>
        /// Returns the currently spawned object represented by an optional network identity.
        /// </summary>
        private static GameObject GetSpawnedObject(
            uint networkId)
        {
            if (
                networkId == 0 ||
                !NetworkClient.spawned.TryGetValue(
                    networkId,
                    out NetworkIdentity identity) ||
                identity == null)
            {
                return null;
            }

            return identity.gameObject;
        }
    }
}