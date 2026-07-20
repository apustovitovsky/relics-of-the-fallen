using UnityEngine;

namespace RelicsOfTheFallen.Character.Movement
{
    public sealed class CharacterGroundProbe
    {
        readonly LayerMask m_GroundLayerMask;
        readonly float m_ProbeDistance;
        readonly float m_ProbeStartOffset;
        readonly float m_ProbeRadiusPadding;
        readonly float m_MaxGroundAngle;

        public CharacterGroundProbe(
            LayerMask groundLayerMask,
            float probeDistance,
            float probeStartOffset,
            float probeRadiusPadding,
            float maxGroundAngle)
        {
            m_GroundLayerMask = groundLayerMask;
            m_ProbeDistance = probeDistance;
            m_ProbeStartOffset = probeStartOffset;
            m_ProbeRadiusPadding = probeRadiusPadding;
            m_MaxGroundAngle = maxGroundAngle;
        }

        public GroundInfo Probe(
            CharacterController characterController,
            Vector3 referenceDirection)
        {
            Transform characterTransform =
                characterController.transform;

            Vector3 up = characterTransform.up;

            Vector3 worldCenter =
                characterTransform.TransformPoint(
                    characterController.center);

            float halfSegment =
                Mathf.Max(
                    0f,
                    characterController.height * 0.5f -
                    characterController.radius);

            Vector3 bottomSphereCenter =
                worldCenter - up * halfSegment;

            float probeRadius =
                Mathf.Max(
                    0.01f,
                    characterController.radius -
                    m_ProbeRadiusPadding);

            Vector3 castOrigin =
                bottomSphereCenter +
                up * m_ProbeStartOffset;

            float castDistance =
                m_ProbeStartOffset +
                m_ProbeDistance;

            if (!Physics.SphereCast(
                    castOrigin,
                    probeRadius,
                    -up,
                    out RaycastHit sphereHit,
                    castDistance,
                    m_GroundLayerMask,
                    QueryTriggerInteraction.Ignore))
            {
                return default;
            }

            Vector3 normal = sphereHit.normal;

            Vector3 normalRayOrigin =
                sphereHit.point + up * 0.05f;

            if (Physics.Raycast(
                    normalRayOrigin,
                    -up,
                    out RaycastHit normalHit,
                    0.1f,
                    m_GroundLayerMask,
                    QueryTriggerInteraction.Ignore))
            {
                normal = normalHit.normal;
            }

            float slopeAngle =
                Vector3.Angle(normal, up);

            bool isGrounded =
                slopeAngle <= m_MaxGroundAngle;

            float inclineAngle =
                CalculateInclineAngle(
                    normal,
                    referenceDirection,
                    characterTransform.forward,
                    up);

            return new GroundInfo(
                true,
                isGrounded,
                sphereHit.point,
                normal,
                slopeAngle,
                inclineAngle);
        }

        static float CalculateInclineAngle(
            Vector3 groundNormal,
            Vector3 referenceDirection,
            Vector3 fallbackForward,
            Vector3 up)
        {
            Vector3 direction =
                Vector3.ProjectOnPlane(
                    referenceDirection,
                    up);

            if (direction.sqrMagnitude < 0.0001f)
            {
                direction =
                    Vector3.ProjectOnPlane(
                        fallbackForward,
                        up);
            }

            direction.Normalize();

            float rise =
                Vector3.Dot(
                    groundNormal,
                    -direction);

            float vertical =
                Vector3.Dot(
                    groundNormal,
                    up);

            return Mathf.Atan2(rise, vertical) *
                Mathf.Rad2Deg;
        }
    }
}