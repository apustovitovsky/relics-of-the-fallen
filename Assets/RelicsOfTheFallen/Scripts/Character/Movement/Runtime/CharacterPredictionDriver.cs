using RelicsOfTheFallen.Character;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace RelicsOfTheFallen.Character.Movement
{
    /// <summary>
    /// Predicts only the owning remote client's character.
    /// The server remains authoritative. On the predicted owner, the
    /// server-authoritative NetworkTransform is disabled locally so it
    /// cannot overwrite the same simulation root as prediction.
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

        [Header("History")]
        [SerializeField]
        [Min(1)]
        private int m_PredictedFrameHistoryCapacity = 128;

        [Header("Reconciliation")]
        [SerializeField]
        [Min(0f)]
        private float m_PositionCorrectionThreshold = 0.03f;

        [SerializeField]
        [Range(0f, 180f)]
        private float m_RotationCorrectionThreshold = 1f;

        [SerializeField]
        [Min(0f)]
        private float m_VelocityCorrectionThreshold = 0.01f;

        private ICharacterInputHistoryProvider
            m_TypedInputHistoryProvider;

        private NetworkTransform m_NetworkTransform;
        private ClientCharacter m_ClientCharacter;
        private CharacterLocomotionModel m_LocomotionModel;

        private CharacterPredictedFrameHistory
            m_PredictedFrameHistory;

        private bool m_IsPredictedGrounded;
        private bool m_IsPredictionActive;

        private void Awake()
        {
            m_TypedInputHistoryProvider =
                m_InputHistoryProvider as
                ICharacterInputHistoryProvider;

            m_NetworkTransform =
                GetComponent<NetworkTransform>();

            m_ClientCharacter =
                GetComponentInChildren<ClientCharacter>(true);
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

            if (m_NetworkTransform == null)
            {
                Debug.LogError(
                    $"{nameof(CharacterPredictionDriver)} " +
                    "requires a NetworkTransform on the " +
                    "same GameObject.",
                    this);

                enabled = false;
                return;
            }

            m_LocomotionModel =
                new CharacterLocomotionModel(
                    m_ServerCharacterMovement
                        .LocomotionConfiguration);

            m_PredictedFrameHistory =
                new CharacterPredictedFrameHistory(
                    m_PredictedFrameHistoryCapacity);

            m_NetworkTransform.enabled = false;

            m_CharacterControllerMotor
                .SetControllerEnabled(true);

            m_ServerCharacter.OwnerSnapshot.OnValueChanged +=
                OnOwnerSnapshotChanged;

            m_TypedInputHistoryProvider.CommandRecorded +=
                OnCommandRecorded;

            m_IsPredictionActive = true;

            RestoreAndReplay(
                m_ServerCharacter.OwnerSnapshot.Value,
                preserveGraphicsPose: false);
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
            m_PredictedFrameHistory.Clear();

            m_NetworkTransform.enabled = true;

            m_IsPredictionActive = false;
        }

        private void OnCommandRecorded(
            CharacterInputCommand command)
        {
            Simulate(
                command,
                shouldRecordPredictedFrame: true);
        }

        private void OnOwnerSnapshotChanged(
            CharacterOwnerSnapshot previousValue,
            CharacterOwnerSnapshot newValue)
        {
            Reconcile(newValue);
        }

        private void Reconcile(
            in CharacterOwnerSnapshot snapshot)
        {
            if (snapshot.ServerTick == 0 &&
                snapshot.LastProcessedInputSequence == 0)
            {
                return;
            }

            bool hasPredictedFrame =
                m_PredictedFrameHistory.TryGet(
                    snapshot.LastProcessedInputSequence,
                    out CharacterPredictedFrame
                        predictedFrame);

            bool requiresCorrection =
                !hasPredictedFrame ||
                RequiresCorrection(
                    predictedFrame,
                    snapshot);

            m_PredictedFrameHistory.DiscardThrough(
                snapshot.LastProcessedInputSequence);

            if (!requiresCorrection)
            {
                return;
            }

            m_PredictedFrameHistory.Clear();

            RestoreAndReplay(
                snapshot,
                preserveGraphicsPose: true);
        }

        private bool RequiresCorrection(
            in CharacterPredictedFrame predictedFrame,
            in CharacterOwnerSnapshot snapshot)
        {
            float positionThresholdSquared =
                m_PositionCorrectionThreshold *
                m_PositionCorrectionThreshold;

            if ((predictedFrame.Position -
                 snapshot.Position).sqrMagnitude >
                positionThresholdSquared)
            {
                return true;
            }

            if (Quaternion.Angle(
                    predictedFrame.Rotation,
                    snapshot.Rotation) >
                m_RotationCorrectionThreshold)
            {
                return true;
            }

            CharacterLocomotionSimulationState
                predictedState =
                    predictedFrame.LocomotionState;

            float velocityThresholdSquared =
                m_VelocityCorrectionThreshold *
                m_VelocityCorrectionThreshold;

            if ((predictedState.HorizontalVelocity -
                 snapshot.HorizontalVelocity).sqrMagnitude >
                velocityThresholdSquared)
            {
                return true;
            }

            if (Mathf.Abs(
                    predictedState.VerticalVelocity -
                    snapshot.VerticalVelocity) >
                m_VelocityCorrectionThreshold)
            {
                return true;
            }

            if (Mathf.Abs(
                    predictedState.CameraRotationOffset -
                    snapshot.CameraRotationOffset) >
                m_RotationCorrectionThreshold)
            {
                return true;
            }

            return predictedFrame.IsGrounded !=
                       snapshot.IsGrounded ||
                   predictedState.IsWalking !=
                       snapshot.IsWalking ||
                   predictedState.IsCrouchRequested !=
                       snapshot.IsCrouchRequested ||
                   predictedState.IsCrouching !=
                       snapshot.IsCrouching ||
                   predictedState.AirState !=
                       snapshot.AirState;
        }

        private void RestoreAndReplay(
            in CharacterOwnerSnapshot snapshot,
            bool preserveGraphicsPose)
        {
            if (snapshot.ServerTick == 0 &&
                snapshot.LastProcessedInputSequence == 0)
            {
                return;
            }

            Pose graphicsPoseBeforeCorrection =
                default;

            if (preserveGraphicsPose &&
                m_ClientCharacter != null)
            {
                graphicsPoseBeforeCorrection =
                    new Pose(
                        m_ClientCharacter.transform.position,
                        m_ClientCharacter.transform.rotation);
            }

            Vector3 probeDirection =
                snapshot.Rotation * Vector3.forward;

            m_CharacterControllerMotor.Teleport(
                snapshot.Position,
                snapshot.Rotation,
                snapshot.IsCrouching,
                probeDirection);

            m_IsPredictedGrounded = snapshot.IsGrounded;

            m_LocomotionModel.RestoreState(
                new CharacterLocomotionSimulationState(
                    snapshot.HorizontalVelocity,
                    snapshot.ActualVelocity,
                    snapshot.VerticalVelocity,
                    snapshot.CameraRotationOffset,
                    snapshot.IsWalking,
                    snapshot.IsCrouchRequested,
                    snapshot.IsCrouching,
                    snapshot.AirState));

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

                Simulate(
                    command,
                    shouldRecordPredictedFrame: true);
            }

            if (preserveGraphicsPose &&
                m_ClientCharacter != null)
            {
                m_ClientCharacter
                    .PreservePredictedGraphicsPose(
                        graphicsPoseBeforeCorrection);
            }
        }

        private void Simulate(
            in CharacterInputCommand inputCommand,
            bool shouldRecordPredictedFrame)
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

            if (!shouldRecordPredictedFrame)
            {
                return;
            }

            CharacterPredictedFrame predictedFrame =
                new CharacterPredictedFrame(
                    inputCommand.Sequence,
                    transform.position,
                    transform.rotation,
                    m_LocomotionModel.CaptureState(),
                    m_IsPredictedGrounded);

            if (!m_PredictedFrameHistory.TryRecord(
                    predictedFrame))
            {
                Debug.LogError(
                    "The predicted character frame history is full.",
                    this);
            }
        }
    }
}