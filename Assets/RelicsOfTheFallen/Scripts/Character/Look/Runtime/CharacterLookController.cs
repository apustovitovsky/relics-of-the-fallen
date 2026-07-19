using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RelicsOfTheFallen.Character.Look
{
    public sealed class CharacterLookController : NetworkBehaviour
    {
        const float k_LagDeltaTimeAdjustment = 20f;

        [SerializeField] Transform m_Pivot;
        [SerializeField] Transform m_Origin;
        [SerializeField] Transform m_TargetProxy;
        [SerializeField] InputActionReference m_LookAction;

        [SerializeField] bool m_InvertVerticalLook;
        [SerializeField] float m_Sensitivity = 2f;
        [SerializeField] float m_MouseSensitivity = 0.1f;
        [SerializeField] float m_GamepadLookSpeed = 120f;
        [SerializeField] Vector2 m_PitchBounds = new(-70f, 70f);
        [SerializeField] float m_PositionLag = 0.2f;
        [SerializeField] float m_RotationLag = 0.2f;

        Transform m_Target;
        Vector3 m_LastPosition;
        float m_LastYaw;
        float m_Pitch;
        float m_Yaw;

        public Vector3 ForwardFlatNormalized
        {
            get
            {
                Vector3 forward = m_Pivot.forward;
                forward.y = 0f;
                return forward.normalized;
            }
        }

        public Vector3 RightFlatNormalized
        {
            get
            {
                Vector3 right = m_Pivot.right;
                right.y = 0f;
                return right.normalized;
            }
        }

        public float Pitch => m_Pivot.eulerAngles.x;

        public override void OnNetworkSpawn()
        {
            if (!IsClient || !IsOwner)
            {
                enabled = false;
                return;
            }

            if (m_Pivot == null ||
                m_Origin == null ||
                m_LookAction == null ||
                m_LookAction.action == null)
            {
                Debug.LogError(
                    $"{nameof(CharacterLookController)} is not configured.",
                    this);

                enabled = false;
                return;
            }

            m_LookAction.action.actionMap.Enable();

            m_Pivot.SetPositionAndRotation(
                m_Origin.position,
                m_Origin.rotation);

            m_LastPosition = m_Pivot.position;

            Vector3 pivotAngles = m_Pivot.eulerAngles;
            m_Pitch = NormalizeAngle(pivotAngles.x);
            m_Yaw = pivotAngles.y;
            m_LastYaw = m_Yaw;
        }

        public override void OnNetworkDespawn()
        {
            enabled = false;
        }

        void Update()
        {
            float positionalFollowSpeed =
                k_LagDeltaTimeAdjustment /
                Mathf.Max(m_PositionLag, 0.001f);

            float rotationalFollowSpeed =
                k_LagDeltaTimeAdjustment /
                Mathf.Max(m_RotationLag, 0.001f);

            Vector2 lookInput =
                m_LookAction.action.ReadValue<Vector2>();

            float inputScale =
                m_LookAction.action.activeControl?.device is Pointer
                    ? m_MouseSensitivity
                    : m_GamepadLookSpeed * Time.deltaTime;

            float verticalDirection =
                m_InvertVerticalLook ? 1f : -1f;

            m_Pitch +=
                lookInput.y *
                verticalDirection *
                inputScale *
                m_Sensitivity;

            m_Pitch = Mathf.Clamp(
                m_Pitch,
                m_PitchBounds.x,
                m_PitchBounds.y);

            if (m_Target != null)
            {
                UpdateTargetRotation(rotationalFollowSpeed);
            }
            else
            {
                m_Yaw +=
                    lookInput.x *
                    inputScale *
                    m_Sensitivity;

                m_Yaw = Mathf.Lerp(
                    m_LastYaw,
                    m_Yaw,
                    rotationalFollowSpeed * Time.deltaTime);
            }

            Vector3 position = Vector3.Lerp(
                m_LastPosition,
                m_Origin.position,
                positionalFollowSpeed * Time.deltaTime);

            m_Pivot.SetPositionAndRotation(
                position,
                Quaternion.Euler(m_Pitch, m_Yaw, 0f));

            m_LastPosition = position;
            m_LastYaw = m_Yaw;
        }

        public void SetTarget(Transform target)
        {
            m_Target = target;
        }

        public void ClearTarget()
        {
            m_Target = null;
        }

        public void SetWorldDirection(Vector3 direction)
        {
            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            ClearTarget();

            m_Yaw = Quaternion
                .LookRotation(direction.normalized)
                .eulerAngles
                .y;
        }

        void UpdateTargetRotation(float rotationalFollowSpeed)
        {
            Vector3 targetPosition = m_Target.position;

            if (m_TargetProxy != null)
            {
                m_TargetProxy.position = targetPosition;
                targetPosition = m_TargetProxy.position;
            }

            Vector3 aimVector =
                targetPosition - m_Origin.position;

            if (aimVector.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Quaternion targetRotation =
                Quaternion.LookRotation(aimVector);

            targetRotation = Quaternion.Lerp(
                m_Pivot.rotation,
                targetRotation,
                rotationalFollowSpeed * Time.deltaTime);

            Vector3 euler = targetRotation.eulerAngles;
            m_Pitch = NormalizeAngle(euler.x);
            m_Yaw = euler.y;
        }

        static float NormalizeAngle(float angle)
        {
            return angle > 180f ? angle - 360f : angle;
        }
    }
}