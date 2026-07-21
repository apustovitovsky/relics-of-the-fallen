using Mirror;
using UnityEngine;

namespace RelicsOfTheFallen.Player
{
    [RequireComponent(typeof(NetworkIdentity))]
    public sealed class PersistentPlayer : NetworkBehaviour
    {
        public override void OnStartClient()
        {
            gameObject.name = $"PersistentPlayer-{netId}";
        }
    }
}