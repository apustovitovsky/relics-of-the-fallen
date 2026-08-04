using UnityEngine;

namespace RelicsOfTheFallen.Targeting
{
    public sealed class AngleTargetScorer :
        ITargetScorer
    {
        private readonly float m_Weight;

        public AngleTargetScorer(float weight)
        {
            m_Weight = weight;
        }

        /// <summary>
        /// Scores targets nearer to the supplied forward direction more highly.
        /// </summary>
        public float Score(
            ITargetable target,
            Vector3 origin,
            Vector3 forward)
        {
            Vector3 direction =
                target.TargetAnchor.position - origin;

            if (direction.sqrMagnitude <= 0.0001f ||
                forward.sqrMagnitude <= 0.0001f)
            {
                return 0f;
            }

            return Vector3.Dot(
                direction.normalized,
                forward.normalized) * m_Weight;
        }
    }
}