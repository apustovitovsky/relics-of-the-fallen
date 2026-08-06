using System;
using UnityEngine;

namespace GAS
{
    [Serializable]
    public sealed class GameplayCueNotifyData
    {
        [field: SerializeField]
        public GameplayCueNotify GameplayCueNotify
        {
            get;
            private set;
        }

        public GameplayTag GameplayCueTag =>
            GameplayCueNotify != null
                ? GameplayCueNotify.GameplayCueTag
                : null;
    }
}