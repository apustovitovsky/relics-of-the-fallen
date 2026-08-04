using System.Collections.Generic;
using UnityEngine;

namespace RelicsOfTheFallen.Targeting
{
    public sealed class TargetSelector
    {
        private readonly IReadOnlyList<ITargetFilter> m_Filters;
        private readonly IReadOnlyList<ITargetScorer> m_Scorers;

        public TargetSelector(
            IReadOnlyList<ITargetFilter> filters,
            IReadOnlyList<ITargetScorer> scorers)
        {
            m_Filters = filters;
            m_Scorers = scorers;
        }

        /// <summary>
        /// Selects the highest-scoring target that satisfies every configured filter.
        /// </summary>
        public ITargetable SelectBest(
            IReadOnlyCollection<ITargetable> candidates,
            Vector3 origin,
            Vector3 forward)
        {
            ITargetable bestTarget = null;
            float bestScore = float.NegativeInfinity;

            foreach (ITargetable candidate in candidates)
            {
                if (!IsSelectable(
                        candidate,
                        origin,
                        forward))
                {
                    continue;
                }

                float score = Score(
                    candidate,
                    origin,
                    forward);

                if (score <= bestScore)
                {
                    continue;
                }

                bestScore = score;
                bestTarget = candidate;
            }

            return bestTarget;
        }

        /// <summary>
        /// Determines whether a target satisfies every configured selection filter.
        /// </summary>
        public bool IsSelectable(
            ITargetable target,
            Vector3 origin,
            Vector3 forward)
        {
            foreach (ITargetFilter filter in m_Filters)
            {
                if (!filter.IsMatch(
                        target,
                        origin,
                        forward))
                {
                    return false;
                }
            }

            return true;
        }

        private float Score(
            ITargetable target,
            Vector3 origin,
            Vector3 forward)
        {
            float score = 0f;

            foreach (ITargetScorer scorer in m_Scorers)
            {
                score += scorer.Score(
                    target,
                    origin,
                    forward);
            }

            return score;
        }
    }
}