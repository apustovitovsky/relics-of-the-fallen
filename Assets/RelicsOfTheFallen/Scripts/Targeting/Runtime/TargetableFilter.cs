using UnityEngine;

namespace RelicsOfTheFallen.Targeting
{
    public sealed class TargetableFilter :
        ITargetFilter
    {
        /// <summary>
        /// Accepts targets that are alive as Unity objects and currently targetable.
        /// </summary>
        public bool IsMatch(
            ITargetable target,
            Vector3 origin,
            Vector3 forward)
        {
            if (target == null)
            {
                return false;
            }

            if (target is Object unityObject &&
                unityObject == null)
            {
                return false;
            }

            return target.IsTargetable &&
                target.TargetActor != null &&
                target.TargetAnchor != null;
        }
    }
}