using UnityEngine;

namespace GAS
{
    public readonly struct GameplayAbilityTargetingLocationInfo
    {
        private readonly bool m_HasLiteralTransform;
        private readonly Pose m_LiteralTransform;

        public Pose LiteralTransform =>
            m_HasLiteralTransform
                ? m_LiteralTransform
                : Pose.identity;

        /// <summary>
        /// Creates targeting location information from a literal world-space transform.
        /// </summary>
        public GameplayAbilityTargetingLocationInfo(
            Pose literalTransform)
        {
            m_HasLiteralTransform =
                true;

            m_LiteralTransform =
                literalTransform;
        }

        /// <summary>
        /// Resolves this location information into a world-space transform.
        /// </summary>
        public Pose GetTargetingTransform()
        {
            return LiteralTransform;
        }
    }
}