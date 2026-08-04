using UnityEngine;

namespace GAS
{
    public static class AbilitySystemGlobals
    {

        private const string k_ActivateFailCooldownTagName =
            "Ability.ActivateFail.Cooldown";

        private const string k_ActivateFailCostTagName =
            "Ability.ActivateFail.Cost";

        /// <summary>
        /// Returns the global failure tag used when an ability is still on cooldown.
        /// </summary>
        public static GameplayTag ActivateFailCooldownTag =>
            GameplayTagLibrary.Instance.GetByName(
                k_ActivateFailCooldownTagName);

        /// <summary>
        /// Returns the global failure tag used when an ability cannot afford its cost.
        /// </summary>
        public static GameplayTag ActivateFailCostTag =>
            GameplayTagLibrary.Instance.GetByName(
                k_ActivateFailCostTagName);

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