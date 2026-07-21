using System;
using RelicsOfTheFallen.Character;
using UnityEngine;

namespace RelicsOfTheFallen.Character.Movement
{
    public sealed class CharacterLocomotionController :
        MonoBehaviour,
        ICharacterLocomotionStateProvider
    {
        [Header("References")]
        [SerializeField]
        CharacterControllerMotor m_CharacterControllerMotor;

        [Header("Movement")]
        [SerializeField]
        bool m_AlwaysStrafe = true;

        [SerializeField]
        float m_WalkSpeed = 1.4f;

        [SerializeField]
        float m_RunSpeed = 5f;

        [SerializeField]
        float m_SprintSpeed = 7f;

        [SerializeField]
        float m_SpeedChangeDamping = 10f;

        [SerializeField]
        float m_JumpForce = 10f;

        [SerializeField]
        float m_GravityMultiplier = 2f;

        [SerializeField]
        float m_RotationSmoothing = 10f;

        [SerializeField]
        [Min(0f)]
        float m_MovementStartSpeed = 0.05f;

        [Header("Turn In Place")]
        [SerializeField]
        float m_TurnInPlaceThreshold = 5f;

        [SerializeField]
        float m_TurnInPlaceSpeed = 120f;

        CharacterLocomotionModel m_LocomotionModel;

        ushort m_LocomotionStartSequence;
        bool m_WasActuallyMoving;

        public CharacterLocomotionState LocomotionState { get; private set; }

        public event Action<CharacterLocomotionState>
            LocomotionStateChanged;

        void Awake()
        {
            CharacterLocomotionConfiguration configuration =
                new CharacterLocomotionConfiguration(
                    m_WalkSpeed,
                    m_RunSpeed,
                    m_SprintSpeed,
                    m_SpeedChangeDamping,
                    m_JumpForce,
                    m_GravityMultiplier,
                    m_RotationSmoothing,
                    m_TurnInPlaceThreshold,
                    m_TurnInPlaceSpeed);

            m_LocomotionModel =
                new CharacterLocomotionModel(configuration);

            enabled = false;
        }

        public void SetSimulationEnabled(bool isEnabled)
        {
            if (enabled == isEnabled)
            {
                return;
            }

            if (isEnabled)
            {
                m_LocomotionStartSequence = 0;
                m_WasActuallyMoving = false;

                m_LocomotionModel.Reset();

                m_CharacterControllerMotor
                    .SetControllerEnabled(true);

                enabled = true;

                return;
            }

            enabled = false;

            m_CharacterControllerMotor
                .ApplyStandingCapsule();

            m_CharacterControllerMotor
                .SetControllerEnabled(false);

            m_LocomotionModel.Reset();

            LocomotionState = default;
        }

        public void Simulate(
            in CharacterInputCommand inputCommand,
            float deltaTime)
        {
            if (!enabled)
            {
                return;
            }

            CharacterMotorCommand motorCommand =
                PlayerCharacterMotorCommandFactory.Create(
                    inputCommand,
                    m_AlwaysStrafe);

            CharacterMotorEnvironment environment =
                new CharacterMotorEnvironment(
                    m_CharacterControllerMotor
                        .GroundInfo
                        .IsGrounded,
                    m_CharacterControllerMotor.CanStandUp(),
                    transform.rotation);

            CharacterMotorIntent motorIntent =
                m_LocomotionModel.Simulate(
                    motorCommand,
                    environment,
                    deltaTime);

            m_CharacterControllerMotor.ApplyCapsule(
                motorIntent.IsCrouching);

            CharacterMotorMoveResult moveResult =
                m_CharacterControllerMotor.Move(
                    motorIntent.Velocity,
                    deltaTime,
                    motorCommand.MovementDirection);

            transform.rotation = motorIntent.Rotation;

            m_LocomotionModel.ApplyMoveResult(moveResult);

            m_CharacterControllerMotor.ApplyCapsule(
                m_LocomotionModel.IsCrouching);

            UpdateLocomotionStartSequence(
                m_LocomotionModel.ActualVelocity);

            LocomotionState = new CharacterLocomotionState
            {
                LocomotionStartSequence =
                    m_LocomotionStartSequence,
                Velocity =
                    m_LocomotionModel.ActualVelocity,
                MoveInput = inputCommand.Move,
                FacingYaw = transform.eulerAngles.y,
                AimYaw = motorCommand.FacingYaw,
                AimPitch = inputCommand.LookPitch,
                InclineAngle =
                    m_CharacterControllerMotor
                        .GroundInfo
                        .InclineAngle,
                CameraRotationOffset =
                    motorIntent.CameraRotationOffset,
                Gait = m_LocomotionModel.CurrentGait,
                IsGrounded = moveResult.IsGrounded,
                IsStrafing = motorIntent.IsStrafing,
                IsTurningInPlace =
                    motorIntent.IsTurningInPlace,
                IsJumping =
                    m_LocomotionModel.IsJumping,
                IsCrouching =
                    m_LocomotionModel.IsCrouching
            };

            LocomotionStateChanged?.Invoke(LocomotionState);
        }

        void UpdateLocomotionStartSequence(
            Vector3 actualVelocity)
        {
            Vector3 horizontalVelocity = actualVelocity;
            horizontalVelocity.y = 0f;

            bool isActuallyMoving =
                horizontalVelocity.sqrMagnitude >
                m_MovementStartSpeed *
                m_MovementStartSpeed;

            if (isActuallyMoving &&
                !m_WasActuallyMoving)
            {
                m_LocomotionStartSequence++;
            }

            m_WasActuallyMoving = isActuallyMoving;
        }
    }
}