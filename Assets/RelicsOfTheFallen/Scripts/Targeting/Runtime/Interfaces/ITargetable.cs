using UnityEngine;

namespace RelicsOfTheFallen.Targeting
{
    public interface ITargetable
    {
        GameObject TargetActor
        {
            get;
        }

        string DisplayName
        {
            get;
        }

        Transform TargetAnchor
        {
            get;
        }

        Transform UiAnchor
        {
            get;
        }

        bool IsTargetable
        {
            get;
        }
    }
}