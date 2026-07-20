using Unity.Netcode;
using UnityEngine;

namespace RelicsOfTheFallen.Character
{
    public class ClientCharacter :
        NetworkBehaviour,
        INetworkUpdateSystem
    {
        const int k_ObserverSnapshotBufferCapacity = 32;

        [SerializeField] Animator m_ClientVisualsAnimator;

        [SerializeField, Min(0.01f)]
        float m_HostGraphicsSmoothingTime = 0.1f;

        [SerializeField, Min(0.01f)]
        float m_PredictionCorrectionSmoothingTime = 0.1f;

        [SerializeField, Min(0)]
        int m_ObserverInterpolationDelayTicks = 2;

        ServerCharacter m_ServerCharacter;

        CharacterLocomotionState m_LatestLocomotionState;
        CharacterLocomotionState m_ObserverLocomotionState;

        Vector3 m_BaseLocalPosition;
        Quaternion m_BaseLocalRotation;

        Vector3 m_HostGraphicsPosition;
        Quaternion m_HostGraphicsRotation;
        Vector3 m_HostGraphicsVelocity;
        bool m_HasHostGraphicsPose;

        Vector3 m_PredictedGraphicsPosition;
        Quaternion m_PredictedGraphicsRotation;
        Vector3 m_PredictedGraphicsVelocity;
        bool m_HasPredictedGraphicsPose;

        CharacterRenderSnapshotBuffer m_ObserverSnapshotBuffer;

        public Animator OurAnimator => m_ClientVisualsAnimator;
        public ServerCharacter ServerCharacter => m_ServerCharacter;

        public CharacterLocomotionState LocomotionState =>
            ShouldUseObserverRenderBuffer()
                ? m_ObserverLocomotionState
                : m_LatestLocomotionState;

        void Awake()
        {
            m_BaseLocalPosition = transform.localPosition;
            m_BaseLocalRotation = transform.localRotation;

            m_ObserverSnapshotBuffer =
                new CharacterRenderSnapshotBuffer(
                    k_ObserverSnapshotBufferCapacity);

            enabled = false;
        }

        public override void OnNetworkSpawn()
        {
            if (!IsClient || transform.parent == null)
            {
                return;
            }

            enabled = true;

            m_ServerCharacter =
                GetComponentInParent<ServerCharacter>();

            m_ServerCharacter.LocomotionState.OnValueChanged +=
                OnLocomotionStateChanged;

            m_ServerCharacter.RenderSnapshotReceived +=
                OnRenderSnapshotReceived;

            m_LatestLocomotionState =
                m_ServerCharacter.LocomotionState.Value;

            m_ObserverSnapshotBuffer.Clear();

            ResetHostGraphicsPose();
            ResetPredictedGraphicsPose();

            this.RegisterNetworkUpdate(
                NetworkUpdateStage.PostLateUpdate);

            name = "AvatarGraphics" +
                m_ServerCharacter.OwnerClientId;
        }

        public override void OnNetworkDespawn()
        {
            this.UnregisterNetworkUpdate(
                NetworkUpdateStage.PostLateUpdate);

            if (m_ServerCharacter != null)
            {
                m_ServerCharacter.LocomotionState.OnValueChanged -=
                    OnLocomotionStateChanged;

                m_ServerCharacter.RenderSnapshotReceived -=
                    OnRenderSnapshotReceived;
            }

            m_HasHostGraphicsPose = false;
            m_HasPredictedGraphicsPose = false;

            m_ObserverSnapshotBuffer.Clear();

            enabled = false;
        }

        public void NetworkUpdate(
            NetworkUpdateStage updateStage)
        {
            if (updateStage !=
                    NetworkUpdateStage.PostLateUpdate ||
                m_ServerCharacter == null)
            {
                return;
            }

            if (IsHost)
            {
                SmoothHostGraphics();
                return;
            }

            if (IsOwner)
            {
                SmoothPredictedGraphics();
                return;
            }

            UpdateObserverGraphics();
        }

        public void PreservePredictedGraphicsPose(
            Pose poseBeforeCorrection)
        {
            if (!IsClient || !IsOwner || IsHost ||
                m_ServerCharacter == null)
            {
                return;
            }

            m_PredictedGraphicsPosition =
                poseBeforeCorrection.position;

            m_PredictedGraphicsRotation =
                poseBeforeCorrection.rotation;

            m_PredictedGraphicsVelocity = Vector3.zero;
            m_HasPredictedGraphicsPose = true;

            transform.SetPositionAndRotation(
                m_PredictedGraphicsPosition,
                m_PredictedGraphicsRotation);
        }

        void OnLocomotionStateChanged(
            CharacterLocomotionState previousValue,
            CharacterLocomotionState newValue)
        {
            m_LatestLocomotionState = newValue;
        }

        void OnRenderSnapshotReceived(
            CharacterRenderSnapshot snapshot)
        {
            m_ObserverSnapshotBuffer.Add(snapshot);
        }

        void ResetHostGraphicsPose()
        {
            GetTargetGraphicsPose(
                out m_HostGraphicsPosition,
                out m_HostGraphicsRotation);

            m_HostGraphicsVelocity = Vector3.zero;
            m_HasHostGraphicsPose = true;

            transform.SetPositionAndRotation(
                m_HostGraphicsPosition,
                m_HostGraphicsRotation);
        }

        void ResetPredictedGraphicsPose()
        {
            GetTargetGraphicsPose(
                out m_PredictedGraphicsPosition,
                out m_PredictedGraphicsRotation);

            m_PredictedGraphicsVelocity = Vector3.zero;
            m_HasPredictedGraphicsPose = true;

            transform.SetPositionAndRotation(
                m_PredictedGraphicsPosition,
                m_PredictedGraphicsRotation);
        }

        void SmoothHostGraphics()
        {
            if (!m_HasHostGraphicsPose)
            {
                ResetHostGraphicsPose();
                return;
            }

            GetTargetGraphicsPose(
                out Vector3 targetPosition,
                out Quaternion targetRotation);

            m_HostGraphicsPosition =
                Vector3.SmoothDamp(
                    m_HostGraphicsPosition,
                    targetPosition,
                    ref m_HostGraphicsVelocity,
                    m_HostGraphicsSmoothingTime,
                    Mathf.Infinity,
                    Time.deltaTime);

            m_HostGraphicsRotation =
                Quaternion.Slerp(
                    m_HostGraphicsRotation,
                    targetRotation,
                    GetSmoothingInterpolation(
                        m_HostGraphicsSmoothingTime));

            transform.SetPositionAndRotation(
                m_HostGraphicsPosition,
                m_HostGraphicsRotation);
        }

        void SmoothPredictedGraphics()
        {
            if (!m_HasPredictedGraphicsPose)
            {
                ResetPredictedGraphicsPose();
                return;
            }

            GetTargetGraphicsPose(
                out Vector3 targetPosition,
                out Quaternion targetRotation);

            m_PredictedGraphicsPosition =
                Vector3.SmoothDamp(
                    m_PredictedGraphicsPosition,
                    targetPosition,
                    ref m_PredictedGraphicsVelocity,
                    m_PredictionCorrectionSmoothingTime,
                    Mathf.Infinity,
                    Time.deltaTime);

            m_PredictedGraphicsRotation =
                Quaternion.Slerp(
                    m_PredictedGraphicsRotation,
                    targetRotation,
                    GetSmoothingInterpolation(
                        m_PredictionCorrectionSmoothingTime));

            transform.SetPositionAndRotation(
                m_PredictedGraphicsPosition,
                m_PredictedGraphicsRotation);
        }

        void UpdateObserverGraphics()
        {
            if (NetworkManager == null)
            {
                return;
            }

            uint serverTick = unchecked(
                (uint)NetworkManager
                    .NetworkTickSystem
                    .ServerTime
                    .Tick);

            uint delayTicks = unchecked(
                (uint)m_ObserverInterpolationDelayTicks);

            uint renderTick =
                serverTick > delayTicks
                    ? serverTick - delayTicks
                    : 0;

            if (!m_ObserverSnapshotBuffer.TrySample(
                    renderTick,
                    out CharacterRenderSnapshot previous,
                    out CharacterRenderSnapshot next,
                    out float interpolation))
            {
                return;
            }

            GetSnapshotGraphicsPose(
                previous,
                out Vector3 previousPosition,
                out Quaternion previousRotation);

            GetSnapshotGraphicsPose(
                next,
                out Vector3 nextPosition,
                out Quaternion nextRotation);

            transform.SetPositionAndRotation(
                Vector3.Lerp(
                    previousPosition,
                    nextPosition,
                    interpolation),
                Quaternion.Slerp(
                    previousRotation,
                    nextRotation,
                    interpolation));

            m_ObserverLocomotionState =
                interpolation < 0.5f
                    ? previous.LocomotionState
                    : next.LocomotionState;
        }

        float GetSmoothingInterpolation(
            float smoothingTime)
        {
            return 1f - Mathf.Exp(
                -Time.deltaTime / smoothingTime);
        }

        bool ShouldUseObserverRenderBuffer()
        {
            return !IsHost && !IsOwner;
        }

        void GetTargetGraphicsPose(
            out Vector3 position,
            out Quaternion rotation)
        {
            Transform simulationRoot =
                m_ServerCharacter.transform;

            position = simulationRoot.TransformPoint(
                m_BaseLocalPosition);

            rotation = simulationRoot.rotation *
                       m_BaseLocalRotation;
        }

        void GetSnapshotGraphicsPose(
            CharacterRenderSnapshot snapshot,
            out Vector3 position,
            out Quaternion rotation)
        {
            position = snapshot.Position +
                       snapshot.Rotation *
                       m_BaseLocalPosition;

            rotation = snapshot.Rotation *
                       m_BaseLocalRotation;
        }
    }
}