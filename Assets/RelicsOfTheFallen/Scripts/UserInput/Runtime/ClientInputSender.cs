using RelicsOfTheFallen.Character;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RelicsOfTheFallen.UserInput
{
    public class ClientInputSender : NetworkBehaviour
    {
        private const float k_SendInterval = 0.04f;

        [SerializeField]
        private ServerCharacter m_ServerCharacter;

        [SerializeField]
        private Transform m_CameraPivot;

        [Header("Actions")]
        [SerializeField]
        private InputActionReference m_MoveAction;

        [SerializeField]
        private InputActionReference m_SprintAction;

        [SerializeField]
        private InputActionReference m_WalkAction;

        [SerializeField]
        private InputActionReference m_AimAction;

        [SerializeField]
        private InputActionReference m_JumpAction;

        [SerializeField]
        private InputActionReference m_CrouchAction;

        private uint m_NextTick;
        private float m_NextSendTime;
        private CharacterInputCommand m_LastSentCommand;
        private bool m_HasSentCommand;
        private bool m_WalkToggleQueued;
        private bool m_JumpPressedQueued;
        private bool m_CrouchToggleQueued;

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                enabled = false;
                return;
            }

            m_MoveAction.action.Enable();
            m_SprintAction.action.Enable();
            m_WalkAction.action.Enable();
            m_AimAction.action.Enable();
            m_JumpAction.action.Enable();
            m_CrouchAction.action.Enable();

            m_WalkAction.action.performed += OnWalkPerformed;
            m_JumpAction.action.performed += OnJumpPerformed;
            m_CrouchAction.action.performed += OnCrouchPerformed;
        }

        public override void OnNetworkDespawn()
        {
            if (!IsOwner)
            {
                return;
            }

            m_WalkAction.action.performed -= OnWalkPerformed;
            m_JumpAction.action.performed -= OnJumpPerformed;
            m_CrouchAction.action.performed -= OnCrouchPerformed;

            m_MoveAction.action.Disable();
            m_SprintAction.action.Disable();
            m_WalkAction.action.Disable();
            m_AimAction.action.Disable();
            m_JumpAction.action.Disable();
            m_CrouchAction.action.Disable();
        }

        private void Update()
        {
            CharacterInputCommand command = CreateCommand();

            if (!ShouldSend(command))
            {
                return;
            }

            m_ServerCharacter.ServerSendCharacterInputRpc(command);

            m_LastSentCommand = command;
            m_HasSentCommand = true;
            m_NextSendTime = Time.unscaledTime + k_SendInterval;

            m_WalkToggleQueued = false;
            m_JumpPressedQueued = false;
            m_CrouchToggleQueued = false;
        }

        private CharacterInputCommand CreateCommand()
        {
            CharacterInputButtons buttons = CharacterInputButtons.None;

            if (m_SprintAction.action.IsPressed())
            {
                buttons |= CharacterInputButtons.SprintHeld;
            }

            if (m_AimAction.action.IsPressed())
            {
                buttons |= CharacterInputButtons.AimHeld;
            }

            if (m_WalkToggleQueued)
            {
                buttons |= CharacterInputButtons.WalkToggle;
            }

            if (m_JumpPressedQueued)
            {
                buttons |= CharacterInputButtons.JumpPressed;
            }

            if (m_CrouchToggleQueued)
            {
                buttons |= CharacterInputButtons.CrouchToggle;
            }

            Vector3 pivotEulerAngles = m_CameraPivot.eulerAngles;

            return new CharacterInputCommand
            {
                Tick = m_NextTick++,
                Move = m_MoveAction.action.ReadValue<Vector2>(),
                LookYaw = pivotEulerAngles.y,
                LookPitch = NormalizePitch(pivotEulerAngles.x),
                Buttons = buttons
            };
        }

        private bool ShouldSend(CharacterInputCommand command)
        {
            if (!m_HasSentCommand)
            {
                return true;
            }

            if (command.Buttons != CharacterInputButtons.None)
            {
                return true;
            }

            if (command.Move != m_LastSentCommand.Move)
            {
                return true;
            }

            return Time.unscaledTime >= m_NextSendTime;
        }

        private void OnWalkPerformed(InputAction.CallbackContext context)
        {
            m_WalkToggleQueued = true;
        }

        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            m_JumpPressedQueued = true;
        }

        private void OnCrouchPerformed(InputAction.CallbackContext context)
        {
            m_CrouchToggleQueued = true;
        }

        private static float NormalizePitch(float pitch)
        {
            return pitch > 180f ? pitch - 360f : pitch;
        }
    }
}