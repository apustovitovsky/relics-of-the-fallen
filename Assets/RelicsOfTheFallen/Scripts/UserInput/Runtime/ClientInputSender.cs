using RelicsOfTheFallen.Character;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RelicsOfTheFallen.UserInput
{
    [RequireComponent(typeof(ServerCharacter))]
    public class ClientInputSender : NetworkBehaviour
    {
        const float k_MoveSendRateSeconds = 0.04f;

        [SerializeField] ServerCharacter m_ServerCharacter;
        [SerializeField] Transform m_CameraPivot;
        [SerializeField] InputActionReference m_MoveAction;
        [SerializeField] InputActionReference m_SprintAction;

        float m_LastSentMove;
        uint m_NextInputTick;
        CharacterInputCommand m_LastSentInputCommand;

        public override void OnNetworkSpawn()
        {
            if (!IsClient || !IsOwner)
            {
                enabled = false;
                return;
            }

            m_MoveAction.action.actionMap.Enable();
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner)
            {
                m_MoveAction.action.actionMap.Disable();
            }

            enabled = false;
        }

        void Update()
        {
            Vector2 moveInput =
                Vector2.ClampMagnitude(
                    m_MoveAction.action.ReadValue<Vector2>(),
                    1f);

            CharacterInputButtons buttons =
                m_SprintAction.action.IsPressed()
                    ? CharacterInputButtons.SprintHeld
                    : CharacterInputButtons.None;

            CharacterInputCommand inputCommand =
                new CharacterInputCommand
                {
                    Move = moveInput,
                    LookYaw = m_CameraPivot.eulerAngles.y,
                    LookPitch = NormalizePitch(
                        m_CameraPivot.eulerAngles.x),
                    Buttons = buttons
                };

            bool inputChanged =
                inputCommand.Move != m_LastSentInputCommand.Move ||
                !Mathf.Approximately(
                    inputCommand.LookYaw,
                    m_LastSentInputCommand.LookYaw) ||
                !Mathf.Approximately(
                    inputCommand.LookPitch,
                    m_LastSentInputCommand.LookPitch) ||
                inputCommand.Buttons !=
                m_LastSentInputCommand.Buttons;

            if (!inputChanged ||
                Time.time - m_LastSentMove <
                k_MoveSendRateSeconds)
            {
                return;
            }

            inputCommand.Tick = ++m_NextInputTick;

            m_LastSentMove = Time.time;
            m_LastSentInputCommand = inputCommand;

            m_ServerCharacter
                .ServerSendCharacterInputRpc(inputCommand);
        }

        static float NormalizePitch(float pitch)
        {
            return pitch > 180f ? pitch - 360f : pitch;
        }
    }
}