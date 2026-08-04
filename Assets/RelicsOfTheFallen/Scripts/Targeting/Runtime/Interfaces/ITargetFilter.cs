using UnityEngine;

namespace RelicsOfTheFallen.Targeting
{
    public interface ITargetFilter
    {
        bool IsMatch(
            ITargetable target,
            Vector3 origin,
            Vector3 forward);
    }
}