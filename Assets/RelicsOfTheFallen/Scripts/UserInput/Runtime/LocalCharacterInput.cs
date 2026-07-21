using RelicsOfTheFallen.Character;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RelicsOfTheFallen.UserInput
{
    public sealed class LocalCharacterInput : MonoBehaviour
    {
        [SerializeField]
        Transform m_CameraPivot;

        [Header("Actions")]
        [SerializeField]
        InputActionReference m_MoveAction;

        [SerializeField]
        InputActionReference m_SprintAction;

        [SerializeField]
        InputActionReference m_WalkAction;

        [SerializeField]
        InputActionReference m_AimAction;

        [SerializeField]
        InputActionReference m_JumpAction;

        [SerializeField]
        InputActionReference m_CrouchAction;

        uint m_NextSequence;
        bool m_IsInputEnabled;

        bool m_WalkToggleQueued;
        bool m_JumpPressedQueued;
        bool m_CrouchToggleQueued;

        public void SetInputEnabled(bool isEnabled)
        {
            if (m_IsInputEnabled == isEnabled)
            {
                return;
            }

            m_IsInputEnabled = isEnabled;

            if (isEnabled)
            {
                EnableActions();
                SubscribeActions();

                m_NextSequence = 1;
                m_WalkToggleQueued = false;
                m_JumpPressedQueued = false;
                m_CrouchToggleQueued = false;

                return;
            }

            UnsubscribeActions();
            DisableActions();

            m_WalkToggleQueued = false;
            m_JumpPressedQueued = false;
            m_CrouchToggleQueued = false;
        }

        public bool TryReadCommand(
            out CharacterInputCommand command)
        {
            command = default;

            if (!m_IsInputEnabled)
            {
                return false;
            }

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

            command = new CharacterInputCommand
            {
                Sequence = m_NextSequence,
                Move = Vector2.ClampMagnitude(
                    m_MoveAction.action.ReadValue<Vector2>(),
                    1f),
                LookYaw = pivotEulerAngles.y,
                LookPitch = NormalizePitch(
                    pivotEulerAngles.x),
                HeldButtons = heldButtons,
                PressedButtons = pressedButtons
            };

            m_NextSequence++;

            m_WalkToggleQueued = false;
            m_JumpPressedQueued = false;
            m_CrouchToggleQueued = false;

            return true;
        }

        void OnDisable()
        {
            SetInputEnabled(false);
        }

        void EnableActions()
        {
            m_MoveAction.action.Enable();
            m_SprintAction.action.Enable();
            m_WalkAction.action.Enable();
            m_AimAction.action.Enable();
            m_JumpAction.action.Enable();
            m_CrouchAction.action.Enable();
        }

        void DisableActions()
        {
            m_MoveAction.action.Disable();
            m_SprintAction.action.Disable();
            m_WalkAction.action.Disable();
            m_AimAction.action.Disable();
            m_JumpAction.action.Disable();
            m_CrouchAction.action.Disable();
        }

        void SubscribeActions()
        {
            m_WalkAction.action.performed +=
                OnWalkPerformed;

            m_JumpAction.action.performed +=
                OnJumpPerformed;

            m_CrouchAction.action.performed +=
                OnCrouchPerformed;
        }

        void UnsubscribeActions()
        {
            m_WalkAction.action.performed -=
                OnWalkPerformed;

            m_JumpAction.action.performed -=
                OnJumpPerformed;

            m_CrouchAction.action.performed -=
                OnCrouchPerformed;
        }

        void OnWalkPerformed(
            InputAction.CallbackContext context)
        {
            m_WalkToggleQueued = true;
        }

        void OnJumpPerformed(
            InputAction.CallbackContext context)
        {
            m_JumpPressedQueued = true;
        }

        void OnCrouchPerformed(
            InputAction.CallbackContext context)
        {
            m_CrouchToggleQueued = true;
        }

        static float NormalizePitch(float pitch)
        {
            return pitch > 180f
                ? pitch - 360f
                : pitch;
        }
    }
}