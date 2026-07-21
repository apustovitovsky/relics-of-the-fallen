using UnityEngine;
using UnityEngine.InputSystem;

namespace RelicsOfTheFallen.Character
{
    [DisallowMultipleComponent]
    public sealed class LocalCharacterInput :
        MonoBehaviour,
        ICharacterInputSource
    {
        [Header("Movement")]
        [SerializeField]
        InputActionReference m_MoveAction;

        [SerializeField]
        InputActionReference m_LookAction;

        [SerializeField]
        InputActionReference m_SprintAction;

        [SerializeField]
        InputActionReference m_AimAction;

        [SerializeField]
        InputActionReference m_JumpAction;

        public CharacterInputState Current { get; private set; }

        void OnEnable()
        {
            if (!TryGetActions(
                    out var moveAction,
                    out var lookAction,
                    out var sprintAction,
                    out var aimAction,
                    out var jumpAction))
            {
                enabled = false;
                return;
            }

            moveAction.Enable();
            lookAction.Enable();
            sprintAction.Enable();
            aimAction.Enable();
            jumpAction.Enable();
        }

        void Update()
        {
            if (!TryGetActions(
                    out var moveAction,
                    out var lookAction,
                    out var sprintAction,
                    out var aimAction,
                    out var jumpAction))
            {
                return;
            }

            Current = new CharacterInputState(
                moveAction.ReadValue<Vector2>(),
                lookAction.ReadValue<Vector2>(),
                sprintAction.IsPressed(),
                aimAction.IsPressed(),
                jumpAction.WasPressedThisFrame());
        }

        void OnDisable()
        {
            if (!TryGetActions(
                    out var moveAction,
                    out var lookAction,
                    out var sprintAction,
                    out var aimAction,
                    out var jumpAction))
            {
                return;
            }

            moveAction.Disable();
            lookAction.Disable();
            sprintAction.Disable();
            aimAction.Disable();
            jumpAction.Disable();

            Current = default;
        }

        bool TryGetActions(
            out InputAction moveAction,
            out InputAction lookAction,
            out InputAction sprintAction,
            out InputAction aimAction,
            out InputAction jumpAction)
        {
            moveAction = m_MoveAction != null ? m_MoveAction.action : null;
            lookAction = m_LookAction != null ? m_LookAction.action : null;
            sprintAction = m_SprintAction != null ? m_SprintAction.action : null;
            aimAction = m_AimAction != null ? m_AimAction.action : null;
            jumpAction = m_JumpAction != null ? m_JumpAction.action : null;

            if (moveAction != null &&
                lookAction != null &&
                sprintAction != null &&
                aimAction != null &&
                jumpAction != null)
            {
                return true;
            }

            Debug.LogError(
                $"{nameof(LocalCharacterInput)} on '{name}' requires all Player input actions.",
                this);

            return false;
        }
    }
}