using RelicsOfTheFallen.Character;
using UnityEngine;

namespace RelicsOfTheFallen.Character.Movement
{
    public sealed class CharacterLocomotionPresenter : MonoBehaviour
    {
        static readonly int s_MovementInputTapped =
            Animator.StringToHash("MovementInputTapped");

        static readonly int s_MovementInputPressed =
            Animator.StringToHash("MovementInputPressed");

        static readonly int s_MovementInputHeld =
            Animator.StringToHash("MovementInputHeld");

        static readonly int s_MoveSpeed =
            Animator.StringToHash("MoveSpeed");

        static readonly int s_CurrentGait =
            Animator.StringToHash("CurrentGait");

        static readonly int s_IsJumping =
            Animator.StringToHash("IsJumping");

        static readonly int s_IsCrouching =
            Animator.StringToHash("IsCrouching");

        static readonly int s_InclineAngle =
            Animator.StringToHash("InclineAngle");

        static readonly int s_StrafeDirectionX =
            Animator.StringToHash("StrafeDirectionX");

        static readonly int s_StrafeDirectionZ =
            Animator.StringToHash("StrafeDirectionZ");

        static readonly int s_ForwardStrafe =
            Animator.StringToHash("ForwardStrafe");

        static readonly int s_IsStrafing =
            Animator.StringToHash("IsStrafing");

        static readonly int s_CameraRotationOffset =
            Animator.StringToHash("CameraRotationOffset");

        static readonly int s_IsTurningInPlace =
            Animator.StringToHash("IsTurningInPlace");

        static readonly int s_IsWalking =
            Animator.StringToHash("IsWalking");

        static readonly int s_IsStopped =
            Animator.StringToHash("IsStopped");

        static readonly int s_IsStarting =
            Animator.StringToHash("IsStarting");

        static readonly int s_IsGrounded =
            Animator.StringToHash("IsGrounded");

        static readonly int s_FallingDuration =
            Animator.StringToHash("FallingDuration");

        [SerializeField]
        private ClientCharacter m_ClientCharacter;

        [SerializeField]
        private Animator m_Animator;

        [SerializeField]
        private float m_SpeedDampTime = 0.1f;

        [SerializeField]
        private float m_ButtonHoldThreshold = 0.15f;

        [SerializeField]
        private float m_StrafeDirectionDampTime = 0.2f;

        [SerializeField]
        private float m_ForwardStrafeMinThreshold = -55f;

        [SerializeField]
        private float m_ForwardStrafeMaxThreshold = 125f;

        private float m_FallStartTime;
        private float m_MovementInputStartTime;
        private float m_StrafeDirectionX;
        private float m_StrafeDirectionZ = 1f;
        private float m_ForwardStrafe = 1f;
        private bool m_WasGrounded;
        private bool m_WasMoving;

        private void Awake()
        {
            if (m_ClientCharacter == null)
            {
                m_ClientCharacter =
                    GetComponentInParent<ClientCharacter>();
            }

            if (m_Animator == null)
            {
                m_Animator = GetComponent<Animator>();
            }
        }

        private void OnEnable()
        {
            m_WasGrounded = true;
            m_WasMoving = false;
            m_FallStartTime = Time.time;
            m_MovementInputStartTime = 0f;
            m_StrafeDirectionX = 0f;
            m_StrafeDirectionZ = 1f;
            m_ForwardStrafe = 1f;
        }

        private void OnDisable()
        {
            if (m_Animator == null)
            {
                return;
            }

            m_Animator.SetBool(
                s_MovementInputTapped,
                false);

            m_Animator.SetBool(
                s_MovementInputPressed,
                false);

            m_Animator.SetBool(
                s_MovementInputHeld,
                false);

            m_Animator.SetBool(s_IsJumping, false);
            m_Animator.SetBool(s_IsCrouching, false);

            m_Animator.SetFloat(s_StrafeDirectionX, 0f);
            m_Animator.SetFloat(s_StrafeDirectionZ, 1f);
            m_Animator.SetFloat(s_ForwardStrafe, 1f);
            m_Animator.SetFloat(s_IsStrafing, 0f);

            m_Animator.SetFloat(
                s_CameraRotationOffset,
                0f);

            m_Animator.SetBool(
                s_IsTurningInPlace,
                false);

            m_Animator.SetBool(s_IsWalking, false);
            m_Animator.SetBool(s_IsStopped, true);
            m_Animator.SetBool(s_IsStarting, false);
            m_Animator.SetFloat(s_MoveSpeed, 0f);

            m_Animator.SetInteger(
                s_CurrentGait,
                (int)CharacterGait.Idle);

            m_Animator.SetFloat(s_InclineAngle, 0f);
            m_Animator.SetBool(s_IsGrounded, true);
            m_Animator.SetFloat(s_FallingDuration, 0f);
        }

