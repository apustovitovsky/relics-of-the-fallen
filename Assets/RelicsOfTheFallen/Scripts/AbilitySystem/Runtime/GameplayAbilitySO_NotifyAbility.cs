using GAS;
using UnityEngine;

namespace RelicsOfTheFallen.AbilitySystem
{
    [CreateAssetMenu(
        fileName = "GA_NotifyAbility",
        menuName =
            "Relics Of The Fallen/Ability System/Notify Ability")]
    public sealed class GameplayAbilitySO_NotifyAbility
        : GameplayAbilitySO
    {
        public GameplayAbilitySO_NotifyAbility()
        {
            ga = new NotifyGameplayAbility();
        }
    }
}