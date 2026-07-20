using UnityEngine;

namespace RelicsOfTheFallen.Character.Movement
{
    /// <summary>
    /// Unity adapter for the physical character capsule.
    /// It is shared by server authority and future owner prediction,
    /// but only one of them may call Move during a given session.
    /// </summary>
    public sealed class CharacterControllerMotor : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private CharacterController m_CharacterController;

        [Header("Crouch")]
        [SerializeField]
        private float m_CrouchingHeight = 1.2f;

        [SerializeField]
        private float m_CrouchingCenter = 0.6f;

        [SerializeField]
        [Range(0f, 0.1f)]
        private float m_CeilingCheckRadiusPadding = 0.02f;

        [Header("Ground")]
        [SerializeField]
        private LayerMask m_GroundLayerMask = ~0;

        [SerializeField]
        private float m_GroundProbeDistance = 0.2f;

        [SerializeField]
        private float m_GroundProbeStartOffset = 0.02f;

        [SerializeField]
        private float m_GroundProbeRadiusPadding = 0.05f;

        [SerializeField]
        private float m_MaxGroundAngle = 60f;

        private CharacterGroundProbe m_GroundProbe;

        private Vector3 m_StandingCenter;
        private float m_StandingHeight;

        public GroundInfo GroundInfo { get; private set; }

        private void Awake()
        {
            m_GroundProbe = new CharacterGroundProbe(
                m_GroundLayerMask,
                m_GroundProbeDistance,
                m_GroundProbeStartOffset,
                m_GroundProbeRadiusPadding,
                m_MaxGroundAngle);

            m_StandingHeight = m_CharacterController.height;
            m_StandingCenter = m_CharacterController.center;
        }

        public void SetControllerEnabled(bool enabled)
        {
            m_CharacterController.enabled = enabled;
        }

        public void ApplyCapsule(bool isCrouching)
        {
            if (isCrouching)
            {
                ApplyCrouchingCapsule();
                return;
            }

            ApplyStandingCapsule();
        }

        public void ApplyStandingCapsule()
        {
            m_CharacterController.height = m_StandingHeight;
            m_CharacterController.center = m_StandingCenter;
        }

        /// <summary>
        /// Repositions the physical capsule without leaving the
        /// CharacterController enabled while its Transform is changed.
        /// </summary>
        public void Teleport(
            Vector3 position,
            Quaternion rotation,
            bool isCrouching,
            Vector3 groundProbeDirection)
        {
            bool wasControllerEnabled =
                m_CharacterController.enabled;

            m_CharacterController.enabled = false;

            transform.SetPositionAndRotation(
                position,
                rotation);

            ApplyCapsule(isCrouching);

            m_CharacterController.enabled =
                wasControllerEnabled;

            GroundInfo = m_GroundProbe.Probe(
                m_CharacterController,
                groundProbeDirection);
        }

        public bool CanStandUp()
        {
            if (Mathf.Approximately(
                    m_CharacterController.height,
                    m_StandingHeight))
            {
                return true;
            }

            float currentHalfSegment = Mathf.Max(
                0f,
                m_CharacterController.height * 0.5f -
                m_CharacterController.radius);

            float standingHalfSegment = Mathf.Max(
                0f,
                m_StandingHeight * 0.5f -
                m_CharacterController.radius);

            Vector3 currentCenter = transform.TransformPoint(
                m_CharacterController.center);

            Vector3 standingCenter = transform.TransformPoint(
                m_StandingCenter);

            Vector3 currentTopSphereCenter = currentCenter +
                                             transform.up *
                                             currentHalfSegment;

            Vector3 standingTopSphereCenter = standingCenter +
                                              transform.up *
                                              standingHalfSegment;

            float requiredHeight = Vector3.Dot(
                standingTopSphereCenter -
                currentTopSphereCenter,
                transform.up);

            if (requiredHeight <= 0f)
            {
                return true;
            }

            float radius = Mathf.Max(
                0.01f,
                m_CharacterController.radius -
                m_CeilingCheckRadiusPadding);

            return !Physics.SphereCast(
                currentTopSphereCenter,
                radius,
                transform.up,
                out _,
                requiredHeight,
                m_GroundLayerMask,
                QueryTriggerInteraction.Ignore);
        }

        public CharacterMotorMoveResult Move(
            Vector3 velocity,
            float deltaTime,
            Vector3 movementDirection)
        {
            Vector3 positionBeforeMove = transform.position;

            CollisionFlags collisionFlags =
                m_CharacterController.Move(
                    velocity * deltaTime);

            Vector3 actualVelocity =
                (transform.position - positionBeforeMove) /
                deltaTime;

            GroundInfo = m_GroundProbe.Probe(
                m_CharacterController,
                movementDirection);

            bool isGrounded = GroundInfo.IsGrounded ||
                              (collisionFlags &
                               CollisionFlags.Below) != 0;

            return new CharacterMotorMoveResult(
                actualVelocity,
                isGrounded);
        }

        private void ApplyCrouchingCapsule()
        {
            Vector3 crouchingCenter = m_StandingCenter;
            crouchingCenter.y = m_CrouchingCenter;

            m_CharacterController.height =
                m_CrouchingHeight;

            m_CharacterController.center =
                crouchingCenter;
        }
    }
}