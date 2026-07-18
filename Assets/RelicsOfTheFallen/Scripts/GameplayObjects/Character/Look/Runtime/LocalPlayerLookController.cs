using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RelicsOfTheFallen.GameplayObjects.Character.Look
{
    public sealed class LocalPlayerLookController : NetworkBehaviour
    {
        [SerializeField]
        Transform m_CameraPivot;

        [SerializeField]
        InputActionReference m_LookAction;

        [SerializeField]
        float m_MouseSensitivity = 0.1f;

        [SerializeField]
        float m_GamepadLookSpeed = 120f;

        [SerializeField]
        Vector2 m_PitchBounds = new(-70f, 70f);

        [SerializeField]
        bool m_InvertVerticalLook;

        float m_Pitch;
        float m_Yaw;

        public override void OnNetworkSpawn()
        {
            if (!IsClient || !IsOwner)
            {
                enabled = false;
                return;
            }

            if (m_CameraPivot == null)
            {
                Debug.LogError(
                    $"{nameof(LocalPlayerLookController)} requires a camera pivot.",
                    this);

                enabled = false;
                return;
            }

            if (m_LookAction == null || m_LookAction.action == null)
            {
                Debug.LogError(
                    $"{nameof(LocalPlayerLookController)} requires a look action.",
                    this);

                enabled = false;
                return;
            }

            var pivotAngles = m_CameraPivot.eulerAngles;

            m_Pitch = NormalizeAngle(pivotAngles.x);
            m_Yaw = pivotAngles.y;
        }

        void Update()
        {
            var lookInput = m_LookAction.action.ReadValue<Vector2>();

            var inputScale =
                m_LookAction.action.activeControl?.device is Pointer
                    ? m_MouseSensitivity
                    : m_GamepadLookSpeed * Time.deltaTime;

            var verticalDirection = m_InvertVerticalLook ? 1f : -1f;

            m_Yaw += lookInput.x * inputScale;
            m_Pitch += lookInput.y * verticalDirection * inputScale;
            m_Pitch = Mathf.Clamp(
                m_Pitch,
                m_PitchBounds.x,
                m_PitchBounds.y);

            m_CameraPivot.rotation =
                Quaternion.Euler(m_Pitch, m_Yaw, 0f);
        }

        static float NormalizeAngle(float angle)
        {
            return angle > 180f ? angle - 360f : angle;
        }
    }
}