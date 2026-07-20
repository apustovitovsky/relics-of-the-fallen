using RelicsOfTheFallen.Character;
using Unity.Netcode;
using UnityEngine;

namespace RelicsOfTheFallen.Character.Movement
{
    public class ServerCharacterMovement : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField]
        private ServerCharacter m_ServerCharacter;

        [SerializeField]
        private CharacterController m_CharacterController;

        [Header("Movement")]
        [SerializeField]
        private bool m_AlwaysStrafe = true;

        [SerializeField]
        private float m_WalkSpeed = 1.4f;

        [SerializeField]
        private float m_RunSpeed = 5f;

        [SerializeField]
        private float m_SprintSpeed = 7f;

        [SerializeField]
        private float m_SpeedChangeDamping = 10f;

        [SerializeField]
        private float m_JumpForce = 10f;

        [SerializeField]
        private float m_GravityMultiplier = 2f;

        [SerializeField]
        private float m_RotationSmoothing = 10f;

        [Header("Crouch")]
        [SerializeField]
        private float m_CrouchingHeight = 1.2f;

        [SerializeField]
        private float m_CrouchingCenter = 0.6f;

        [SerializeField]
        [Range(0f, 0.1f)]
        private float m_CeilingCheckRadiusPadding = 0.02f;

        [Header("Turn In Place")]
        [SerializeField]
        private float m_TurnInPlaceThreshold = 5f;

        [SerializeField]
        private float m_TurnInPlaceSpeed = 120f;

        [Header("Ground")]
        [SerializeField]
        private LayerMask m_GroundLayerMask = ~0;

        [SerializeField]
        private float m_GroundProbeDistance = 0.2f;

        [SerializeField]
        private float m_GroundProbeStartOffset = 0.02f;

        [SerializeField]
        private float m_GroundProbeRadiusPadding = 0.05f;

        [SerializeField]
        private float m_MaxGroundAngle = 60f;

        private CharacterGroundProbe m_GroundProbe;
        private GroundInfo m_GroundInfo;

        private Vector3 m_StandingCenter;
        private float m_StandingHeight;

        private Vector3 m_HorizontalVelocity;
        private float m_VerticalVelocity;
        private float m_CameraRotationOffset;

        private uint m_ServerTick;
        private uint m_LastWalkToggleTick;
        private uint m_LastJumpPressedTick;
        private uint m_LastCrouchToggleTick;

        private bool m_IsWalking;
        private bool m_CrouchRequested;
        private bool m_IsCrouching;

        private void Awake()
        {
            m_GroundProbe = new CharacterGroundProbe(
                m_GroundLayerMask,
                m_GroundProbeDistance,
                m_GroundProbeStartOffset,
                m_GroundProbeRadiusPadding,
                m_MaxGroundAngle);

            m_StandingHeight = m_CharacterController.height;
            m_StandingCenter = m_CharacterController.center;
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                enabled = false;
                return;
            }

            m_CharacterController.enabled = true;
        }

        public override void OnNetworkDespawn()
        {
            if (!IsServer)
            {
                return;
            }

            ApplyStandingCapsule();

            m_HorizontalVelocity = Vector3.zero;
            m_VerticalVelocity = 0f;
            m_CameraRotationOffset = 0f;
            m_IsWalking = false;
            m_CrouchRequested = false;
            m_IsCrouching = false;

            m_CharacterController.enabled = false;
        }

        private void Update()
        {
            CharacterInputCommand command = m_ServerCharacter.InputCommand;

            ApplyWalkInput(command);
            ApplyCrouchInput(command);
            ApplyCrouchRequest();

            Vector3 movementDirection = GetMovementDirection(command);

            bool hasMovementInput =
                movementDirection.sqrMagnitude > 0.0001f;

            bool isSprinting = hasMovementInput &&
                               !m_IsCrouching &&
                               command.IsPressed(
                                   CharacterInputButtons.SprintHeld);

            if (isSprinting)
            {
                m_IsWalking = false;
            }

            bool isStrafing = !isSprinting &&
                              (m_AlwaysStrafe ||
                               command.IsPressed(
                                   CharacterInputButtons.AimHeld));

            if (m_GroundInfo.IsGrounded &&
                m_VerticalVelocity < 0f)
            {
                m_VerticalVelocity = -2f;
            }

            ApplyJumpInput(command);

            m_VerticalVelocity += Physics.gravity.y *
                                  m_GravityMultiplier *
                                  Time.deltaTime;

            float targetMoveSpeed = GetTargetMoveSpeed(
                isSprinting);

            Vector3 targetHorizontalVelocity =
                movementDirection * targetMoveSpeed;

            m_HorizontalVelocity = Vector3.Lerp(
                m_HorizontalVelocity,
                targetHorizontalVelocity,
                m_SpeedChangeDamping * Time.deltaTime);

            Vector3 velocity = m_HorizontalVelocity;
            velocity.y = m_VerticalVelocity;

            CollisionFlags collisionFlags = m_CharacterController.Move(
                velocity * Time.deltaTime);

            UpdateFacing(
                movementDirection,
                command.LookYaw,
                isStrafing,
                out bool isTurningInPlace);

            m_GroundInfo = m_GroundProbe.Probe(
                m_CharacterController,
                movementDirection);

            bool isGrounded = m_GroundInfo.IsGrounded ||
                              (collisionFlags &
                               CollisionFlags.Below) != 0;

            if (isGrounded &&
                m_VerticalVelocity < 0f)
            {
                m_VerticalVelocity = -2f;
            }

            m_ServerCharacter.LocomotionState.Value =
                new CharacterLocomotionState
                {
                    ServerTick = m_ServerTick++,
                    LastProcessedInputTick = command.Tick,
                    Velocity = velocity,
                    MoveInput = command.Move,
                    FacingYaw = transform.eulerAngles.y,
                    AimYaw = command.LookYaw,
                    AimPitch = command.LookPitch,
                    InclineAngle = m_GroundInfo.InclineAngle,
                    CameraRotationOffset = m_CameraRotationOffset,
                    Gait = GetGait(
                        m_HorizontalVelocity.magnitude),
                    IsGrounded = isGrounded,
                    IsStrafing = isStrafing,
                    IsTurningInPlace = isTurningInPlace,
                    IsJumping = m_VerticalVelocity > 0f,
                    IsCrouching = m_IsCrouching
                };
        }

        private void ApplyWalkInput(CharacterInputCommand command)
        {
            if (!command.IsPressed(CharacterInputButtons.WalkToggle) ||
                command.Tick == m_LastWalkToggleTick)
            {
                return;
            }

            m_LastWalkToggleTick = command.Tick;
            m_IsWalking = !m_IsWalking;
        }

        private void ApplyCrouchInput(CharacterInputCommand command)
        {
            if (!command.IsPressed(CharacterInputButtons.CrouchToggle) ||
                command.Tick == m_LastCrouchToggleTick)
            {
                return;
            }

            m_LastCrouchToggleTick = command.Tick;
            m_CrouchRequested = !m_CrouchRequested;
        }

        private void ApplyCrouchRequest()
        {
            if (m_CrouchRequested)
            {
                TryEnterCrouch();
            }
            else
            {
                TryExitCrouch();
            }
        }

        private bool TryEnterCrouch()
        {
            if (m_IsCrouching)
            {
                return true;
            }

            if (!m_GroundInfo.IsGrounded)
            {
                return false;
            }

            ApplyCrouchingCapsule();
            m_IsCrouching = true;
            return true;
        }

        private bool TryExitCrouch()
        {
            if (!m_IsCrouching)
            {
                return true;
            }

            if (!CanStandUp())
            {
                return false;
            }

            ApplyStandingCapsule();
            m_IsCrouching = false;
            return true;
        }

        private void ApplyCrouchingCapsule()
        {
            Vector3 crouchingCenter = m_StandingCenter;
            crouchingCenter.y = m_CrouchingCenter;

            m_CharacterController.height = m_CrouchingHeight;
            m_CharacterController.center = crouchingCenter;
        }

        private void ApplyStandingCapsule()
        {
            m_CharacterController.height = m_StandingHeight;
            m_CharacterController.center = m_StandingCenter;
        }

        private bool CanStandUp()
        {
            float currentHalfSegment = Mathf.Max(
                0f,
                m_CharacterController.height * 0.5f -
                m_CharacterController.radius);

            float standingHalfSegment = Mathf.Max(
                0f,
                m_StandingHeight * 0.5f -
                m_CharacterController.radius);

            Vector3 currentCenter = transform.TransformPoint(
                m_CharacterController.center);

            Vector3 standingCenter = transform.TransformPoint(
                m_StandingCenter);

            Vector3 currentTopSphereCenter = currentCenter +
                                             transform.up *
                                             currentHalfSegment;

            Vector3 standingTopSphereCenter = standingCenter +
                                              transform.up *
                                              standingHalfSegment;

            float requiredHeight = Vector3.Dot(
                standingTopSphereCenter - currentTopSphereCenter,
                transform.up);

            if (requiredHeight <= 0f)
            {
                return true;
            }

            float radius = Mathf.Max(
                0.01f,
                m_CharacterController.radius -
                m_CeilingCheckRadiusPadding);

            return !Physics.SphereCast(
                currentTopSphereCenter,
                radius,
                transform.up,
                out _,
                requiredHeight,
                m_GroundLayerMask,
                QueryTriggerInteraction.Ignore);
        }

        private void ApplyJumpInput(CharacterInputCommand command)
        {
            if (!command.IsPressed(CharacterInputButtons.JumpPressed) ||
                command.Tick == m_LastJumpPressedTick ||
                !m_GroundInfo.IsGrounded)
            {
                return;
            }

            m_LastJumpPressedTick = command.Tick;

            if (m_IsCrouching && !TryExitCrouch())
            {
                return;
            }

            m_CrouchRequested = false;
            m_VerticalVelocity = m_JumpForce;
        }

        private Vector3 GetMovementDirection(CharacterInputCommand command)
        {
            Quaternion lookRotation = Quaternion.Euler(
                0f,
                command.LookYaw,
                0f);

            Vector3 forward = lookRotation * Vector3.forward;
            Vector3 right = lookRotation * Vector3.right;

            Vector3 movementDirection = forward * command.Move.y +
                                        right * command.Move.x;

            return Vector3.ClampMagnitude(movementDirection, 1f);
        }

        private float GetTargetMoveSpeed(bool isSprinting)
        {
            if (!m_GroundInfo.IsGrounded)
            {
                return m_HorizontalVelocity.magnitude;
            }

            if (m_IsCrouching)
            {
                return m_WalkSpeed;
            }

            if (isSprinting)
            {
                return m_SprintSpeed;
            }

            return m_IsWalking
                ? m_WalkSpeed
                : m_RunSpeed;
        }

        private CharacterGait GetGait(float horizontalSpeed)
        {
            float runThreshold =
                (m_WalkSpeed + m_RunSpeed) * 0.5f;

            float sprintThreshold =
                (m_RunSpeed + m_SprintSpeed) * 0.5f;

            if (horizontalSpeed < 0.01f)
            {
                return CharacterGait.Idle;
            }

            if (horizontalSpeed < runThreshold)
            {
                return CharacterGait.Walk;
            }

            if (horizontalSpeed < sprintThreshold)
            {
                return CharacterGait.Run;
            }

            return CharacterGait.Sprint;
        }

        private void UpdateFacing(
            Vector3 movementDirection,
            float lookYaw,
            bool isStrafing,
            out bool isTurningInPlace)
        {
            isTurningInPlace = false;

            Quaternion lookRotation = Quaternion.Euler(
                0f,
                lookYaw,
                0f);

            if (isStrafing)
            {
                if (movementDirection.sqrMagnitude > 0.0001f)
                {
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        lookRotation,
                        m_RotationSmoothing * Time.deltaTime);

                    m_CameraRotationOffset = Mathf.Lerp(
                        m_CameraRotationOffset,
                        0f,
                        m_RotationSmoothing * Time.deltaTime);

                    return;
                }

                float targetOffset = Mathf.DeltaAngle(
                    transform.eulerAngles.y,
                    lookYaw);

                m_CameraRotationOffset = Mathf.Lerp(
                    m_CameraRotationOffset,
                    targetOffset,
                    m_RotationSmoothing * Time.deltaTime);

                if (Mathf.Abs(targetOffset) <
                    m_TurnInPlaceThreshold)
                {
                    return;
                }

                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    lookRotation,
                    m_TurnInPlaceSpeed * Time.deltaTime);

                isTurningInPlace = true;
                return;
            }

            m_CameraRotationOffset = Mathf.Lerp(
                m_CameraRotationOffset,
                0f,
                m_RotationSmoothing * Time.deltaTime);

            if (movementDirection.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Quaternion movementRotation = Quaternion.LookRotation(
                movementDirection,
                Vector3.up);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                movementRotation,
                m_RotationSmoothing * Time.deltaTime);
        }
    }
}