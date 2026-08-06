using System;
using UnityEngine;

namespace GAS.Common
{
    /// <summary>
    /// Describes one gameplay ability granted by a common gameplay ability set.
    /// </summary>
    [Serializable]
    public sealed class GameplayAbilitySetEntry
    {
        [field: SerializeField]
        public GameplayAbilitySO Ability
        {
            get;
            private set;
        }

        [field: SerializeField]
        public int AbilityLevel
        {
            get;
            private set;
        } = 1;

        [field: SerializeField]
        public GameplayTag InputTag
        {
            get;
            private set;
        }
    }
}