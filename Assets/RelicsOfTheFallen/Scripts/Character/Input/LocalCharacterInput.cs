using System;
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

        [Header("Abilities")]
        [SerializeField]
        InputActionReference m_AttackAction;

        public CharacterInputState Current { get; private set; }

        public event Action AttackPerformed;

        void OnEnable()
        {
            if (!TryGetActions(
                    out var moveAction,
                    out var lookAction,
                    out var sprintAction,
                    out var aimAction,
                    out var jumpAction,
                    out var attackAction))
            {
                enabled = false;
                return;
            }

            attackAction.performed +=
                OnAttackPerformed;

            moveAction.Enable();
            lookAction.Enable();
            sprintAction.Enable();
            aimAction.Enable();
            jumpAction.Enable();
            attackAction.Enable();
        }

        void Update()
        {
            if (!TryGetActions(
                    out var moveAction,
                    out var lookAction,
                    out var sprintAction,
                    out var aimAction,
                    out var jumpAction,
                    out _))
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
                    out var jumpAction,
                    out var attackAction))
            {
                Current = default;
                return;
            }

            attackAction.performed -=
                OnAttackPerformed;

            moveAction.Disable();
            lookAction.Disable();
            sprintAction.Disable();
            aimAction.Disable();
            jumpAction.Disable();
            attackAction.Disable();

            Current = default;
        }

        void OnAttackPerformed(
            InputAction.CallbackContext context)
        {
            AttackPerformed?.Invoke();
        }

        bool TryGetActions(
            out InputAction moveAction,
            out InputAction lookAction,
            out InputAction sprintAction,
            out InputAction aimAction,
            out InputAction jumpAction,
            out InputAction attackAction)
        {
            moveAction =
                m_MoveAction != null
                    ? m_MoveAction.action
                    : null;

            lookAction =
                m_LookAction != null
                    ? m_LookAction.action
                    : null;

            sprintAction =
                m_SprintAction != null
                    ? m_SprintAction.action
                    : null;

            aimAction =
                m_AimAction != null
                    ? m_AimAction.action
                    : null;

            jumpAction =
                m_JumpAction != null
                    ? m_JumpAction.action
                    : null;

            attackAction =
                m_AttackAction != null
                    ? m_AttackAction.action
                    : null;

            if (moveAction != null &&
                lookAction != null &&
                sprintAction != null &&
                aimAction != null &&
                jumpAction != null &&
                attackAction != null)
            {
                return true;
            }

            Debug.LogError(
                $"{nameof(LocalCharacterInput)} on " +
                $"'{name}' requires all Player input actions.",
                this);

            return false;
        }
    }
}