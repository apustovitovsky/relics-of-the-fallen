using RelicsOfTheFallen.Character;
using UnityEngine;

namespace RelicsOfTheFallen.Character.Movement
{
    public readonly struct CharacterLocomotionConfiguration
    {
        public readonly float WalkSpeed;
        public readonly float RunSpeed;
        public readonly float SprintSpeed;
        public readonly float SpeedChangeDamping;
        public readonly float JumpForce;
        public readonly float GravityMultiplier;
        public readonly float RotationSmoothing;
        public readonly float TurnInPlaceThreshold;
        public readonly float TurnInPlaceSpeed;

        public CharacterLocomotionConfiguration(
            float walkSpeed,
            float runSpeed,
            float sprintSpeed,
            float speedChangeDamping,
            float jumpForce,
            float gravityMultiplier,
            float rotationSmoothing,
            float turnInPlaceThreshold,
            float turnInPlaceSpeed)
        {
            WalkSpeed = walkSpeed;
            RunSpeed = runSpeed;
            SprintSpeed = sprintSpeed;
            SpeedChangeDamping = speedChangeDamping;
            JumpForce = jumpForce;
            GravityMultiplier = gravityMultiplier;
            RotationSmoothing = rotationSmoothing;
            TurnInPlaceThreshold = turnInPlaceThreshold;
            TurnInPlaceSpeed = turnInPlaceSpeed;
        }
    }

    public readonly struct CharacterMotorEnvironment
    {
        public readonly bool IsGrounded;
        public readonly bool CanStandUp;
        public readonly Quaternion CurrentRotation;

        public CharacterMotorEnvironment(
            bool isGrounded,
            bool canStandUp,
            Quaternion currentRotation)
        {
            IsGrounded = isGrounded;
            CanStandUp = canStandUp;
            CurrentRotation = currentRotation;
        }
    }

    public readonly struct CharacterMotorIntent
    {
        public readonly Vector3 Velocity;
        public readonly Quaternion Rotation;

        public readonly bool IsWalking;
        public readonly bool IsCrouching;
        public readonly bool IsSprinting;
        public readonly bool IsStrafing;
        public readonly bool IsTurningInPlace;
        public readonly bool IsJumping;

        public readonly float CameraRotationOffset;

        public CharacterMotorIntent(
            Vector3 velocity,
            Quaternion rotation,
            bool isWalking,
            bool isCrouching,
            bool isSprinting,
            bool isStrafing,
            bool isTurningInPlace,
            bool isJumping,
            float cameraRotationOffset)
        {
            Velocity = velocity;
            Rotation = rotation;
            IsWalking = isWalking;
            IsCrouching = isCrouching;
            IsSprinting = isSprinting;
            IsStrafing = isStrafing;
            IsTurningInPlace = isTurningInPlace;
            IsJumping = isJumping;
            CameraRotationOffset = cameraRotationOffset;
        }
    }

    public readonly struct CharacterMotorMoveResult
    {
        public readonly Vector3 ActualVelocity;
        public readonly bool IsGrounded;

        public CharacterMotorMoveResult(
            Vector3 actualVelocity,
            bool isGrounded)
        {
            ActualVelocity = actualVelocity;
            IsGrounded = isGrounded;
        }
    }

    public readonly struct CharacterLocomotionSimulationState
    {
        public readonly Vector3 HorizontalVelocity;
        public readonly float VerticalVelocity;
        public readonly float CameraRotationOffset;

        public readonly bool IsWalking;
        public readonly bool IsCrouchRequested;
        public readonly bool IsCrouching;

        public readonly CharacterAirState AirState;

        public CharacterLocomotionSimulationState(
            Vector3 horizontalVelocity,
            float verticalVelocity,
            float cameraRotationOffset,
            bool isWalking,
            bool isCrouchRequested,
            bool isCrouching,
            CharacterAirState airState)
        {
            HorizontalVelocity = horizontalVelocity;
            VerticalVelocity = verticalVelocity;
            CameraRotationOffset = cameraRotationOffset;
            IsWalking = isWalking;
            IsCrouchRequested = isCrouchRequested;
            IsCrouching = isCrouching;
            AirState = airState;
        }
    }

    /// <summary>
    /// Authoritative locomotion state machine.
    /// It is independent from NGO, Input System, Animator and
    /// CharacterController so the same simulation can later serve
    /// server authority, owner prediction and AI.
    /// </summary>
    public sealed class CharacterLocomotionModel
    {
        private readonly CharacterLocomotionConfiguration m_Configuration;

