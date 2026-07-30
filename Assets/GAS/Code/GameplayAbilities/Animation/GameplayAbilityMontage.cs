using UnityEngine;

namespace GAS
{
    [CreateAssetMenu(
        menuName = "GAS/Gameplay Ability Montage",
        fileName = "AM_")]
    public sealed class GameplayAbilityMontage
        : ScriptableObject
    {
        [field: SerializeField]
        public AnimationClip Animation
        {
            get;
            private set;
        }
    }
}