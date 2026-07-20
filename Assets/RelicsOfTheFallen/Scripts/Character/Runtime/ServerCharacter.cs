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
        [Min(1)]
        private int m_InputBufferCapacity = 64;

        [SerializeField]
        private ClientCharacter m_ClientCharacter;

        private CharacterInputBuffer m_InputBuffer;
        private CharacterInputCommand m_CurrentInputCommand;
        private float m_LastInputReceivedTime;
        private bool m_HasReceivedInput;

        public ClientCharacter clientCharacter => m_ClientCharacter;

        public NetworkVariable<CharacterLocomotionState>
            LocomotionState
        { get; } = new();

        public NetworkVariable<CharacterOwnerSnapshot>
            OwnerSnapshot
        {
            get;
        } = new(
            default,
            NetworkVariableReadPermission.Owner,
            NetworkVariableWritePermission.Server);

        private void Awake()
        {
            m_InputBuffer = new CharacterInputBuffer(
                m_InputBufferCapacity);
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
        }

        public CharacterInputCommand ConsumeInputCommand()
        {
            if (!m_HasReceivedInput &&
                m_InputBuffer.Count == 0)
            {
                return default;
            }

            if (Time.unscaledTime - m_LastInputReceivedTime >
                m_InputTimeoutSeconds)
            {
                ClearBufferedInput();

                m_CurrentInputCommand.Move = Vector2.zero;
                m_CurrentInputCommand.HeldButtons =
                    CharacterInputHeldButtons.None;
                m_CurrentInputCommand.PressedButtons =
                    CharacterInputPressedButtons.None;

                return m_CurrentInputCommand;
            }

            if (m_InputBuffer.TryDequeue(
                    out CharacterInputCommand receivedCommand))
            {
                m_CurrentInputCommand = receivedCommand;
                m_HasReceivedInput = true;
            }

            CharacterInputCommand command = m_CurrentInputCommand;

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

            if (!m_InputBuffer.TryEnqueue(inputCommand))
            {
                return;
            }

            m_LastInputReceivedTime = Time.unscaledTime;
        }

        private void ClearInput()
        {
            ClearBufferedInput();

            m_CurrentInputCommand = default;
            m_LastInputReceivedTime = 0f;
            m_HasReceivedInput = false;
        }

        private void ClearBufferedInput()
        {
            m_InputBuffer.Clear();
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