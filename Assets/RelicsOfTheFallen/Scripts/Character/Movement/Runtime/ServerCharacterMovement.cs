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
        private CharacterControllerMotor m_CharacterControllerMotor;

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

        [Header("Turn In Place")]
        [SerializeField]
        private float m_TurnInPlaceThreshold = 5f;

        [SerializeField]
        private float m_TurnInPlaceSpeed = 120f;

        private CharacterLocomotionConfiguration
            m_LocomotionConfiguration;

        private CharacterLocomotionModel m_LocomotionModel;

        public CharacterLocomotionConfiguration
            LocomotionConfiguration =>
                m_LocomotionConfiguration;

        public CharacterControllerMotor
            CharacterControllerMotor =>
                m_CharacterControllerMotor;

        public bool AlwaysStrafe => m_AlwaysStrafe;

        private void Awake()
        {
            m_LocomotionConfiguration =
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
                new CharacterLocomotionModel(
                    m_LocomotionConfiguration);
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                enabled = false;
                return;
            }

            m_CharacterControllerMotor
                .SetControllerEnabled(true);

            NetworkManager.NetworkTickSystem.Tick +=
                OnNetworkTick;
        }

        public override void OnNetworkDespawn()
        {
            if (!IsServer)
            {
                return;
            }

            NetworkManager.NetworkTickSystem.Tick -=
                OnNetworkTick;

            m_CharacterControllerMotor
                .ApplyStandingCapsule();

            m_LocomotionModel.Reset();

            m_CharacterControllerMotor
                .SetControllerEnabled(false);
        }

        private void OnNetworkTick()
        {
            float deltaTime = 1f /
                              NetworkManager.NetworkTickSystem
                                  .TickRate;

            CharacterInputCommand inputCommand =
                m_ServerCharacter.ConsumeInputCommand();

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

            CharacterLocomotionState locomotionState =
                new CharacterLocomotionState
                {
                    ServerTick = unchecked(
                        (uint)NetworkManager.NetworkTickSystem
                            .ServerTime.Tick),
                    LastProcessedInputSequence =
                        inputCommand.Sequence,
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

            CharacterLocomotionSimulationState
                simulationState =
                    m_LocomotionModel.CaptureState();

            m_ServerCharacter.LocomotionState.Value =
                locomotionState;

            m_ServerCharacter.OwnerSnapshot.Value =
                new CharacterOwnerSnapshot
                {
                    ServerTick = locomotionState.ServerTick,
                    LastProcessedInputSequence =
                        locomotionState
                            .LastProcessedInputSequence,
                    Position = transform.position,
                    Rotation = transform.rotation,
                    HorizontalVelocity =
                        simulationState.HorizontalVelocity,
                    VerticalVelocity =
                        simulationState.VerticalVelocity,
                    CameraRotationOffset =
                        simulationState.CameraRotationOffset,
                    IsWalking = simulationState.IsWalking,
                    IsCrouchRequested =
                        simulationState.IsCrouchRequested,
                    IsCrouching =
                        simulationState.IsCrouching,
                    IsGrounded = moveResult.IsGrounded,
                    AirState = simulationState.AirState
                };
        }
    }
}