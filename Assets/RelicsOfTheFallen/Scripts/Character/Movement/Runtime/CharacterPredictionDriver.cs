using RelicsOfTheFallen.Character;
using Unity.Netcode;
using UnityEngine;

namespace RelicsOfTheFallen.Character.Movement
{
    /// <summary>
    /// Predicts only the owning remote client's character.
    /// The server remains authoritative and periodically restores an
    /// owner snapshot before unacknowledged commands are replayed.
    /// </summary>
    public sealed class CharacterPredictionDriver :
        NetworkBehaviour
    {
        [Header("References")]
        [SerializeField]
        private ServerCharacter m_ServerCharacter;

        [SerializeField]
        private ServerCharacterMovement m_ServerCharacterMovement;

        [SerializeField]
        private CharacterControllerMotor m_CharacterControllerMotor;

        [SerializeField]
        private MonoBehaviour m_InputHistoryProvider;

        private ICharacterInputHistoryProvider
            m_TypedInputHistoryProvider;

        private CharacterLocomotionModel m_LocomotionModel;
        private bool m_IsPredictedGrounded;
        private bool m_IsPredictionActive;

        private void Awake()
        {
            m_TypedInputHistoryProvider =
                m_InputHistoryProvider as
                ICharacterInputHistoryProvider;
        }

        public override void OnNetworkSpawn()
        {
            if (!IsClient || !IsOwner || IsServer)
            {
                enabled = false;
                return;
            }

            if (m_TypedInputHistoryProvider == null)
            {
                Debug.LogError(
                    $"{nameof(CharacterPredictionDriver)} " +
                    "requires an input history provider.",
                    this);

                enabled = false;
                return;
            }

            m_LocomotionModel =
                new CharacterLocomotionModel(
                    m_ServerCharacterMovement
                        .LocomotionConfiguration);

            m_CharacterControllerMotor
                .SetControllerEnabled(true);

            m_ServerCharacter.OwnerSnapshot.OnValueChanged +=
                OnOwnerSnapshotChanged;

            m_TypedInputHistoryProvider.CommandRecorded +=
                OnCommandRecorded;

            m_IsPredictionActive = true;

            RestoreAndReplay(
                m_ServerCharacter.OwnerSnapshot.Value);
        }

        public override void OnNetworkDespawn()
        {
            if (!m_IsPredictionActive)
            {
                return;
            }

            m_ServerCharacter.OwnerSnapshot.OnValueChanged -=
                OnOwnerSnapshotChanged;

            m_TypedInputHistoryProvider.CommandRecorded -=
                OnCommandRecorded;

            m_CharacterControllerMotor
                .ApplyStandingCapsule();

            m_CharacterControllerMotor
                .SetControllerEnabled(false);

            m_LocomotionModel.Reset();

            m_IsPredictionActive = false;
        }

        private void OnCommandRecorded(
            CharacterInputCommand command)
        {
            Simulate(command);
        }

        private void OnOwnerSnapshotChanged(
            CharacterOwnerSnapshot previousValue,
            CharacterOwnerSnapshot newValue)
        {
            RestoreAndReplay(newValue);
        }

        private void RestoreAndReplay(
            CharacterOwnerSnapshot snapshot)
        {
            if (snapshot.ServerTick == 0 &&
                snapshot.LastProcessedInputSequence == 0)
            {
                return;
            }

            transform.SetPositionAndRotation(
                snapshot.Position,
                snapshot.Rotation);

            m_IsPredictedGrounded = snapshot.IsGrounded;

            m_LocomotionModel.RestoreState(
                new CharacterLocomotionSimulationState(
                    snapshot.HorizontalVelocity,
                    snapshot.VerticalVelocity,
                    snapshot.CameraRotationOffset,
                    snapshot.IsWalking,
                    snapshot.IsCrouchRequested,
                    snapshot.IsCrouching,
                    snapshot.AirState));

            m_CharacterControllerMotor.ApplyCapsule(
                snapshot.IsCrouching);

            CharacterInputHistory inputHistory =
                m_TypedInputHistoryProvider.SentInputHistory;

            for (int index = 0;
                 index < inputHistory.Count;
                 index++)
            {
                if (!inputHistory.TryGet(
                        index,
                        out CharacterInputCommand command) ||
                    command.Sequence <=
                    snapshot.LastProcessedInputSequence)
                {
                    continue;
                }

                Simulate(command);
            }
        }

        private void Simulate(
            in CharacterInputCommand inputCommand)
        {
            float deltaTime = 1f /
                              NetworkManager.NetworkTickSystem
                                  .TickRate;

            CharacterMotorCommand motorCommand =
                PlayerCharacterMotorCommandFactory.Create(
                    inputCommand,
                    m_ServerCharacterMovement.AlwaysStrafe);

            CharacterMotorEnvironment environment =
                new CharacterMotorEnvironment(
                    m_IsPredictedGrounded,
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

            m_IsPredictedGrounded = moveResult.IsGrounded;

            m_LocomotionModel.ApplyMoveResult(moveResult);

            m_CharacterControllerMotor.ApplyCapsule(
                m_LocomotionModel.IsCrouching);
        }
    }
}