        private Vector3 m_HorizontalVelocity;
        private Vector3 m_ActualVelocity;
        private float m_VerticalVelocity;
        private float m_CameraRotationOffset;

        private bool m_IsWalking;
        private bool m_CrouchRequested;
        private bool m_IsCrouching;

        private CharacterAirState m_AirState;

        public bool IsCrouching => m_IsCrouching;

        public bool IsJumping =>
            m_AirState == CharacterAirState.Jumping;

        public float VerticalVelocity => m_VerticalVelocity;

        public Vector3 ActualVelocity => m_ActualVelocity;

        public CharacterGait CurrentGait
        {
            get
            {
                Vector3 horizontalVelocity = m_ActualVelocity;
                horizontalVelocity.y = 0f;

                return GetGait(horizontalVelocity.magnitude);
            }
        }

        public CharacterLocomotionModel(
            CharacterLocomotionConfiguration configuration)
        {
            m_Configuration = configuration;
        }

        public CharacterMotorIntent Simulate(
            in CharacterMotorCommand command,
            in CharacterMotorEnvironment environment,
            float deltaTime)
        {
            ApplyWalkInput(command);
            ApplyCrouchInput(command);
            ApplyCrouchRequest(environment);

            bool hasMovementInput =
                command.MovementDirection.sqrMagnitude > 0.0001f;

            bool isSprinting = hasMovementInput &&
                               !m_IsCrouching &&
                               command.WantsSprint;

            if (isSprinting)
            {
                m_IsWalking = false;
            }

            bool isStrafing = !isSprinting &&
                              command.WantsStrafe;

            if (environment.IsGrounded &&
                m_VerticalVelocity < 0f)
            {
                m_VerticalVelocity = -2f;
            }

            ApplyJumpInput(command, environment);

            m_VerticalVelocity += Physics.gravity.y *
                                  m_Configuration.GravityMultiplier *
                                  deltaTime;

            float targetMoveSpeed = GetTargetMoveSpeed(
                environment.IsGrounded,
                isSprinting);

            Vector3 targetHorizontalVelocity =
                command.MovementDirection * targetMoveSpeed;

            m_HorizontalVelocity = Vector3.Lerp(
                m_HorizontalVelocity,
                targetHorizontalVelocity,
                m_Configuration.SpeedChangeDamping * deltaTime);

            Vector3 velocity = m_HorizontalVelocity;
            velocity.y = m_VerticalVelocity;

            Quaternion rotation = CalculateRotation(
                command.MovementDirection,
                command.FacingYaw,
                isStrafing,
                environment.CurrentRotation,
                deltaTime,
                out bool isTurningInPlace);

            return new CharacterMotorIntent(
                velocity,
                rotation,
                m_IsWalking,
                m_IsCrouching,
                isSprinting,
                isStrafing,
                isTurningInPlace,
                IsJumping,
                m_CameraRotationOffset);
        }

        public void ApplyMoveResult(
            in CharacterMotorMoveResult moveResult)
        {
            m_ActualVelocity = moveResult.ActualVelocity;

            if (moveResult.IsGrounded)
            {
                if (m_VerticalVelocity < 0f)
                {
                    m_VerticalVelocity = -2f;
                }

                m_AirState = CharacterAirState.Grounded;
                return;
            }

            if (m_VerticalVelocity > 0f)
            {
                m_AirState = CharacterAirState.Jumping;
                return;
            }

            if (m_AirState == CharacterAirState.Falling)
            {
                return;
            }

            EnterFallState();
        }

        public CharacterLocomotionSimulationState
            CaptureState()
        {
            return new CharacterLocomotionSimulationState(
                m_HorizontalVelocity,
                m_VerticalVelocity,
                m_CameraRotationOffset,
                m_IsWalking,
                m_CrouchRequested,
                m_IsCrouching,
                m_AirState);
        }

        public void RestoreState(
            in CharacterLocomotionSimulationState state)
        {
            m_HorizontalVelocity = state.HorizontalVelocity;
            m_VerticalVelocity = state.VerticalVelocity;
            m_CameraRotationOffset =
                state.CameraRotationOffset;

            m_IsWalking = state.IsWalking;
            m_CrouchRequested =
                state.IsCrouchRequested;

            m_IsCrouching = state.IsCrouching;
            m_AirState = state.AirState;

            m_ActualVelocity = Vector3.zero;
        }

        public void Reset()
        {
            m_HorizontalVelocity = Vector3.zero;
            m_ActualVelocity = Vector3.zero;
            m_VerticalVelocity = 0f;
            m_CameraRotationOffset = 0f;

            m_IsWalking = false;
            m_CrouchRequested = false;
            m_IsCrouching = false;
            m_AirState = CharacterAirState.Grounded;
        }

