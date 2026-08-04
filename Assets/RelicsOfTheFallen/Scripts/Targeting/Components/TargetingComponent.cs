using UnityEngine;

namespace RelicsOfTheFallen.Targeting
{
    [DisallowMultipleComponent]
    public sealed class TargetingComponent :
        MonoBehaviour,
        ITargetable
    {
        [field: SerializeField]
        public GameObject TargetActor
        {
            get; private set;
        }

        [field: SerializeField]
        public string DisplayName { get; private set; } = "Player";

        [field: SerializeField]
        public Transform TargetAnchor
        {
            get; private set;
        }

        [field: SerializeField]
        public Transform UiAnchor
        {
            get; private set;
        }

        [field: SerializeField]
        public bool IsTargetable { get; private set; } = true;

        private void Awake()
        {
            if (TargetActor == null)
            {
                TargetActor = gameObject;
            }

            if (TargetAnchor == null)
            {
                TargetAnchor = transform;
            }

            if (UiAnchor == null)
            {
                UiAnchor = TargetAnchor;
            }
        }

        /// <summary>
        /// Changes the name presented for this target.
        /// </summary>
        public void SetDisplayName(string displayName)
        {
            DisplayName = displayName;
        }

        /// <summary>
        /// Changes whether this component may currently be selected as a target.
        /// </summary>
        public void SetTargetable(bool isTargetable)
        {
            IsTargetable = isTargetable;
        }
    }
}