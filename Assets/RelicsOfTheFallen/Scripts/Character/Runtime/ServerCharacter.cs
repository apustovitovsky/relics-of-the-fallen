using System;
using Unity.Netcode;
using UnityEngine;

namespace RelicsOfTheFallen.Character
{
    [RequireComponent(typeof(NetworkObject))]
    public class ServerCharacter : NetworkBehaviour
    {
        [SerializeField]
        [Min(0.05f)]
        private float m_InputTimeoutSeconds = 0.2f;

        [SerializeField]
        private ClientCharacter m_ClientCharacter;

        private CharacterInputInbox m_InputInbox;
        private CharacterInputCommand m_CurrentInputCommand;
        private float m_LastInputReceivedTime;
        private bool m_HasReceivedInput;

        public ClientCharacter clientCharacter => m_ClientCharacter;

        /// <summary>
        /// Latest server locomotion state for host and owner presentation.
        /// Remote observers consume the buffered unreliable snapshot stream.
        /// </summary>
        public NetworkVariable<CharacterLocomotionState>
            LocomotionState
        { get; } = new();

        /// <summary>
        /// Server-authoritative rollback state visible only to the owner.
        /// </summary>
        public NetworkVariable<CharacterOwnerSnapshot>
            OwnerSnapshot
        {
            get;
        } = new(
            default,
            NetworkVariableReadPermission.Owner,
            NetworkVariableWritePermission.Server);

        /// <summary>
        /// Raised only on non-owning clients by the unreliable observer
        /// snapshot RPC. The snapshot is visual data, not gameplay truth.
        /// </summary>
        public event Action<CharacterRenderSnapshot>
            RenderSnapshotReceived;

        private void Awake()
        {
            m_InputInbox = new CharacterInputInbox();
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                enabled = false;
            }
        }

        public override void OnNetworkDespawn()
        {
            ClearInput();
            RenderSnapshotReceived = null;
        }

        public CharacterInputCommand ConsumeInputCommand()
        {
            if (!m_HasReceivedInput &&
                !m_InputInbox.HasInput)
            {
                return default;
            }

            if (Time.unscaledTime - m_LastInputReceivedTime >
                m_InputTimeoutSeconds)
            {
                m_InputInbox.ClearPendingInput();

                m_CurrentInputCommand.Move = Vector2.zero;
                m_CurrentInputCommand.HeldButtons =
                    CharacterInputHeldButtons.None;

                m_CurrentInputCommand.PressedButtons =
                    CharacterInputPressedButtons.None;

                return m_CurrentInputCommand;
            }

            if (m_InputInbox.TryConsume(
                    out CharacterInputCommand receivedCommand))
            {
                m_CurrentInputCommand = receivedCommand;
                m_HasReceivedInput = true;
            }

            CharacterInputCommand command =
                m_CurrentInputCommand;

            m_CurrentInputCommand.PressedButtons =
                CharacterInputPressedButtons.None;

            return command;
        }

        [Rpc(
            SendTo.Server,
            InvokePermission = RpcInvokePermission.Owner)]
        public void ServerSendCharacterInputRpc(
            CharacterInputCommand inputCommand,
            RpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != OwnerClientId ||
                !IsFinite(inputCommand))
            {
                return;
            }

            inputCommand.Move = Vector2.ClampMagnitude(
                inputCommand.Move,
                1f);

            inputCommand.LookYaw = Mathf.Repeat(
                inputCommand.LookYaw,
                360f);

            inputCommand.LookPitch = Mathf.Clamp(
                inputCommand.LookPitch,
                -89f,
                89f);

            inputCommand.HeldButtons &=
                CharacterInputHeldButtons.All;

            inputCommand.PressedButtons &=
                CharacterInputPressedButtons.All;

            if (!m_InputInbox.TryPush(inputCommand))
            {
                return;
            }

            m_LastInputReceivedTime = Time.unscaledTime;
        }

        /// <summary>
        /// Sends a visual snapshot to all non-owning clients. Unreliable
        /// delivery prevents an old pose from delaying a newer one.
        /// </summary>
        [Rpc(
            SendTo.NotOwner,
            Delivery = RpcDelivery.Unreliable,
            InvokePermission = RpcInvokePermission.Server)]
        public void ClientReceiveRenderSnapshotRpc(
            CharacterRenderSnapshot snapshot)
        {
            RenderSnapshotReceived?.Invoke(snapshot);
        }

        private void ClearInput()
        {
            m_InputInbox.Clear();

            m_CurrentInputCommand = default;
            m_LastInputReceivedTime = 0f;
            m_HasReceivedInput = false;
        }

        private static bool IsFinite(
            CharacterInputCommand inputCommand)
        {
            return IsFinite(inputCommand.Move.x) &&
                   IsFinite(inputCommand.Move.y) &&
                   IsFinite(inputCommand.LookYaw) &&
                   IsFinite(inputCommand.LookPitch);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) &&
                   !float.IsInfinity(value);
        }
    }
}