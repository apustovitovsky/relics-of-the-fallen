using UnityEngine;

namespace RelicsOfTheFallen.Character.Movement
{
    [DisallowMultipleComponent]
    public sealed class CharacterMovementController :
        MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        MonoBehaviour m_InputSourceComponent;

        [SerializeField]
        MonoBehaviour m_LookSourceComponent;

        [SerializeField]
        CharacterController m_CharacterController;

        [Header("Movement")]
        [SerializeField, Min(0f)]
        float m_RunSpeed = 5f;

        [SerializeField, Min(0f)]
        float m_SprintSpeed = 10f;

        [SerializeField, Min(0f)]
        float m_Acceleration = 15f;

        [SerializeField, Min(0f)]
        float m_RotationSpeed = 720f;

        [Header("Jump")]
        [SerializeField]
        float m_Gravity = -25f;

        [SerializeField, Min(0f)]
        float m_JumpHeight = 5f;

        [SerializeField, Min(0f)]
        float m_MaxFallSpeed = 30f;

        ICharacterInputSource m_InputSource;
        ICharacterLookSource m_LookSource;

        Vector3 m_HorizontalVelocity;
        float m_VerticalVelocity;

        public Vector3 Velocity { get; private set; }

        public bool IsGrounded { get; private set; }

        public bool IsStrafing { get; private set; }

        void Awake()
        {
            m_InputSource =
                m_InputSourceComponent as ICharacterInputSource;

            m_LookSource =
                m_LookSourceComponent as ICharacterLookSource;

            if (m_CharacterController == null)
            {
                m_CharacterController =
                    GetComponentInParent<CharacterController>();
            }

            if (m_InputSource == null ||
                m_LookSource == null ||
                m_CharacterController == null)
            {
                Debug.LogError(
                    $"{nameof(CharacterMovementController)} on " +
                    $"'{name}' requires a character controller, input " +
                    "source, and look source.",
                    this);

                enabled = false;
            }
        }

        void Update()
        {
            var input = m_InputSource.Current;
            var moveDirection = CalculateMoveDirection(input.Move);

            IsStrafing =
                input.AimHeld &&
                !input.SprintHeld;

            UpdateHorizontalVelocity(
                moveDirection,
                input.SprintHeld);

            UpdateVerticalVelocity(input.JumpPressed);

            RotateCharacter(moveDirection);

            var movement =
                m_HorizontalVelocity +
                Vector3.up * m_VerticalVelocity;

            var collisionFlags =
                m_CharacterController.Move(
                    movement * Time.deltaTime);

            IsGrounded =
                (collisionFlags & CollisionFlags.Below) != 0;

            Velocity = m_CharacterController.velocity;
        }

        Vector3 CalculateMoveDirection(Vector2 moveInput)
        {
            var direction =
                m_LookSource.ForwardOnGround * moveInput.y +
                m_LookSource.RightOnGround * moveInput.x;

            return Vector3.ClampMagnitude(direction, 1f);
        }

        void UpdateHorizontalVelocity(
            Vector3 moveDirection,
            bool sprintHeld)
        {
            var targetSpeed = sprintHeld
                ? m_SprintSpeed
                : m_RunSpeed;

            var targetVelocity =
                moveDirection * targetSpeed;

            m_HorizontalVelocity =
                Vector3.MoveTowards(
                    m_HorizontalVelocity,
                    targetVelocity,
                    m_Acceleration * Time.deltaTime);
        }

        void UpdateVerticalVelocity(bool jumpPressed)
        {
            if (IsGrounded &&
                m_VerticalVelocity < 0f)
            {
                m_VerticalVelocity = -2f;
            }

            if (jumpPressed && IsGrounded)
            {
                m_VerticalVelocity =
                    Mathf.Sqrt(
                        m_JumpHeight *
                        -2f *
                        m_Gravity);
            }

            m_VerticalVelocity = Mathf.Max(
                m_VerticalVelocity +
                m_Gravity * Time.deltaTime,
                -m_MaxFallSpeed);
        }

        void RotateCharacter(Vector3 moveDirection)
        {
            Vector3 targetDirection;

            if (IsStrafing)
            {
                targetDirection =
                    m_LookSource.ForwardOnGround;
            }
            else
            {
                targetDirection =
                    m_HorizontalVelocity;
            }

            targetDirection.y = 0f;

            if (targetDirection.sqrMagnitude < 0.0001f)
            {
                return;
            }

            var targetRotation =
                Quaternion.LookRotation(
                    targetDirection,
                    Vector3.up);

            var characterRoot =
                m_CharacterController.transform;

            characterRoot.rotation =
                Quaternion.RotateTowards(
                    characterRoot.rotation,
                    targetRotation,
                    m_RotationSpeed * Time.deltaTime);
        }
    }
}