using GAS;
using UnityEngine;

namespace RelicsOfTheFallen.Abilities
{
    /// <summary>
    /// Provides the Unity asset representation of the fireball gameplay ability.
    /// </summary>
    [CreateAssetMenu(
        menuName = "Relics Of The Fallen/Abilities/Fireball",
        fileName = "GA_Fireball")]
    public sealed class FireballAbilitySO :
        GameplayAbilitySO
    {
        private void OnEnable()
        {
            ga ??= new FireballAbility();
        }
    }
}