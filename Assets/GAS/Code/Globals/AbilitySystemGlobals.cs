using UnityEngine;

namespace GAS
{
    public static class AbilitySystemGlobals
    {
        /// <summary>
        /// Finds the ability system associated with the requested gameplay actor root.
        /// </summary>
        public static bool TryGetAbilitySystemComponentFromActor(
            GameObject actor,
            out AbilitySystemComponent abilitySystem)
        {
            abilitySystem = null;

            if (actor == null)
            {
                return false;
            }

            return actor.TryGetComponent(out abilitySystem);
        }
    }
}