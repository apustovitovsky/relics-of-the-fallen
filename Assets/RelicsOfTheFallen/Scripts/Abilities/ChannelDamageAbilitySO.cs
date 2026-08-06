using GAS;
using UnityEngine;

namespace RelicsOfTheFallen.Abilities
{
    /// <summary>
    /// Provides the Unity asset representation of the channel damage gameplay ability.
    /// </summary>
    [CreateAssetMenu(
        menuName = "Relics Of The Fallen/Abilities/Channel Damage",
        fileName = "GA_ChannelDamage")]
    public sealed class ChannelDamageAbilitySO :
        GameplayAbilitySO
    {
        private void OnEnable()
        {
            ga ??= new ChannelDamageAbility();
        }
    }
}