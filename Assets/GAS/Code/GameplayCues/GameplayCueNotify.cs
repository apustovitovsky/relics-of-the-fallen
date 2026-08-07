using UnityEngine;

namespace GAS
{
    /// <summary>
    /// Defines immutable authoring data for one routed gameplay cue.
    /// </summary>
    [CreateAssetMenu(
        menuName = "GAS/Gameplay Cue Notify",
        fileName = "GCN_")]
    public sealed class GameplayCueNotify :
        ScriptableObject
    {
        [field: SerializeField]
        public GameplayTag GameplayCueTag
        {
            get;
            private set;
        }

        [field: SerializeField]
        public bool IsOverride
        {
            get;
            private set;
        } = true;

        [field: SerializeField]
        public GameObject OnActivePrefab
        {
            get;
            private set;
        }

        [field: SerializeField]
        public GameObject WhileActivePrefab
        {
            get;
            private set;
        }

        [field: SerializeField]
        public GameObject ExecutedPrefab
        {
            get;
            private set;
        }

        [field: SerializeField]
        public GameObject RemovedPrefab
        {
            get;
            private set;
        }

        [field: SerializeField]
        public bool AttachToTarget
        {
            get;
            private set;
        }

        [field: SerializeField]
        public bool AllowMultipleOnActiveEvents
        {
            get;
            private set;
        }

        [field: SerializeField]
        public bool UniqueInstancePerInstigator
        {
            get;
            private set;
        }

        [field: SerializeField]
        public bool UniqueInstancePerSourceObject
        {
            get;
            private set;
        }
    }
}