using UnityEngine;

namespace RelicsOfTheFallen.Character.Look
{
    [DisallowMultipleComponent]
    public sealed class CharacterLookController :
        MonoBehaviour,
        ICharacterLookSource
    {
        [Header("References")]
        [SerializeField]
        MonoBehaviour m_InputSourceComponent;

        [SerializeField]
        Transform m_CameraPivot;

        [Header("Pointer Look")]
        [SerializeField, Min(0f)]
        float m_Sensitivity = 0.1f;

        [SerializeField]
        float m_MinPitch = -70f;

        [SerializeField]
        float m_MaxPitch = 70f;

        ICharacterInputSource m_InputSource;
        float m_Yaw;
        float m_Pitch;

        public float Yaw => m_Yaw;

        public float Pitch => m_Pitch;

        public Vector3 ForwardOnGround =>
            ProjectOnGround(
                m_CameraPivot.forward,
                transform.forward);

        public Vector3 RightOnGround =>
            ProjectOnGround(
                m_CameraPivot.right,
                transform.right);

        void Awake()
        {
            m_InputSource =
                m_InputSourceComponent as ICharacterInputSource;

            if (m_InputSource == null ||
                m_CameraPivot == null)
            {
                Debug.LogError(
                    $"{nameof(CharacterLookController)} on '{name}' " +
                    "requires an input source and a camera pivot.",
                    this);

                enabled = false;
                return;
            }

            var worldEulerAngles =
                m_CameraPivot.eulerAngles;

            m_Yaw = worldEulerAngles.y;
            m_Pitch = ToSignedAngle(worldEulerAngles.x);
        }

        void LateUpdate()
        {
            var look = m_InputSource.Current.Look;

            m_Yaw = Mathf.Repeat(
                m_Yaw + look.x * m_Sensitivity,
                360f);

            m_Pitch = Mathf.Clamp(
                m_Pitch - look.y * m_Sensitivity,
                m_MinPitch,
                m_MaxPitch);

            m_CameraPivot.rotation =
                Quaternion.Euler(m_Pitch, m_Yaw, 0f);
        }

        static Vector3 ProjectOnGround(
            Vector3 direction,
            Vector3 fallback)
        {
            var projected =
                Vector3.ProjectOnPlane(
                    direction,
                    Vector3.up);

            return projected.sqrMagnitude > 0.0001f
                ? projected.normalized
                : fallback.normalized;
        }

        static float ToSignedAngle(float angle)
        {
            return angle > 180f
                ? angle - 360f
                : angle;
        }
    }
}