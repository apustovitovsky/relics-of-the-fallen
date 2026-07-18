using Unity.Netcode;
using UnityEngine;

namespace RelicsOfTheFallen.GameplayObjects
{
    [RequireComponent(typeof(NetworkObject))]
    public sealed class PersistentPlayer : NetworkBehaviour
    {
        public override void OnNetworkSpawn()
        {
            gameObject.name = $"PersistentPlayer-{OwnerClientId}";
        }
    }
}