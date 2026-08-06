using Mirror;

namespace GAS.Mirror
{
    /// <summary>
    /// Defines a value that can serialize itself into network data.
    /// </summary>
    internal interface INetworkSerializable
    {
        /// <summary>
        /// Serializes this value into the provided network writer.
        /// </summary>
        void Serialize(NetworkWriter writer);
    }
}