        private void Update()
        {
            if (m_ClientCharacter == null ||
                m_Animator == null)
            {
                return;
            }

            CharacterLocomotionState locomotionState =
                m_ClientCharacter.LocomotionState;

            bool isMoving =
                locomotionState.MoveInput.sqrMagnitude >
                0.0001f;

            bool movementStarted =
                isMoving && !m_WasMoving;

            if (movementStarted)
            {
                m_MovementInputStartTime = Time.time;
            }

            float movementInputDuration =
                isMoving
                    ? Time.time - m_MovementInputStartTime
                    : 0f;

            bool movementPressed =
                isMoving &&
                !movementStarted &&
                movementInputDuration <
                m_ButtonHoldThreshold;

            bool movementHeld =
                isMoving &&
                movementInputDuration >=
                m_ButtonHoldThreshold;

            Vector3 horizontalVelocity =
                locomotionState.Velocity;

            horizontalVelocity.y = 0f;

            Vector3 localVelocity =
                Quaternion.Inverse(
                    Quaternion.Euler(
                        0f,
                        locomotionState.FacingYaw,
                        0f)) *
                horizontalVelocity;

            Vector3 localDirection =
                localVelocity.sqrMagnitude > 0.0001f
                    ? localVelocity.normalized
                    : Vector3.forward;

            UpdateStrafeParameters(
                locomotionState.IsStrafing,
                localDirection);

            m_Animator.SetBool(
                s_MovementInputTapped,
                movementStarted);

            m_Animator.SetBool(
                s_MovementInputPressed,
                movementPressed);

            m_Animator.SetBool(
                s_MovementInputHeld,
                movementHeld);

            m_Animator.SetBool(
                s_IsJumping,
                locomotionState.IsJumping);

            m_Animator.SetBool(
                s_IsCrouching,
                locomotionState.IsCrouching);

            m_Animator.SetBool(
                s_IsWalking,
                locomotionState.Gait ==
                CharacterGait.Walk);

            m_Animator.SetBool(
                s_IsStopped,
                !isMoving);

            m_Animator.SetBool(
                s_IsStarting,
                movementStarted);

            m_Animator.SetFloat(
                s_MoveSpeed,
                horizontalVelocity.magnitude,
                m_SpeedDampTime,
                Time.deltaTime);

            m_Animator.SetInteger(
                s_CurrentGait,
                (int)locomotionState.Gait);

            m_Animator.SetFloat(
                s_InclineAngle,
                locomotionState.InclineAngle);

            m_Animator.SetFloat(
                s_CameraRotationOffset,
                locomotionState.CameraRotationOffset);

            m_Animator.SetBool(
                s_IsTurningInPlace,
                locomotionState.IsTurningInPlace);

            if (!locomotionState.IsGrounded &&
                m_WasGrounded)
            {
                m_FallStartTime = Time.time;
            }

            m_Animator.SetBool(
                s_IsGrounded,
                locomotionState.IsGrounded);

            m_Animator.SetFloat(
                s_FallingDuration,
                locomotionState.IsGrounded
                    ? 0f
                    : Time.time - m_FallStartTime);

            m_WasGrounded =
                locomotionState.IsGrounded;

            m_WasMoving = isMoving;
        }

        private void UpdateStrafeParameters(
            bool isStrafing,
            Vector3 localDirection)
        {
            float targetDirectionX =
                isStrafing
                    ? localDirection.x
                    : 0f;

            float targetDirectionZ =
                isStrafing
                    ? localDirection.z
                    : 1f;

            float dampFactor =
                Time.deltaTime /
                Mathf.Max(
                    0.0001f,
                    m_StrafeDirectionDampTime);

            m_StrafeDirectionX = Mathf.Lerp(
                m_StrafeDirectionX,
                targetDirectionX,
                dampFactor);

            m_StrafeDirectionZ = Mathf.Lerp(
                m_StrafeDirectionZ,
                targetDirectionZ,
                dampFactor);

            float strafeAngle =
                Mathf.Atan2(
                    localDirection.x,
                    localDirection.z) *
                Mathf.Rad2Deg;

            float targetForwardStrafe =
                strafeAngle > m_ForwardStrafeMinThreshold &&
                strafeAngle < m_ForwardStrafeMaxThreshold
                    ? 1f
                    : 0f;

            m_ForwardStrafe = Mathf.Lerp(
                m_ForwardStrafe,
                targetForwardStrafe,
                dampFactor);

            m_Animator.SetFloat(
                s_StrafeDirectionX,
                m_StrafeDirectionX);

            m_Animator.SetFloat(
                s_StrafeDirectionZ,
                m_StrafeDirectionZ);

            m_Animator.SetFloat(
                s_ForwardStrafe,
                m_ForwardStrafe);

            m_Animator.SetFloat(
                s_IsStrafing,
                isStrafing ? 1f : 0f);
        }
    }
}