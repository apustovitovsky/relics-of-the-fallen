using UnityEngine;

namespace RelicsOfTheFallen.Targeting
{
    public sealed class ViewAngleTargetFilter :
        ITargetFilter
    {
        private readonly float m_MinimumDot;

        public ViewAngleTargetFilter(float maximumAngle)
        {
            float clampedAngle = Mathf.Clamp(
                maximumAngle,
                0f,
                180f);

            m_MinimumDot = Mathf.Cos(
                clampedAngle * Mathf.Deg2Rad);
        }

        /// <summary>
        /// Accepts targets located within the configured horizontal view angle.
        /// </summary>
        public bool IsMatch(
            ITargetable target,
            Vector3 origin,
            Vector3 forward)
        {
            Vector3 direction =
                target.TargetAnchor.position - origin;

            direction.y = 0f;
            forward.y = 0f;

            if (direction.sqrMagnitude <= 0.0001f ||
                forward.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            return Vector3.Dot(
                forward.normalized,
                direction.normalized) >= m_MinimumDot;
        }
    }
}