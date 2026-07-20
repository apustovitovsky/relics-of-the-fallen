using System;
using RelicsOfTheFallen.Character;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RelicsOfTheFallen.UserInput
{
    public class ClientInputSender :
        NetworkBehaviour,
        ICharacterInputHistoryProvider
    {
        [SerializeField]
        private ServerCharacter m_ServerCharacter;

        [SerializeField]
        private Transform m_CameraPivot;

        [Header("Prediction")]
        [SerializeField]
        [Min(1)]
        private int m_SentInputHistoryCapacity = 128;

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

        private CharacterInputHistory m_SentInputHistory;

        private uint m_NextSequence;
        private bool m_WalkToggleQueued;
        private bool m_JumpPressedQueued;
        private bool m_CrouchToggleQueued;

        public CharacterInputHistory SentInputHistory =>
            m_SentInputHistory;

        public event Action<CharacterInputCommand>
            CommandRecorded;

        private void Awake()
        {
            m_SentInputHistory = new CharacterInputHistory(
                m_SentInputHistoryCapacity);
        }

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                enabled = false;
                return;
            }

            m_NextSequence = 1;

            m_WalkToggleQueued = false;
            m_JumpPressedQueued = false;
            m_CrouchToggleQueued = false;

            m_SentInputHistory.Clear();

            m_MoveAction.action.Enable();
            m_SprintAction.action.Enable();
            m_WalkAction.action.Enable();
            m_AimAction.action.Enable();
            m_JumpAction.action.Enable();
            m_CrouchAction.action.Enable();

            m_WalkAction.action.performed += OnWalkPerformed;
            m_JumpAction.action.performed += OnJumpPerformed;
            m_CrouchAction.action.performed += OnCrouchPerformed;

            m_ServerCharacter.OwnerSnapshot.OnValueChanged +=
                OnOwnerSnapshotChanged;

            NetworkManager.NetworkTickSystem.Tick +=
                OnNetworkTick;

            AcknowledgeInputThrough(
                m_ServerCharacter.OwnerSnapshot.Value);
        }

        public override void OnNetworkDespawn()
        {
            if (!IsOwner)
            {
                return;
            }

            NetworkManager.NetworkTickSystem.Tick -=
                OnNetworkTick;

            m_ServerCharacter.OwnerSnapshot.OnValueChanged -=
                OnOwnerSnapshotChanged;

            m_WalkAction.action.performed -= OnWalkPerformed;
            m_JumpAction.action.performed -= OnJumpPerformed;
            m_CrouchAction.action.performed -= OnCrouchPerformed;

            m_MoveAction.action.Disable();
            m_SprintAction.action.Disable();
            m_WalkAction.action.Disable();
            m_AimAction.action.Disable();
            m_JumpAction.action.Disable();
            m_CrouchAction.action.Disable();

            m_SentInputHistory.Clear();
        }

        private void OnNetworkTick()
        {
            CharacterInputCommand command = CreateCommand();

            if (!m_SentInputHistory.TryRecord(command))
            {
                Debug.LogError(
                    "The sent character input history is full.",
                    this);

                return;
            }

            CommandRecorded?.Invoke(command);

            m_ServerCharacter.ServerSendCharacterInputRpc(command);

            m_NextSequence++;

            m_WalkToggleQueued = false;
            m_JumpPressedQueued = false;
            m_CrouchToggleQueued = false;
        }

        private CharacterInputCommand CreateCommand()
        {
            CharacterInputHeldButtons heldButtons =
                CharacterInputHeldButtons.None;

            if (m_SprintAction.action.IsPressed())
            {
                heldButtons |= CharacterInputHeldButtons.Sprint;
            }

            if (m_AimAction.action.IsPressed())
            {
                heldButtons |= CharacterInputHeldButtons.Aim;
            }

            CharacterInputPressedButtons pressedButtons =
                CharacterInputPressedButtons.None;

            if (m_WalkToggleQueued)
            {
                pressedButtons |=
                    CharacterInputPressedButtons.WalkToggle;
            }

            if (m_JumpPressedQueued)
            {
                pressedButtons |=
                    CharacterInputPressedButtons.Jump;
            }

            if (m_CrouchToggleQueued)
            {
                pressedButtons |=
                    CharacterInputPressedButtons.CrouchToggle;
            }

            Vector3 pivotEulerAngles = m_CameraPivot.eulerAngles;

            return new CharacterInputCommand
            {
                Sequence = m_NextSequence,
                Move = m_MoveAction.action.ReadValue<Vector2>(),
                LookYaw = pivotEulerAngles.y,
                LookPitch = NormalizePitch(pivotEulerAngles.x),
                HeldButtons = heldButtons,
                PressedButtons = pressedButtons
            };
        }

        private void OnOwnerSnapshotChanged(
            CharacterOwnerSnapshot previousValue,
            CharacterOwnerSnapshot newValue)
        {
            AcknowledgeInputThrough(newValue);
        }

        private void AcknowledgeInputThrough(
            CharacterOwnerSnapshot snapshot)
        {
            m_SentInputHistory.DiscardThrough(
                snapshot.LastProcessedInputSequence);
        }

        private void OnWalkPerformed(
            InputAction.CallbackContext context)
        {
            m_WalkToggleQueued = true;
        }

        private void OnJumpPerformed(
            InputAction.CallbackContext context)
        {
            m_JumpPressedQueued = true;
        }

        private void OnCrouchPerformed(
            InputAction.CallbackContext context)
        {
            m_CrouchToggleQueued = true;
        }

        private static float NormalizePitch(float pitch)
        {
            return pitch > 180f ? pitch - 360f : pitch;
        }
    }
}