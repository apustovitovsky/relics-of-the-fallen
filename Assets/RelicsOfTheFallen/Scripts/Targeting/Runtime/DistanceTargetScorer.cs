using UnityEngine;

namespace RelicsOfTheFallen.Targeting
{
    public sealed class DistanceTargetScorer :
        ITargetScorer
    {
        private readonly float m_Weight;

        public DistanceTargetScorer(float weight)
        {
            m_Weight = weight;
        }

        /// <summary>
        /// Scores nearer targets higher according to the configured distance weight.
        /// </summary>
        public float Score(
            ITargetable target,
            Vector3 origin,
            Vector3 forward)
        {
            float distance = Vector3.Distance(
                origin,
                target.TargetAnchor.position);

            return m_Weight /
                Mathf.Max(
                    distance,
                    0.01f);
        }
    }
}