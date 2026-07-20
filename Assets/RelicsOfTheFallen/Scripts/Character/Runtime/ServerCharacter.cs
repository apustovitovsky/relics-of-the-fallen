using Unity.Netcode;
using UnityEngine;

namespace RelicsOfTheFallen.Character
{
    [RequireComponent(typeof(NetworkObject))]
    public class ServerCharacter : NetworkBehaviour
    {
        [SerializeField] ClientCharacter m_ClientCharacter;

        public ClientCharacter clientCharacter => m_ClientCharacter;

        public NetworkVariable<CharacterLocomotionState>
            LocomotionState
        { get; } = new();

        public CharacterInputCommand InputCommand { get; private set; }

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                enabled = false;
            }
        }

        public override void OnNetworkDespawn()
        {
            InputCommand = default;
        }

        [Rpc(SendTo.Server)]
        public void ServerSendCharacterInputRpc(
            CharacterInputCommand inputCommand)
        {
            if (inputCommand.Tick <= InputCommand.Tick)
            {
                return;
            }

            inputCommand.Move =
                Vector2.ClampMagnitude(inputCommand.Move, 1f);

            inputCommand.LookYaw =
                Mathf.Repeat(inputCommand.LookYaw, 360f);

            inputCommand.LookPitch =
                Mathf.Clamp(inputCommand.LookPitch, -89f, 89f);

            InputCommand = inputCommand;
        }
    }
}