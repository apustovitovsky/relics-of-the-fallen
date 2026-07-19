using Unity.Netcode;
using UnityEngine;

namespace RelicsOfTheFallen.Character
{
    [RequireComponent(typeof(NetworkObject))]
    public class ServerCharacter : NetworkBehaviour
    {
        [SerializeField] ClientCharacter m_ClientCharacter;

        public ClientCharacter clientCharacter => m_ClientCharacter;

        public NetworkVariable<bool> IsGrounded { get; } =
            new(true);

        public Vector3 MovementInput { get; private set; }

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                enabled = false;
            }
        }

        public override void OnNetworkDespawn()
        {
            MovementInput = Vector3.zero;
        }

        [Rpc(SendTo.Server)]
        public void ServerSendCharacterInputRpc(Vector3 movementInput)
        {
            movementInput.y = 0f;

            MovementInput =
                Vector3.ClampMagnitude(movementInput, 1f);
        }
    }
}