        private void ApplyWalkInput(
            in CharacterMotorCommand command)
        {
            if (command.ToggleWalk)
            {
                m_IsWalking = !m_IsWalking;
            }
        }

        private void ApplyCrouchInput(
            in CharacterMotorCommand command)
        {
            if (command.ToggleCrouch)
            {
                m_CrouchRequested = !m_CrouchRequested;
            }
        }

        private void ApplyCrouchRequest(
            in CharacterMotorEnvironment environment)
        {
            if (m_CrouchRequested)
            {
                if (environment.IsGrounded)
                {
                    m_IsCrouching = true;
                }

                return;
            }

            if (environment.CanStandUp)
            {
                m_IsCrouching = false;
            }
        }

        private void ApplyJumpInput(
            in CharacterMotorCommand command,
            in CharacterMotorEnvironment environment)
        {
            if (!command.JumpPressed ||
                !environment.IsGrounded)
            {
                return;
            }

            if (m_IsCrouching &&
                !environment.CanStandUp)
            {
                return;
            }

            m_IsCrouching = false;
            m_CrouchRequested = false;
            m_VerticalVelocity = m_Configuration.JumpForce;
            m_AirState = CharacterAirState.Jumping;
        }

        private void EnterFallState()
        {
            m_AirState = CharacterAirState.Falling;
            m_VerticalVelocity = 0f;

            // Legacy behaviour: leaving the ground always cancels crouch.
            m_CrouchRequested = false;
            m_IsCrouching = false;
        }

        private float GetTargetMoveSpeed(
            bool isGrounded,
            bool isSprinting)
        {
            if (!isGrounded)
            {
                return m_HorizontalVelocity.magnitude;
            }

            if (m_IsCrouching)
            {
                return m_Configuration.WalkSpeed;
            }

            if (isSprinting)
            {
                return m_Configuration.SprintSpeed;
            }

            return m_IsWalking
                ? m_Configuration.WalkSpeed
                : m_Configuration.RunSpeed;
        }

        private CharacterGait GetGait(float horizontalSpeed)
        {
            float runThreshold =
                (m_Configuration.WalkSpeed +
                 m_Configuration.RunSpeed) * 0.5f;

            float sprintThreshold =
                (m_Configuration.RunSpeed +
                 m_Configuration.SprintSpeed) * 0.5f;

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

        private Quaternion CalculateRotation(
            Vector3 movementDirection,
            float facingYaw,
            bool isStrafing,
            Quaternion currentRotation,
            float deltaTime,
            out bool isTurningInPlace)
        {
            isTurningInPlace = false;

            Quaternion facingRotation = Quaternion.Euler(
                0f,
                facingYaw,
                0f);

            if (isStrafing)
            {
                if (movementDirection.sqrMagnitude > 0.0001f)
                {
                    m_CameraRotationOffset = Mathf.Lerp(
                        m_CameraRotationOffset,
                        0f,
                        m_Configuration.RotationSmoothing *
                        deltaTime);

                    return Quaternion.Slerp(
                        currentRotation,
                        facingRotation,
                        m_Configuration.RotationSmoothing *
                        deltaTime);
                }

                float targetOffset = Mathf.DeltaAngle(
                    currentRotation.eulerAngles.y,
                    facingYaw);

                m_CameraRotationOffset = Mathf.Lerp(
                    m_CameraRotationOffset,
                    targetOffset,
                    m_Configuration.RotationSmoothing *
                    deltaTime);

                if (Mathf.Abs(targetOffset) <
                    m_Configuration.TurnInPlaceThreshold)
                {
                    return currentRotation;
                }

                isTurningInPlace = true;

                return Quaternion.RotateTowards(
                    currentRotation,
                    facingRotation,
                    m_Configuration.TurnInPlaceSpeed *
                    deltaTime);
            }

            m_CameraRotationOffset = Mathf.Lerp(
                m_CameraRotationOffset,
                0f,
                m_Configuration.RotationSmoothing *
                deltaTime);

            if (movementDirection.sqrMagnitude <= 0.0001f)
            {
                return currentRotation;
            }

            Quaternion movementRotation =
                Quaternion.LookRotation(
                    movementDirection,
                    Vector3.up);

            return Quaternion.Slerp(
                currentRotation,
                movementRotation,
                m_Configuration.RotationSmoothing *
                deltaTime);
        }
    }
}