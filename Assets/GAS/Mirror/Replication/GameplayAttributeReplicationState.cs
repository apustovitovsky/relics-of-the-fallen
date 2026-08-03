using Mirror;

namespace GAS.Mirror
{
    /// <summary>
    /// Contains the replicated base and current values of one gameplay attribute.
    /// </summary>
    internal readonly struct GameplayAttributeReplicationState :
        INetworkSerializable
    {
        public float BaseValue
        {
            get;
        }

        public float CurrentValue
        {
            get;
        }

        /// <summary>
        /// Creates replicated gameplay attribute state from authoritative values.
        /// </summary>
        public GameplayAttributeReplicationState(
            float baseValue,
            float currentValue)
        {
            BaseValue = baseValue;
            CurrentValue = currentValue;
        }

        /// <summary>
        /// Creates gameplay attribute replication state from network data.
        /// </summary>
        internal GameplayAttributeReplicationState(
            NetworkReader reader)
            : this(
                reader.ReadFloat(),
                reader.ReadFloat())
        {
        }

        /// <summary>
        /// Serializes this gameplay attribute state into network data.
        /// </summary>
        public void Serialize(
            NetworkWriter writer)
        {
            writer.WriteFloat(
                BaseValue);

            writer.WriteFloat(
                CurrentValue);
        }
    }
}