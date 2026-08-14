using GAS;
using UnityEngine;

namespace RelicsOfTheFallen.Abilities
{
    /// <summary>
    /// Provides the Unity asset representation of a configurable projectile ability.
    /// </summary>
    [CreateAssetMenu(
        menuName = "Relics Of The Fallen/Abilities/Projectile",
        fileName = "GA_Projectile")]
    public sealed class ProjectileAbilitySO :
        GameplayAbilitySO
    {
        private void OnEnable()
        {
            ga ??= new ProjectileAbility();
        }
    }
}